using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Memento.Core.Settings.Models;

namespace Memento.Core.Settings.Converters;

/// <summary>
/// A time of day as "HH:mm". An unreadable value, of any JSON type, falls back to the default instead of
/// discarding the whole file.
/// </summary>
internal sealed class HourMinuteJsonConverter : JsonConverter<TimeOnly>
{
    private const string Format = "HH:mm";

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            reader.Skip(); // past a whole object or array, so the rest of the file still reads
            return new AppSettings().DefaultTime;
        }

        return TimeOnly.TryParseExact(reader.GetString(), Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            ? time
            : new AppSettings().DefaultTime;
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
}
