using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CasualtiesMiner.Uploader.MediaWiki;

internal sealed class MediaWikiClient : IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    private readonly string _apiUrl;
    private readonly HttpClient _client;
    private readonly TimeSpan _requestDelay;
    private string? _csrfToken;

    public MediaWikiClient(string apiUrl, TimeSpan? requestDelay = null)
    {
        _apiUrl = apiUrl;
        _requestDelay = requestDelay ?? TimeSpan.FromMilliseconds(750);

        var handler = new SocketsHttpHandler
        {
            CookieContainer = new CookieContainer(),
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(10)
        };

        _client = new HttpClient(handler)
        {
            Timeout = DefaultTimeout
        };

        _client.DefaultRequestHeaders.UserAgent.ParseAdd("CasualtiesMinerUploader/1.0 (wiki data sync bot)");
    }

    public async Task LoginAsync(string user, string password)
    {
        var loginToken = await GetTokenAsync("login");

        using var response = await PostAsync(new Dictionary<string, string>
        {
            ["action"] = "login",
            ["lgname"] = user,
            ["lgpassword"] = password,
            ["lgtoken"] = loginToken,
            ["format"] = "json"
        });

        using var document = await ReadJsonAsync(response);
        var login = document.RootElement.GetProperty("login");
        var result = login.GetProperty("result").GetString();

        if (!string.Equals(result, "Success", StringComparison.Ordinal))
        {
            var reason = login.TryGetProperty("reason", out var r) ? r.GetString() : result;
            throw new InvalidOperationException($"Login failed: {reason}");
        }

        _csrfToken = await GetTokenAsync("csrf");
    }

    /// <summary>
    /// Creates or replaces a page. Skips the write when the revision SHA1 already matches.
    /// Returns the resulting edit status (<c>"nochange"</c>, <c>"Success"</c>, or <c>"dry-run"</c>).
    /// </summary>
    public async Task<string> EditAsync(string title, string text, string summary, bool dryRun = false)
    {
        var localSha1 = ComputeSha1(text);
        var remoteSha1 = await GetRevisionSha1Async(title);

        if (string.Equals(localSha1, remoteSha1, StringComparison.OrdinalIgnoreCase))
            return "nochange";

        if (dryRun)
        {
            var sanitizedTitle = Path.GetInvalidFileNameChars().Aggregate(title, (current, ch) => current.Replace(ch, '_'));
            var dryRunPath = Path.Combine(Program.DryRunFolder, sanitizedTitle + ".txt");
            await using var writer = new StreamWriter(File.OpenWrite(dryRunPath));
            
            await writer.WriteLineAsync("=== Page ===");
            await writer.WriteLineAsync(title);
            await writer.WriteLineAsync("=== Summary ===");
            await writer.WriteLineAsync(summary);
            await writer.WriteLineAsync("=== Contents ===");
            await writer.WriteLineAsync(text);
            return "dry-run";
        }

        if (_csrfToken is null)
            throw new InvalidOperationException("Not logged in: call LoginAsync first.");

        using var response = await PostAsync(new Dictionary<string, string>
        {
            ["action"] = "edit",
            ["title"] = title,
            ["text"] = text,
            ["summary"] = summary,
            ["bot"] = "1",
            ["token"] = _csrfToken,
            ["format"] = "json"
        });

        using var document = await ReadJsonAsync(response);
        ThrowIfApiError(document, $"Edit of '{title}'");

        return document.RootElement.GetProperty("edit").GetProperty("result").GetString() ?? "unknown";
    }

    private async Task<string?> GetRevisionSha1Async(string title)
    {
        using var response = await PostAsync(new Dictionary<string, string>
        {
            ["action"] = "query",
            ["prop"] = "revisions",
            ["rvprop"] = "sha1",
            ["rvslots"] = "main",
            ["titles"] = title,
            ["formatversion"] = "2",
            ["format"] = "json"
        });

        using var document = await ReadJsonAsync(response);
        ThrowIfApiError(document, $"Query SHA1 for '{title}'");

        var pages = document.RootElement.GetProperty("query").GetProperty("pages");
        if (pages.GetArrayLength() == 0)
            return null;

        var page = pages[0];
        if (page.TryGetProperty("missing", out _))
            return null;

        if (!page.TryGetProperty("revisions", out var revisions) || revisions.GetArrayLength() == 0)
            return null;

        if (!revisions[0].TryGetProperty("slots", out var slots))
            return null;

        if (!slots.TryGetProperty("main", out var main))
            return null;

        return main.TryGetProperty("sha1", out var sha1) ? sha1.GetString() : null;
    }

    private async Task<string> GetTokenAsync(string type)
    {
        using var response = await PostAsync(new Dictionary<string, string>
        {
            ["action"] = "query",
            ["meta"] = "tokens",
            ["type"] = type,
            ["format"] = "json"
        });

        using var document = await ReadJsonAsync(response);
        ThrowIfApiError(document, $"Obtain '{type}' token");

        var tokens = document.RootElement.GetProperty("query").GetProperty("tokens");
        return tokens.GetProperty(type + "token").GetString()
               ?? throw new InvalidOperationException($"Could not obtain '{type}' token.");
    }

    private async Task<HttpResponseMessage> PostAsync(Dictionary<string, string> parameters)
    {
        const int maxAttempts = 5;
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var content = new FormUrlEncodedContent(parameters);
                var response = await _client.PostAsync(_apiUrl, content);

                if (IsTransientStatusCode(response.StatusCode) && attempt < maxAttempts)
                {
                    response.Dispose();
                    await DelayBeforeRetryAsync(attempt);
                    continue;
                }

                if (_requestDelay > TimeSpan.Zero)
                    await Task.Delay(_requestDelay);

                return response;
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < maxAttempts)
            {
                lastException = ex;
                Console.Error.WriteLine($"  API request failed (attempt {attempt}/{maxAttempts}): {ex.Message}");
                await DelayBeforeRetryAsync(attempt);
            }
        }

        throw lastException ?? new InvalidOperationException("API request failed after all retry attempts.");
    }

    private static async Task DelayBeforeRetryAsync(int attempt)
    {
        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
        Console.Error.WriteLine($"  Retrying in {delay.TotalSeconds:0}s ...");
        await Task.Delay(delay);
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private static bool IsTransient(Exception ex)
    {
        return ex is HttpRequestException or TaskCanceledException or IOException;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private static void ThrowIfApiError(JsonDocument document, string context)
    {
        if (!document.RootElement.TryGetProperty("error", out var error))
            return;

        var code = error.TryGetProperty("code", out var c) ? c.GetString() : "unknown";
        var info = error.TryGetProperty("info", out var i) ? i.GetString() : "unknown error";
        throw new InvalidOperationException($"{context} failed: {code} — {info}");
    }

    private static string ComputeSha1(string text)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
