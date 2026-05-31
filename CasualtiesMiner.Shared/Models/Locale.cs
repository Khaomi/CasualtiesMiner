using System.Text.Json.Serialization;

namespace CasualtiesMiner.Shared.Models;

public class Locale
{
    public class Note
    {
        [JsonPropertyName("Item1")]
        [JsonPropertyOrder(1)]
        public string? Text { get; set; }

        [JsonPropertyName("Item2")]
        [JsonPropertyOrder(2)]
        public string? Sprite { get; set; }

        [JsonPropertyName("Item3")]
        [JsonPropertyOrder(3)]
        public string? Font { get; set; }

        public Note Clone()
        {
            return new Note()
            {
                Text = Text,
                Sprite = Sprite,
                Font = Font,
            };
        }
    }

    public class PdaNote
    {
        [JsonPropertyName("Item1")]
        [JsonPropertyOrder(1)]
        public string? Text { get; set; }

        [JsonPropertyName("Item2")]
        [JsonPropertyOrder(2)]
        public string? Sprite { get; set; }

        public PdaNote Clone()
        {
            return new PdaNote()
            {
                Text = Text,
                Sprite = Sprite,
            };
        }
    }

    [JsonPropertyName("name")]
    [JsonPropertyOrder(1)]
    public string Name { get; set; } = "Unknown";

    [JsonPropertyName("description")]
    [JsonPropertyOrder(2)]
    public string Description { get; set; } = "";

    [JsonPropertyName("main")]
    [JsonPropertyOrder(3)]
    public OrderedDictionary<string, string> Main { get; set; } = [];

    [JsonPropertyName("buildings")]
    [JsonPropertyOrder(4)]
    public OrderedDictionary<string, string> Buildings { get; set; } = [];

    [JsonPropertyName("moodles")]
    [JsonPropertyOrder(5)]
    public OrderedDictionary<string, string> Moodles { get; set; } = [];

    [JsonPropertyName("other")]
    [JsonPropertyOrder(6)]
    public OrderedDictionary<string, string> Other { get; set; } = [];

    [JsonPropertyName("character")]
    [JsonPropertyOrder(7)]
    public List<OrderedDictionary<string, List<string>>> Character { get; set; } = [];

    [JsonPropertyName("notes")]
    [JsonPropertyOrder(8)]
    public List<List<Note>> Notes { get; set; } = [];

    [JsonPropertyName("pdaNotes")]
    [JsonPropertyOrder(9)]
    public List<PdaNote> PdaNotes { get; set; } = [];

    [JsonPropertyName("pauseQuotes")]
    [JsonPropertyOrder(10)]
    public List<string> PauseQuotes { get; set; } = [];
}