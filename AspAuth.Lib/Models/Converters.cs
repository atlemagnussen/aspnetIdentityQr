using System.Text.Json;
using System.Text.Json.Serialization;

namespace AspAuth.Lib.Models;

public sealed class OnOffBooleanConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        return r.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number => r.TryGetInt64(out var n) && n != 0,
            JsonTokenType.String => ParseString(r.GetString()),
            _ => throw new JsonException("Invalid token for bool")
        };
        static bool ParseString(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (string.Equals(s, "on", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, "off", StringComparison.OrdinalIgnoreCase)) return false;
            if (bool.TryParse(s, out var b)) return b;
            if (long.TryParse(s, out var n)) return n != 0;
            throw new JsonException($"Cannot convert '{s}' to bool");
        }
    }
    public override void Write(Utf8JsonWriter w, bool value, JsonSerializerOptions o) => w.WriteBooleanValue(value);
}