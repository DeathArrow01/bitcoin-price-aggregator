using BitcoinPriceAggregator.Domain.Entities;
using BitcoinPriceAggregator.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BitcoinPriceAggregator.Infrastructure.Persistence
{
    public class BitcoinPriceRepository : IBitcoinPriceRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<BitcoinPriceRepository> _logger;
        private const string SinglePriceCacheKeyPrefix = "price_";
        private const string RangeCacheKeyPrefix = "range_";
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(60);

        public BitcoinPriceRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<BitcoinPriceRepository> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<BitcoinPrice?> GetPriceAsync(long utcTicks, string pair, CancellationToken cancellationToken = default)
        {
            utcTicks = NormalizeTicksToHourPrecision(utcTicks);
            var cacheKey = GetSinglePriceCacheKey(utcTicks, pair);
            if (_cache.TryGetValue<BitcoinPrice>(cacheKey, out var cachedPrice))
            {
                _logger.LogDebug("Cache hit for price at ticks {UtcTicks} for {Pair}", utcTicks, pair);
                return cachedPrice;
            }

            var price = await _context.BitcoinPrices
                .FirstOrDefaultAsync(p => p.UtcTicks == utcTicks && p.Pair == pair, cancellationToken);

            if (price != null)
            {
                _cache.Set(cacheKey, price, CacheExpiration);
                _logger.LogDebug("Cached price for ticks {UtcTicks} and {Pair}", utcTicks, pair);
            }

            return price;
        }

        public async Task<IEnumerable<BitcoinPrice>> GetPriceRangeAsync(long startTicks, long endTicks, string pair, CancellationToken cancellationToken = default)
        {
            startTicks = NormalizeTicksToHourPrecision(startTicks);
            endTicks = NormalizeTicksToHourPrecision(endTicks);
            var cacheKey = GetRangeCacheKey(startTicks, endTicks, pair);
            if (_cache.TryGetValue<IEnumerable<BitcoinPrice>>(cacheKey, out var cachedPrices))
            {
                return cachedPrices;
            }

            var prices = await _context.BitcoinPrices
                .AsNoTracking()
                .Where(p => p.Pair == pair && p.UtcTicks >= startTicks && p.UtcTicks <= endTicks)
                .OrderBy(p => p.UtcTicks)
                .ToListAsync(cancellationToken);

            _cache.Set(cacheKey, prices, TimeSpan.FromMinutes(5));
            return prices;
        }

        public async Task StorePriceAsync(BitcoinPrice bitcoinPrice, CancellationToken cancellationToken = default)
        {
            bitcoinPrice.UtcTicks = NormalizeTicksToHourPrecision(bitcoinPrice.UtcTicks);
            await _context.BitcoinPrices.AddAsync(bitcoinPrice, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var singlePriceCacheKey = GetSinglePriceCacheKey(bitcoinPrice.UtcTicks, bitcoinPrice.Pair);
            _cache.Set(singlePriceCacheKey, bitcoinPrice, CacheExpiration);

            // Invalidate any range caches that might contain this timestamp
            InvalidateRangeCaches(bitcoinPrice.UtcTicks, bitcoinPrice.Pair);

            _logger.LogDebug("Stored and cached price for ticks {UtcTicks} and {Pair}", bitcoinPrice.UtcTicks, bitcoinPrice.Pair);
        }

        public async Task RemoveOldDataAsync(long cutoffTicks, CancellationToken cancellationToken = default)
        {
            cutoffTicks = NormalizeTicksToHourPrecision(cutoffTicks);
            var oldRecords = await _context.BitcoinPrices
                .Where(p => p.UtcTicks < cutoffTicks)
                .ToListAsync(cancellationToken);

            if (oldRecords.Any())
            {
                _context.BitcoinPrices.RemoveRange(oldRecords);
                await _context.SaveChangesAsync(cancellationToken);
                await InvalidateCacheAsync();
            }
        }

        public async Task OptimizeDatabaseAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting database optimization");
            await _context.Database.ExecuteSqlRawAsync("VACUUM;", cancellationToken);
            _logger.LogInformation("Database optimization completed");
        }

        private string GetSinglePriceCacheKey(long utcTicks, string pair)
        {
            var hourTicks = NormalizeTicksToHourPrecision(utcTicks);
            var hourDate = new DateTime(hourTicks, DateTimeKind.Utc);
            return $"{SinglePriceCacheKeyPrefix}{hourDate:yyyy-MM-ddTHH}_{pair}";
        }

        private string GetRangeCacheKey(long startTicks, long endTicks, string pair)
        {
            var startHourDate = new DateTime(NormalizeTicksToHourPrecision(startTicks), DateTimeKind.Utc);
            var endHourDate = new DateTime(NormalizeTicksToHourPrecision(endTicks), DateTimeKind.Utc);
            return $"{RangeCacheKeyPrefix}{startHourDate:yyyy-MM-ddTHH}_{endHourDate:yyyy-MM-ddTHH}_{pair}";
        }

        private void InvalidateRangeCaches(long utcTicks, string pair)
        {
            // For now, we'll just remove all range caches for the given pair
            var pattern = $"{RangeCacheKeyPrefix}*_{pair}";
            _cache.Remove(pattern);
        }

        private static long NormalizeTicksToHourPrecision(long ticks)
        {
            const long ticksPerHour = TimeSpan.TicksPerHour;
            return ticks - (ticks % ticksPerHour);
        }

        private async Task InvalidateCacheAsync()
        {
            // Implementation of InvalidateCacheAsync method
        }
    }

    // Extension method to get cache keys
    public static class MemoryCacheExtensions
    {
        public static IEnumerable<string> GetKeys<T>(this IMemoryCache cache)
        {
            var field = typeof(MemoryCache).GetProperty("EntriesCollection",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var collection = field?.GetValue(cache) as dynamic;
            var keys = new List<string>();

            if (collection != null)
            {
                foreach (var item in collection)
                {
                    keys.Add(item.Key.ToString());
                }
            }

            return keys;
        }
    }
}