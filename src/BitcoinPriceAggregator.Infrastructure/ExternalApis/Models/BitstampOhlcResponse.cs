using System.Text.Json.Serialization;

namespace BitcoinPriceAggregator.Infrastructure.ExternalApis.Models
{
    public class BitstampOhlcResponse
    {
        [JsonPropertyName("data")]
        public BitstampOhlcData Data { get; set; } = null!;
    }

    public class BitstampOhlcData
    {
        [JsonPropertyName("ohlc")]
        public BitstampOhlcEntry[] Ohlc { get; set; } = null!;
    }

    public class BitstampOhlcEntry
    {
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = null!;

        [JsonPropertyName("close")]
        public string Close { get; set; } = null!;
    }
} 