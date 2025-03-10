using System.Text.Json;
using System.Text.Json.Serialization;

namespace BitcoinPriceAggregator.Application.Common.JsonConverters
{
    public class UtcTicksConverter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var timestampStr = reader.GetString();
            if (DateTime.TryParse(timestampStr, out var timestamp))
            {
                return NormalizeTicksToHourPrecision(timestamp.ToUniversalTime().Ticks);
            }
            throw new JsonException("Invalid timestamp format");
        }

        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        {
            var normalizedTicks = NormalizeTicksToHourPrecision(value);
            var dateTime = new DateTime(normalizedTicks, DateTimeKind.Utc);
            writer.WriteStringValue(dateTime.ToString("O"));
        }

        private static long NormalizeTicksToHourPrecision(long ticks)
        {
            const long ticksPerHour = TimeSpan.TicksPerHour;
            return ticks - (ticks % ticksPerHour);
        }
    }
} 