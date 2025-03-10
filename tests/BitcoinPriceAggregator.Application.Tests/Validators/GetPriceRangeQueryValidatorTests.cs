using System;
using Xunit;
using FluentAssertions;
using FluentValidation.TestHelper;
using BitcoinPriceAggregator.Application.Queries;
using BitcoinPriceAggregator.Application.Queries.Validators;

namespace BitcoinPriceAggregator.Application.Tests.Validators
{
    public class GetPriceRangeQueryValidatorTests
    {
        private readonly GetPriceRangeQueryValidator _validator;

        public GetPriceRangeQueryValidatorTests()
        {
            _validator = new GetPriceRangeQueryValidator();
        }

        [Fact]
        public void Validate_WhenTimeRangeIsValid_ShouldNotHaveError()
        {
            // Arrange
            var endTime = DateTimeOffset.UtcNow.AddMinutes(-5);
            var startTime = endTime.AddHours(-1);
            var query = new GetPriceRangeQuery
            {
                StartTicks = startTime.Ticks,
                EndTicks = endTime.Ticks,
                Pair = "BTC/USD"
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.StartTicks);
            result.ShouldNotHaveValidationErrorFor(x => x.EndTicks);
        }

        [Fact]
        public void Validate_WhenStartTimeIsAfterEndTime_ShouldHaveError()
        {
            // Arrange
            var startTime = DateTimeOffset.UtcNow.AddMinutes(-5);
            var endTime = startTime.AddHours(-1);
            var query = new GetPriceRangeQuery
            {
                StartTicks = startTime.Ticks,
                EndTicks = endTime.Ticks,
                Pair = "BTC/USD"
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.EndTicks)
                .WithErrorMessage("End time must be after start time");
        }

        [Fact]
        public void Validate_WhenTimeRangeExceedsMaximum_ShouldHaveError()
        {
            // Arrange
            var endTime = DateTimeOffset.UtcNow.AddMinutes(-5);
            var startTime = endTime.AddDays(-31); // More than 30 days
            var query = new GetPriceRangeQuery
            {
                StartTicks = startTime.Ticks,
                EndTicks = endTime.Ticks,
                Pair = "BTC/USD"
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.StartTicks)
                .WithErrorMessage("Time range cannot exceed 30 days");
        }

        [Fact]
        public void Validate_WhenEndTimeIsInFuture_ShouldHaveError()
        {
            // Arrange
            var endTime = DateTimeOffset.UtcNow.AddMinutes(5);
            var startTime = endTime.AddHours(-1);
            var query = new GetPriceRangeQuery
            {
                StartTicks = startTime.Ticks,
                EndTicks = endTime.Ticks,
                Pair = "BTC/USD"
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.EndTicks)
                .WithErrorMessage("End time cannot be in the future");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("BTC")]
        [InlineData("ETH/USD")]
        [InlineData("BTC/EUR")]
        public void Validate_WhenPairIsInvalid_ShouldHaveError(string pair)
        {
            // Arrange
            var endTime = DateTimeOffset.UtcNow.AddMinutes(-5);
            var startTime = endTime.AddHours(-1);
            var query = new GetPriceRangeQuery
            {
                StartTicks = startTime.Ticks,
                EndTicks = endTime.Ticks,
                Pair = pair
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Pair)
                .WithErrorMessage("Only BTC/USD pair is supported");
        }
    }
} 