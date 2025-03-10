using System;
using System.Threading.Tasks;

namespace BitcoinPriceAggregator.Domain.Services
{
    /// <summary>
    /// Interface for external price providers
    /// </summary>
    public interface IPriceProvider
    {
        /// <summary>
        /// Gets the name of the price provider
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Gets the price for a specific timestamp
        /// </summary>
        /// <param name="utcTicks">The UTC ticks (will be normalized to hour precision)</param>
        /// <param name="pair">The trading pair (e.g., "BTC/USD")</param>
        /// <returns>The price value</returns>
        Task<decimal> GetPriceAsync(long utcTicks, string pair);
    }
} 