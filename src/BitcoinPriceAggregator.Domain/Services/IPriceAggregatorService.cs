using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BitcoinPriceAggregator.Domain.Entities;

namespace BitcoinPriceAggregator.Domain.Services
{
    /// <summary>
    /// Service for aggregating Bitcoin prices from multiple providers
    /// </summary>
    public interface IPriceAggregatorService
    {
        /// <summary>
        /// Gets the aggregated Bitcoin price for a specific timestamp
        /// </summary>
        /// <param name="utcTicks">The UTC ticks (will be normalized to hour precision)</param>
        /// <param name="pair">The trading pair (e.g., "BTC/USD")</param>
        /// <returns>The aggregated Bitcoin price</returns>
        Task<BitcoinPrice> GetAggregatedPriceAsync(long utcTicks, string pair);
    }
} 