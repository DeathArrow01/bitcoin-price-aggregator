using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BitcoinPriceAggregator.Domain.Exceptions;
using BitcoinPriceAggregator.Domain.Services;
using BitcoinPriceAggregator.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BitcoinPriceAggregator.Infrastructure.Tests.Services
{
    public class PriceAggregatorServiceTests
    {
        private readonly Mock<IPriceProvider> _mockProvider1;
        private readonly Mock<IPriceProvider> _mockProvider2;
        private readonly Mock<ILogger<PriceAggregatorService>> _mockLogger;
        private readonly Mock<IPriceCalculationStrategy> _mockStrategy;
        private readonly PriceAggregatorService _service;

        public PriceAggregatorServiceTests()
        {
            _mockProvider1 = new Mock<IPriceProvider>();
            _mockProvider2 = new Mock<IPriceProvider>();
            _mockLogger = new Mock<ILogger<PriceAggregatorService>>();
            _mockStrategy = new Mock<IPriceCalculationStrategy>();

            var providers = new List<IPriceProvider> { _mockProvider1.Object, _mockProvider2.Object };
            _service = new PriceAggregatorService(providers, _mockLogger.Object, _mockStrategy.Object);
        }

        private static long NormalizeToHourPrecision(DateTime timestamp)
        {
            return new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, 0, 0, DateTimeKind.Utc).Ticks;
        }

        [Fact]
        public async Task GetAggregatedPriceAsync_WithValidPrices_ReturnsAveragePrice()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            var normalizedTicks = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero).Ticks;
            var pair = "BTC/USD";
            _mockProvider1.Setup(p => p.GetPriceAsync(normalizedTicks, pair)).ReturnsAsync(50000m);
            _mockProvider2.Setup(p => p.GetPriceAsync(normalizedTicks, pair)).ReturnsAsync(52000m);
            _mockStrategy.Setup(s => s.Calculate(It.Is<IEnumerable<decimal>>(prices => 
                prices.Contains(50000m) && prices.Contains(52000m)))).Returns(51000m);

            // Act
            var result = await _service.GetAggregatedPriceAsync(now.Ticks, pair);

            // Assert
            result.Should().NotBeNull();
            result.Price.Should().Be(51000m);
            result.Pair.Should().Be(pair);
            result.UtcTicks.Should().Be(normalizedTicks);
        }

        [Fact]
        public async Task GetAggregatedPriceAsync_WithOneProviderFailing_UsesValidPrice()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            var normalizedTicks = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero).Ticks;
            var pair = "BTC/USD";
            _mockProvider1.Setup(p => p.GetPriceAsync(normalizedTicks, pair)).ReturnsAsync(50000m);
            _mockProvider2.Setup(p => p.GetPriceAsync(normalizedTicks, pair)).ThrowsAsync(new Exception("API Error"));
            _mockStrategy.Setup(s => s.Calculate(It.Is<IEnumerable<decimal>>(prices => 
                prices.Single() == 50000m))).Returns(50000m);

            // Act
            var result = await _service.GetAggregatedPriceAsync(now.Ticks, pair);

            // Assert
            result.Should().NotBeNull();
            result.Price.Should().Be(50000m);
            result.Pair.Should().Be(pair);
            result.UtcTicks.Should().Be(normalizedTicks);
        }

        [Fact]
        public async Task GetAggregatedPriceAsync_WithAllProvidersFailing_ThrowsException()
        {
            // Arrange
            var pair = "BTC/USD";
            var timestamp = DateTime.UtcNow;
            var normalizedTicks = NormalizeToHourPrecision(timestamp);
            _mockProvider1.Setup(p => p.GetPriceAsync(normalizedTicks, pair)).ThrowsAsync(new Exception("Provider 1 error"));
            _mockProvider2.Setup(p => p.GetPriceAsync(normalizedTicks, pair)).ThrowsAsync(new Exception("Provider 2 error"));

            // Act & Assert
            await _service.Invoking(s => s.GetAggregatedPriceAsync(normalizedTicks, pair))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Failed to get price from any provider");
        }

        [Fact]
        public async Task GetAggregatedPriceAsync_WithInvalidPrices_IgnoresInvalidPrices()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;
            var normalizedTicks = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero).Ticks;
            var pair = "BTC/USD";
            _mockProvider1.Setup(p => p.GetPriceAsync(normalizedTicks, pair)).ReturnsAsync(50000m);
            _mockProvider2.Setup(p => p.GetPriceAsync(normalizedTicks, pair)).ReturnsAsync(0m);
            _mockStrategy.Setup(s => s.Calculate(It.Is<IEnumerable<decimal>>(prices => 
                prices.Single() == 50000m))).Returns(50000m);

            // Act
            var result = await _service.GetAggregatedPriceAsync(now.Ticks, pair);

            // Assert
            result.Should().NotBeNull();
            result.Price.Should().Be(50000m);
            result.Pair.Should().Be(pair);
            result.UtcTicks.Should().Be(normalizedTicks);
        }

        [Fact]
        public void Constructor_WithNullParameters_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PriceAggregatorService(null!, _mockLogger.Object, _mockStrategy.Object));
            Assert.Throws<ArgumentNullException>(() => new PriceAggregatorService(new List<IPriceProvider>(), null!, _mockStrategy.Object));
            Assert.Throws<ArgumentNullException>(() => new PriceAggregatorService(new List<IPriceProvider>(), _mockLogger.Object, null!));
        }
    }
}