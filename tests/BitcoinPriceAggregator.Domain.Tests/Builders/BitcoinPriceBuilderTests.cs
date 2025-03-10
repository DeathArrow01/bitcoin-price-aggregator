using BitcoinPriceAggregator.Domain.Builders;
using FluentAssertions;
using Xunit;

namespace BitcoinPriceAggregator.Domain.Tests.Builders
{
    public class BitcoinPriceBuilderTests
    {
        private static DateTime NormalizeToHourPrecision(DateTime timestamp)
        {
            return new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, 0, 0, DateTimeKind.Utc);
        }

        [Fact]
        public void Build_WithValidValues_CreatesCorrectBitcoinPrice()
        {
            // Arrange
            var price = 50000m;
            var pair = "BTC/USD";
            var timestamp = new DateTime(2025, 3, 9, 15, 30, 45, DateTimeKind.Utc);
            var normalizedTimestamp = NormalizeToHourPrecision(timestamp);
            var provider = "Bitstamp";

            // Act
            var bitcoinPrice = BitcoinPriceBuilder.Create()
                .WithPrice(price)
                .WithPair(pair)
                .WithTimestamp(timestamp)
                .WithProvider(provider)
                .Build();

            // Assert
            bitcoinPrice.Price.Should().Be(price);
            bitcoinPrice.Pair.Should().Be(pair);
            bitcoinPrice.Timestamp.Should().Be(normalizedTimestamp);
            bitcoinPrice.Provider.Should().Be(provider);
        }

        [Fact]
        public void WithTimestamp_WithNonUtcTimestamp_NormalizesToUtc()
        {
            // Arrange
            var localTimestamp = new DateTime(2025, 3, 9, 15, 30, 45, DateTimeKind.Local);
            var expectedUtcTimestamp = NormalizeToHourPrecision(localTimestamp.ToUniversalTime());

            // Act
            var bitcoinPrice = BitcoinPriceBuilder.Create()
                .WithPrice(50000m)
                .WithPair("BTC/USD")
                .WithTimestamp(localTimestamp)
                .WithProvider("Bitstamp")
                .Build();

            // Assert
            bitcoinPrice.Timestamp.Should().Be(expectedUtcTimestamp);
            bitcoinPrice.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void WithPrice_WithInvalidPrice_ThrowsArgumentException(decimal price)
        {
            // Act
            var action = () => BitcoinPriceBuilder.Create().WithPrice(price);

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("Price must be greater than zero*");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void WithPair_WithInvalidPair_ThrowsArgumentException(string pair)
        {
            // Act
            var action = () => BitcoinPriceBuilder.Create().WithPair(pair);

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("Pair cannot be null or empty*");
        }

        [Fact]
        public void WithTimestamp_WithDefaultTimestamp_ThrowsArgumentException()
        {
            // Act
            var action = () => BitcoinPriceBuilder.Create().WithTimestamp(default);

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("Timestamp must be set*");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void WithProvider_WithInvalidProvider_ThrowsArgumentException(string provider)
        {
            // Act
            var action = () => BitcoinPriceBuilder.Create().WithProvider(provider);

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("Provider cannot be null or empty*");
        }

        [Fact]
        public void Build_WithMissingValues_ThrowsInvalidOperationException()
        {
            // Act
            var action = () => BitcoinPriceBuilder.Create().Build();

            // Assert
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot build BitcoinPrice due to validation errors:*");
        }

        [Fact]
        public void Build_WithPartialValues_ThrowsInvalidOperationException()
        {
            // Act
            var action = () => BitcoinPriceBuilder.Create()
                .WithPrice(50000m)
                .WithPair("BTC/USD")
                .Build();

            // Assert
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot build BitcoinPrice due to validation errors:*")
                .And.Message.Should().Contain("Timestamp must be set")
                .And.Contain("Provider must be set");
        }
    }
} 