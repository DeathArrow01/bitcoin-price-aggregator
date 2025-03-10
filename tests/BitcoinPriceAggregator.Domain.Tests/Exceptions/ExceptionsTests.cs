using System;
using Xunit;
using FluentAssertions;
using BitcoinPriceAggregator.Domain.Exceptions;

namespace BitcoinPriceAggregator.Domain.Tests.Exceptions
{
    public class ExceptionsTests
    {
        [Fact]
        public void BitcoinPriceException_WithMessage_SetsMessageCorrectly()
        {
            // Arrange
            var message = "Test error message";

            // Act
            var exception = new BitcoinPriceException(message);

            // Assert
            exception.Message.Should().Be(message);
        }

        [Fact]
        public void ExternalApiException_WithProviderAndMessage_FormatsMessageCorrectly()
        {
            // Arrange
            var provider = "Bitstamp";
            var message = "API timeout";

            // Act
            var exception = new ExternalApiException(provider, message);

            // Assert
            exception.Message.Should().Be($"Provider {provider}: {message}");
            exception.Provider.Should().Be(provider);
        }

        [Fact]
        public void ExternalApiException_WithInnerException_PreservesInnerException()
        {
            // Arrange
            var provider = "Bitfinex";
            var message = "Connection failed";
            var innerException = new TimeoutException();

            // Act
            var exception = new ExternalApiException(provider, message, innerException);

            // Assert
            exception.InnerException.Should().BeSameAs(innerException);
            exception.Provider.Should().Be(provider);
        }

        [Fact]
        public void InvalidPriceDataException_WithPairAndTimestamp_FormatsMessageCorrectly()
        {
            // Arrange
            var pair = "BTC/USD";
            var timestamp = DateTime.UtcNow;
            var message = "Price cannot be negative";

            // Act
            var exception = new InvalidPriceDataException(pair, timestamp, message);

            // Assert
            exception.Message.Should().Contain(pair);
            exception.Message.Should().Contain(timestamp.ToString("u"));
            exception.Message.Should().Contain(message);
            exception.Pair.Should().Be(pair);
            exception.Timestamp.Should().Be(timestamp);
        }
    }
} 