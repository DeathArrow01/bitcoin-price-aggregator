using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BitcoinPriceAggregator.Domain.Entities;
using BitcoinPriceAggregator.Domain.Services;
using BitcoinPriceAggregator.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BitcoinPriceAggregator.Application.Tests.Services
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
            var pair = "BTC/USD";
            var timestamp = DateTime.UtcNow;
            var normalizedTicks = NormalizeToHourPrecision(timestamp);
            var price1 = 50000m;
            var price2 = 51000m;

            _mockProvider1.Setup(p => p.GetPriceAsync(normalizedTicks, pair)).ReturnsAsync(price1);
            _mockProvider2.Setup(p => p.GetPriceAsync(normalizedTicks, pair)).ReturnsAsync(price2);
            _mockStrategy.Setup(s => s.Calculate(It.IsAny<IEnumerable<decimal>>())).Returns(50500m);

            // Act
            var result = await _service.GetAggregatedPriceAsync(normalizedTicks, pair);

            // Assert
            result.Should().NotBeNull();
            result.Price.Should().Be(50500m);
            result.Pair.Should().Be(pair);
            result.UtcTicks.Should().Be(normalizedTicks);
        }

        [Fact]
        public async Task GetAggregatedPriceAsync_WithOneProviderFailing_UsesValidPrice()
        {
            // Arrange
            var pair = "BTC/USD";
            var timestamp = DateTime.UtcNow;
            var normalizedTicks = NormalizeToHourPrecision(timestamp);
            var price = 50000m;

            _mockProvider1.Setup(p => p.GetPriceAsync(normalizedTicks, pair)).ReturnsAsync(price);
            _mockProvider2.Setup(p => p.GetPriceAsync(normalizedTicks, pair)).ThrowsAsync(new Exception("Provider error"));
            _mockStrategy.Setup(s => s.Calculate(It.IsAny<IEnumerable<decimal>>())).Returns(50000m);

            // Act
            var result = await _service.GetAggregatedPriceAsync(normalizedTicks, pair);

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
        public void Constructor_WithNullParameters_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PriceAggregatorService(null!, _mockLogger.Object, _mockStrategy.Object));
            Assert.Throws<ArgumentNullException>(() => new PriceAggregatorService(new List<IPriceProvider>(), null!, _mockStrategy.Object));
            Assert.Throws<ArgumentNullException>(() => new PriceAggregatorService(new List<IPriceProvider>(), _mockLogger.Object, null!));
        }
    }
} 