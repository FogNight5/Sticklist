using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sticklist.Services;

// Reads and writes DateTime as the literal wall-clock value, ignoring any timezone
// offset in the JSON. Without this, a timestamp like "10:00:00+02:00" gets converted
// to whatever the CURRENT machine's timezone happens to be when deserialized (e.g.
// after a daylight-saving change), silently shifting the displayed time. Sticklist
// never reasons about timezones anywhere else (entries are always compared against
// the same device's current local clock), so timestamps should stay exactly as recorded.
public class LiteralDateTimeConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.fffffff";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString()!;
        return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture).DateTime;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}
