using System.Text.Json.Serialization;

namespace BitcoinPriceAggregator.Infrastructure.ExternalApis.Models
{
    // Bitfinex returns an array of arrays where each inner array represents:
    // [timestamp, open, close, high, low, volume]
    public class BitfinexOhlcEntry
    {
        [JsonConstructor]
        public BitfinexOhlcEntry(long timestamp, decimal close)
        {
            Timestamp = timestamp;
            Close = close;
        }

        public long Timestamp { get; }
        public decimal Close { get; }
    }
} 