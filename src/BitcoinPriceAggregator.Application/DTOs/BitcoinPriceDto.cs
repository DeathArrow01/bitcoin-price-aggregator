using System;
using System.Text.Json.Serialization;
using BitcoinPriceAggregator.Application.Common.JsonConverters;

namespace BitcoinPriceAggregator.Application.DTOs
{
    /// <summary>
    /// Data transfer object representing a Bitcoin price at a specific point in time.
    /// </summary>
    public class BitcoinPriceDto
    {
        /// <summary>
        /// Gets or sets the trading pair (e.g., "BTC/USD").
        /// </summary>
        /// <example>BTC/USD</example>
        [JsonPropertyName("pair")]
        public string Pair { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the price value.
        /// </summary>
        /// <example>45000.50</example>
        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when this price was recorded.
        /// </summary>
        /// <example>2024-03-08T12:00:00Z</example>
        [JsonPropertyName("timestamp")]
        public DateTimeOffset Timestamp { get; set; }
    }
} 