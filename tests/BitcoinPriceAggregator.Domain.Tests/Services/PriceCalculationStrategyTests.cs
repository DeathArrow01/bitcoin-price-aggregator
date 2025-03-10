using FluentAssertions;
using Xunit;

namespace BitcoinPriceAggregator.Domain.Tests.Services
{
    public class PriceCalculationStrategyTests
    {
        [Fact]
        public void AverageStrategy_Calculate_WithValidPrices_ReturnsCorrectAverage()
        {
            // Arrange
            var strategy = new AveragePriceCalculationStrategy();
            var prices = new List<decimal> { 10m, 20m, 30m };

            // Act
            var result = strategy.Calculate(prices);

            // Assert
            result.Should().Be(20m);
        }

        [Fact]
        public void MedianStrategy_Calculate_WithOddNumberOfPrices_ReturnsMiddleValue()
        {
            // Arrange
            var strategy = new MedianPriceCalculationStrategy();
            var prices = new List<decimal> { 10m, 30m, 20m };

            // Act
            var result = strategy.Calculate(prices);

            // Assert
            result.Should().Be(20m);
        }

        [Fact]
        public void MedianStrategy_Calculate_WithEvenNumberOfPrices_ReturnsAverageOfMiddleValues()
        {
            // Arrange
            var strategy = new MedianPriceCalculationStrategy();
            var prices = new List<decimal> { 10m, 20m, 30m, 40m };

            // Act
            var result = strategy.Calculate(prices);

            // Assert
            result.Should().Be(25m);
        }

        [Fact]
        public void WeightedAverageStrategy_Calculate_WithValidPrices_ReturnsWeightedAverage()
        {
            // Arrange
            var strategy = new WeightedAveragePriceCalculationStrategy();
            var prices = new List<decimal> { 10m, 20m, 30m };

            // Act
            var result = strategy.Calculate(prices);

            // Assert
            result.Should().BeGreaterThan(19m).And.BeLessThan(21m);
        }

        [Fact]
        public void TrimmedMeanStrategy_Calculate_WithValidPrices_ReturnsTrimmedMean()
        {
            // Arrange
            var strategy = new TrimmedMeanPriceCalculationStrategy(0.2m);
            var prices = new List<decimal> { 10m, 15m, 20m, 25m, 30m };

            // Act
            var result = strategy.Calculate(prices);

            // Assert
            result.Should().Be(20m);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new double[] { })]
        public void AllStrategies_Calculate_WithNullOrEmptyPrices_ThrowsArgumentException(IEnumerable<double> prices)
        {
            // Arrange
            var strategies = new IPriceCalculationStrategy[]
            {
                new AveragePriceCalculationStrategy(),
                new MedianPriceCalculationStrategy(),
                new WeightedAveragePriceCalculationStrategy(),
                new TrimmedMeanPriceCalculationStrategy()
            };

            // Act & Assert
            foreach (var strategy in strategies)
            {
                var decimalPrices = prices?.Select(p => (decimal)p);
                var action = () => strategy.Calculate(decimalPrices);
                action.Should().Throw<ArgumentException>();
            }
        }

        [Fact]
        public void TrimmedMeanStrategy_Constructor_WithInvalidTrimPercentage_ThrowsArgumentException()
        {
            // Act & Assert
            var action1 = () => new TrimmedMeanPriceCalculationStrategy(-0.1m);
            var action2 = () => new TrimmedMeanPriceCalculationStrategy(0.6m);

            action1.Should().Throw<ArgumentException>();
            action2.Should().Throw<ArgumentException>();
        }
    }
} 