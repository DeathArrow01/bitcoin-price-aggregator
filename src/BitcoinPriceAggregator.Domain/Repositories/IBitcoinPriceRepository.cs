using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BitcoinPriceAggregator.Domain.Entities;

namespace BitcoinPriceAggregator.Domain.Repositories
{
    /// <summary>
    /// Repository interface for Bitcoin price data
    /// </summary>
    public interface IBitcoinPriceRepository
    {
        /// <summary>
        /// Gets the price for a specific timestamp and pair
        /// </summary>
        /// <param name="utcTicks">The UTC ticks (will be normalized to hour precision)</param>
        /// <param name="pair">The trading pair (e.g., "BTC/USD")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The price record if found, null otherwise</returns>
        Task<BitcoinPrice?> GetPriceAsync(long utcTicks, string pair, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets prices within a time range for a specific pair
        /// </summary>
        /// <param name="startTicks">The start UTC ticks (will be normalized to hour precision)</param>
        /// <param name="endTicks">The end UTC ticks (will be normalized to hour precision)</param>
        /// <param name="pair">The trading pair (e.g., "BTC/USD")</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of prices within the range</returns>
        Task<IEnumerable<BitcoinPrice>> GetPriceRangeAsync(long startTicks, long endTicks, string pair, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores a new price record
        /// </summary>
        /// <param name="bitcoinPrice">The price record to store</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task StorePriceAsync(BitcoinPrice bitcoinPrice, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes data older than the specified cutoff date
        /// </summary>
        /// <param name="cutoffTicks">The cutoff UTC ticks (will be normalized to hour precision)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RemoveOldDataAsync(long cutoffTicks, CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes the database
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task OptimizeDatabaseAsync(CancellationToken cancellationToken = default);
    }
} 