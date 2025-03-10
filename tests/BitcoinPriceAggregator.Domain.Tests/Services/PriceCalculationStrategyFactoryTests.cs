using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BitcoinPriceAggregator.Domain.Tests.Services
{
    public class PriceCalculationStrategyFactoryTests
    {
        private readonly IPriceCalculationStrategyFactory _factory;

        public PriceCalculationStrategyFactoryTests()
        {
            var services = new ServiceCollection();
            services.AddScoped<AveragePriceCalculationStrategy>();
            services.AddScoped<MedianPriceCalculationStrategy>();
            services.AddScoped<WeightedAveragePriceCalculationStrategy>();
            services.AddScoped<TrimmedMeanPriceCalculationStrategy>();

            var serviceProvider = services.BuildServiceProvider();
            _factory = new PriceCalculationStrategyFactory(serviceProvider);
        }

        [Fact]
        public void CreateStrategy_WithNullOrEmptyName_ReturnsAverageStrategy()
        {
            // Act
            var strategy1 = _factory.CreateStrategy(null);
            var strategy2 = _factory.CreateStrategy(string.Empty);
            var strategy3 = _factory.CreateStrategy("   ");

            // Assert
            strategy1.Should().BeOfType<AveragePriceCalculationStrategy>();
            strategy2.Should().BeOfType<AveragePriceCalculationStrategy>();
            strategy3.Should().BeOfType<AveragePriceCalculationStrategy>();
        }

        [Theory]
        [InlineData("Average")]
        [InlineData("average")]
        [InlineData("AVERAGE")]
        public void CreateStrategy_WithAverageStrategyName_ReturnsAverageStrategy(string strategyName)
        {
            // Act
            var strategy = _factory.CreateStrategy(strategyName);

            // Assert
            strategy.Should().BeOfType<AveragePriceCalculationStrategy>();
        }

        [Theory]
        [InlineData("Median")]
        [InlineData("median")]
        [InlineData("MEDIAN")]
        public void CreateStrategy_WithMedianStrategyName_ReturnsMedianStrategy(string strategyName)
        {
            // Act
            var strategy = _factory.CreateStrategy(strategyName);

            // Assert
            strategy.Should().BeOfType<MedianPriceCalculationStrategy>();
        }

        [Theory]
        [InlineData("WeightedAverage")]
        [InlineData("weightedaverage")]
        [InlineData("WEIGHTEDAVERAGE")]
        public void CreateStrategy_WithWeightedAverageStrategyName_ReturnsWeightedAverageStrategy(string strategyName)
        {
            // Act
            var strategy = _factory.CreateStrategy(strategyName);

            // Assert
            strategy.Should().BeOfType<WeightedAveragePriceCalculationStrategy>();
        }

        [Theory]
        [InlineData("TrimmedMean")]
        [InlineData("trimmedmean")]
        [InlineData("TRIMMEDMEAN")]
        public void CreateStrategy_WithTrimmedMeanStrategyName_ReturnsTrimmedMeanStrategy(string strategyName)
        {
            // Act
            var strategy = _factory.CreateStrategy(strategyName);

            // Assert
            strategy.Should().BeOfType<TrimmedMeanPriceCalculationStrategy>();
        }

        [Fact]
        public void CreateStrategy_WithUnknownStrategyName_ThrowsArgumentException()
        {
            // Act
            var action = () => _factory.CreateStrategy("UnknownStrategy");

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("Unknown strategy name: UnknownStrategy*");
        }

        [Fact]
        public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Act
            var action = () => new PriceCalculationStrategyFactory(null);

            // Assert
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("serviceProvider");
        }
    }
} 