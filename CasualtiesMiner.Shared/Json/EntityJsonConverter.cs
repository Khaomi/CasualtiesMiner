using System.Text.Json;
using System.Text.Json.Serialization;
using AngouriMath;

namespace CasualtiesMiner.Shared.Json;

public class EntityJsonConverter : JsonConverter<Entity>
{
    public override Entity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, Entity value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("$type", value.GetType().Name);

        switch (value)
        {
            case Entity.Number.Integer v:
                writer.WriteNumber("value", v.EInteger.ToInt64Checked());
                break;
            case Entity.Number.Real v:
                writer.WriteNumber("value", v.AsDouble());
                break;
            case Entity.Variable v:
                writer.WriteString("name", v.Name);
                break;
        }

        var children = value.DirectChildren;
        if (children.Count > 0)
        {
            writer.WritePropertyName("children");
            writer.WriteStartArray();
            foreach (var child in children)
            {
                Write(writer, child, options);
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }
}