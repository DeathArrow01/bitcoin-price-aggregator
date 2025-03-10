using System;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;
using BitcoinPriceAggregator.Domain.Services;

namespace BitcoinPriceAggregator.Infrastructure.Tests.Services
{
    public class AveragePriceCalculationStrategyTests
    {
        private readonly AveragePriceCalculationStrategy _strategy;

        public AveragePriceCalculationStrategyTests()
        {
            _strategy = new AveragePriceCalculationStrategy();
        }

        [Fact]
        public void Calculate_WithValidPrices_ReturnsAverage()
        {
            // Arrange
            var prices = new List<decimal> { 100m, 200m, 300m };
            var expectedAverage = 200m;

            // Act
            var result = _strategy.Calculate(prices);

            // Assert
            result.Should().Be(expectedAverage);
        }

        [Fact]
        public void Calculate_WithSinglePrice_ReturnsSamePrice()
        {
            // Arrange
            var prices = new List<decimal> { 100m };

            // Act
            var result = _strategy.Calculate(prices);

            // Assert
            result.Should().Be(100m);
        }

        [Theory]
        [InlineData(null)]
        public void Calculate_WithNullPrices_ThrowsArgumentException(IEnumerable<decimal> prices)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _strategy.Calculate(prices));
            exception.Message.Should().Be("Prices list cannot be null or empty (Parameter 'prices')");
        }

        [Fact]
        public void Calculate_WithEmptyPrices_ThrowsArgumentException()
        {
            // Arrange
            var prices = new List<decimal>();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _strategy.Calculate(prices));
            exception.Message.Should().Be("Prices list cannot be null or empty (Parameter 'prices')");
        }
    }
} 