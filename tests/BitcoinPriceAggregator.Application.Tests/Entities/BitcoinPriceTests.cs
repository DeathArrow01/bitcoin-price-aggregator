using System;
using FluentAssertions;
using Xunit;
using BitcoinPriceAggregator.Domain.Entities;

namespace BitcoinPriceAggregator.Application.Tests.Entities
{
    public class BitcoinPriceTests
    {
        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange
            var pair = "BTC/USD";
            var price = 45000.50m;
            var timestamp = DateTimeOffset.UtcNow;
            var expectedTicks = new DateTimeOffset(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, 0, 0, TimeSpan.Zero).Ticks;

            // Act
            var bitcoinPrice = BitcoinPrice.CreateBuilder()
                .WithPair(pair)
                .WithPrice(price)
                .WithUtcTicks(timestamp.Ticks)
                .Build();

            // Assert
            bitcoinPrice.Should().NotBeNull();
            bitcoinPrice.Pair.Should().Be(pair);
            bitcoinPrice.Price.Should().Be(price);
            bitcoinPrice.UtcTicks.Should().Be(expectedTicks);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void Constructor_WithInvalidPair_ThrowsArgumentException(string pair)
        {
            // Arrange
            var price = 45000.50m;
            var timestamp = DateTimeOffset.UtcNow;

            // Act & Assert
            var act = () => BitcoinPrice.CreateBuilder()
                .WithPair(pair)
                .WithPrice(price)
                .WithUtcTicks(timestamp.Ticks)
                .Build();

            act.Should().Throw<ArgumentException>()
                .WithMessage("Pair cannot be null or empty. (Parameter 'pair')");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WithInvalidPrice_ThrowsArgumentException(decimal price)
        {
            // Arrange
            var pair = "BTC/USD";
            var timestamp = DateTimeOffset.UtcNow;

            // Act & Assert
            var act = () => BitcoinPrice.CreateBuilder()
                .WithPair(pair)
                .WithPrice(price)
                .WithUtcTicks(timestamp.Ticks)
                .Build();

            act.Should().Throw<ArgumentException>()
                .WithMessage("Price must be greater than zero. (Parameter 'price')");
        }

        [Fact]
        public void Constructor_WithLocalTimestamp_ConvertsToUtc()
        {
            // Arrange
            var pair = "BTC/USD";
            var price = 45000.50m;
            var localTimestamp = DateTimeOffset.Now;
            var utcTimestamp = localTimestamp.ToUniversalTime();
            var expectedTicks = new DateTimeOffset(utcTimestamp.Year, utcTimestamp.Month, utcTimestamp.Day, utcTimestamp.Hour, 0, 0, TimeSpan.Zero).Ticks;

            // Act
            var bitcoinPrice = BitcoinPrice.CreateBuilder()
                .WithPair(pair)
                .WithPrice(price)
                .WithUtcTicks(localTimestamp.ToUniversalTime().Ticks)
                .Build();

            // Assert
            bitcoinPrice.Should().NotBeNull();
            bitcoinPrice.UtcTicks.Should().Be(expectedTicks);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void UpdatePrice_WithInvalidPrice_ThrowsArgumentException(decimal newPrice)
        {
            // Arrange
            var pair = "BTC/USD";
            var price = 45000.50m;
            var timestamp = DateTimeOffset.UtcNow;
            var bitcoinPrice = BitcoinPrice.CreateBuilder()
                .WithPair(pair)
                .WithPrice(price)
                .WithUtcTicks(timestamp.Ticks)
                .Build();

            // Act & Assert
            var act = () => bitcoinPrice.UpdatePrice(newPrice);

            act.Should().Throw<ArgumentException>()
                .WithMessage("Price must be greater than zero. (Parameter 'newPrice')");
        }

        [Fact]
        public void UpdatePrice_WithValidPrice_UpdatesPrice()
        {
            // Arrange
            var pair = "BTC/USD";
            var price = 45000.50m;
            var timestamp = DateTimeOffset.UtcNow;
            var bitcoinPrice = BitcoinPrice.CreateBuilder()
                .WithPair(pair)
                .WithPrice(price)
                .WithUtcTicks(timestamp.Ticks)
                .Build();

            var newPrice = 46000.50m;

            // Act
            bitcoinPrice.UpdatePrice(newPrice);

            // Assert
            bitcoinPrice.Price.Should().Be(newPrice);
        }
    }
} 