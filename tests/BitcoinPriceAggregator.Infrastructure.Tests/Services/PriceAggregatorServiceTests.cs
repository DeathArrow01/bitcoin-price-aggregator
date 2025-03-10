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
        public void Constructor_WithNullParameters_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PriceAggregatorService(null!, _mockLogger.Object, _mockStrategy.Object));
            Assert.Throws<ArgumentNullException>(() => new PriceAggregatorService(new List<IPriceProvider>(), null!, _mockStrategy.Object));
            Assert.Throws<ArgumentNullException>(() => new PriceAggregatorService(new List<IPriceProvider>(), _mockLogger.Object, null!));
        }
    }
}