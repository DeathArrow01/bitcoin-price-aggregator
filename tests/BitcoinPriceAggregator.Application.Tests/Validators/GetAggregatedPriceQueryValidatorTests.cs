using System;
using Xunit;
using FluentAssertions;
using FluentValidation.TestHelper;
using BitcoinPriceAggregator.Application.Queries;
using BitcoinPriceAggregator.Application.Queries.Validators;

namespace BitcoinPriceAggregator.Application.Tests.Validators
{
    public class GetAggregatedPriceQueryValidatorTests
    {
        private readonly GetAggregatedPriceQueryValidator _validator;

        public GetAggregatedPriceQueryValidatorTests()
        {
            _validator = new GetAggregatedPriceQueryValidator();
        }

        private static DateTimeOffset NormalizeToHourPrecision(DateTimeOffset timestamp)
        {
            return new DateTimeOffset(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, 0, 0, timestamp.Offset);
        }

        [Theory]
        [InlineData("BTC/USD")]
        [InlineData("BTC-USD")]
        [InlineData("btc/usd")]
        [InlineData("btc-usd")]
        public async Task Validate_WithValidPair_ShouldNotHaveValidationError(string pair)
        {
            // Arrange
            var timestamp = NormalizeToHourPrecision(DateTimeOffset.UtcNow.AddMinutes(-1));
            var query = new GetAggregatedPriceQuery(timestamp.Ticks, pair);

            // Act
            var result = await _validator.TestValidateAsync(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Pair);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        [InlineData("BTC")]
        [InlineData("ETH/USD")]
        [InlineData("BTC/EUR")]
        [InlineData("BTC/US")]
        [InlineData("BT/USD")]
        public async Task Validate_WithInvalidPair_ShouldHaveValidationError(string pair)
        {
            // Arrange
            var timestamp = NormalizeToHourPrecision(DateTimeOffset.UtcNow.AddMinutes(-1));
            var query = new GetAggregatedPriceQuery(timestamp.Ticks, pair);

            // Act
            var result = await _validator.TestValidateAsync(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Pair);
        }

        [Fact]
        public async Task Validate_WithValidTimestamp_ShouldNotHaveValidationError()
        {
            // Arrange
            var timestamp = NormalizeToHourPrecision(DateTimeOffset.UtcNow.AddMinutes(-1));
            var query = new GetAggregatedPriceQuery(timestamp.Ticks, "BTC/USD");

            // Act
            var result = await _validator.TestValidateAsync(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.UtcTicks);
        }

        [Fact]
        public async Task Validate_WithDefaultTimestamp_ShouldHaveValidationError()
        {
            // Arrange
            var query = new GetAggregatedPriceQuery(0, "BTC/USD");

            // Act
            var result = await _validator.TestValidateAsync(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UtcTicks)
                .WithErrorMessage("Timestamp must be greater than 0");
        }

        [Fact]
        public async Task Validate_WithFutureTimestamp_ShouldHaveValidationError()
        {
            // Arrange
            var timestamp = NormalizeToHourPrecision(DateTimeOffset.UtcNow.AddDays(1));
            var query = new GetAggregatedPriceQuery(timestamp.Ticks, "BTC/USD");

            // Act
            var result = await _validator.TestValidateAsync(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.UtcTicks)
                .WithErrorMessage("Timestamp cannot be in the future");
        }
    }
} 