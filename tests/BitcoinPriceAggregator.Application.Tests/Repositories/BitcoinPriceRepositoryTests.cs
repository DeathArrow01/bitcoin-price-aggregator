using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;
using BitcoinPriceAggregator.Domain.Entities;
using BitcoinPriceAggregator.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;

namespace BitcoinPriceAggregator.Application.Tests.Repositories
{
    public class BitcoinPriceRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly Mock<ILogger<BitcoinPriceRepository>> _mockLogger;
        private readonly BitcoinPriceRepository _repository;
        private readonly SqliteConnection _connection;

        public BitcoinPriceRepositoryTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            _cache = new MemoryCache(new MemoryCacheOptions());
            _mockLogger = new Mock<ILogger<BitcoinPriceRepository>>();
            _repository = new BitcoinPriceRepository(_context, _cache, _mockLogger.Object);
        }

        [Fact]
        public async Task GetPriceRangeAsync_ReturnsCorrectPrices()
        {
            // Arrange
            await ClearDatabase();
            var now = DateTimeOffset.UtcNow;
            var startTicks = NormalizeTicksToHourPrecision(now.AddHours(-2).Ticks);
            var endTicks = NormalizeTicksToHourPrecision(now.Ticks);
            var pair = "BTC/USD";
            var prices = new List<BitcoinPrice>
            {
                BitcoinPrice.CreateBuilder()
                    .WithPair(pair)
                    .WithPrice(50000m)
                    .WithUtcTicks(startTicks)
                    .Build(),
                BitcoinPrice.CreateBuilder()
                    .WithPair(pair)
                    .WithPrice(51000m)
                    .WithUtcTicks(endTicks)
                    .Build()
            };

            await _context.BitcoinPrices.AddRangeAsync(prices);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetPriceRangeAsync(startTicks, endTicks, pair);

            // Assert
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(prices);
        }

        [Fact]
        public async Task GetPriceAsync_ReturnsCachedPrice()
        {
            // Arrange
            await ClearDatabase();
            var utcTicks = NormalizeTicksToHourPrecision(DateTimeOffset.UtcNow.Ticks);
            var pair = "BTC/USD";
            var price = BitcoinPrice.CreateBuilder()
                .WithPair(pair)
                .WithPrice(52000m)
                .WithUtcTicks(utcTicks)
                .Build();

            var hourDate = new DateTimeOffset(utcTicks, TimeSpan.Zero);
            var cacheKey = $"price_{hourDate:yyyy-MM-ddTHH}_{pair}";
            _cache.Set(cacheKey, price, TimeSpan.FromMinutes(60));

            // Act
            var result = await _repository.GetPriceAsync(utcTicks, pair);

            // Assert
            result.Should().NotBeNull();
            result!.Price.Should().Be(52000m);
            result.Pair.Should().Be(pair);
            result.UtcTicks.Should().Be(utcTicks);
        }

        [Fact]
        public async Task GetPriceAsync_ReturnsFromDatabase()
        {
            // Arrange
            await ClearDatabase();
            var utcTicks = NormalizeTicksToHourPrecision(DateTimeOffset.UtcNow.Ticks);
            var pair = "BTC/USD";
            var price = BitcoinPrice.CreateBuilder()
                .WithPair(pair)
                .WithPrice(52000m)
                .WithUtcTicks(utcTicks)
                .Build();

            await _context.BitcoinPrices.AddAsync(price);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetPriceAsync(utcTicks, pair);

            // Assert
            result.Should().NotBeNull();
            result!.Price.Should().Be(52000m);
            result.Pair.Should().Be(pair);
            result.UtcTicks.Should().Be(utcTicks);
        }

        [Fact]
        public async Task StorePriceAsync_StoresPriceAndUpdatesCache()
        {
            // Arrange
            await ClearDatabase();
            var utcTicks = NormalizeTicksToHourPrecision(DateTimeOffset.UtcNow.Ticks);
            var pair = "BTC/USD";
            var price = BitcoinPrice.CreateBuilder()
                .WithPair(pair)
                .WithPrice(52000m)
                .WithUtcTicks(utcTicks)
                .Build();

            // Act
            await _repository.StorePriceAsync(price);

            // Assert
            var storedPrice = await _context.BitcoinPrices.FirstOrDefaultAsync(p => p.UtcTicks == utcTicks && p.Pair == pair);
            storedPrice.Should().NotBeNull();
            storedPrice!.Price.Should().Be(52000m);

            var hourDate = new DateTimeOffset(utcTicks, TimeSpan.Zero);
            var cacheKey = $"price_{hourDate:yyyy-MM-ddTHH}_{pair}";
            var cachedPrice = _cache.Get<BitcoinPrice>(cacheKey);
            cachedPrice.Should().NotBeNull();
            cachedPrice!.Price.Should().Be(52000m);
        }

        [Fact]
        public async Task RemoveOldDataAsync_RemovesOldPrices()
        {
            // Arrange
            await ClearDatabase();
            var now = DateTimeOffset.UtcNow;
            var oldTicks = NormalizeTicksToHourPrecision(now.AddDays(-2).Ticks);
            var recentTicks = NormalizeTicksToHourPrecision(now.Ticks);
            var cutoffTicks = NormalizeTicksToHourPrecision(now.AddDays(-1).Ticks);
            var pair = "BTC/USD";

            var prices = new List<BitcoinPrice>
            {
                BitcoinPrice.CreateBuilder()
                    .WithPair(pair)
                    .WithPrice(52000m)
                    .WithUtcTicks(oldTicks)
                    .Build(),
                BitcoinPrice.CreateBuilder()
                    .WithPair(pair)
                    .WithPrice(52000m)
                    .WithUtcTicks(recentTicks)
                    .Build()
            };

            await _context.BitcoinPrices.AddRangeAsync(prices);
            await _context.SaveChangesAsync();

            // Act
            await _repository.RemoveOldDataAsync(cutoffTicks);

            // Assert
            var remainingPrices = await _context.BitcoinPrices.ToListAsync();
            remainingPrices.Should().HaveCount(1);
            remainingPrices.Single().UtcTicks.Should().Be(recentTicks);
        }

        private async Task ClearDatabase()
        {
            _context.BitcoinPrices.RemoveRange(_context.BitcoinPrices);
            await _context.SaveChangesAsync();
        }

        private static long NormalizeTicksToHourPrecision(long ticks)
        {
            var dateTime = new DateTimeOffset(ticks, TimeSpan.Zero);
            return new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0, TimeSpan.Zero).Ticks;
        }

        public void Dispose()
        {
            _connection.Dispose();
            _context.Dispose();
            _cache.Dispose();
        }
    }
} 