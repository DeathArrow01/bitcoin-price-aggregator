using System;
using FluentValidation;
using System.Text.RegularExpressions;

namespace BitcoinPriceAggregator.Application.Queries.Validators
{
    public class GetPriceRangeQueryValidator : AbstractValidator<GetPriceRangeQuery>
    {
        private static readonly Regex PairFormatRegex = new(@"^[A-Za-z]{3}[/-][A-Za-z]{3}$", RegexOptions.Compiled);

        public GetPriceRangeQueryValidator()
        {
            // Configure validator to continue validation after first failure
            ClassLevelCascadeMode = CascadeMode.Continue;

            RuleFor(x => x.Pair)
                .NotEmpty()
                .WithMessage("Trading pair must be specified")
                .Must(pair => PairFormatRegex.IsMatch(pair))
                .WithMessage("Trading pair must be in format 'XXX/YYY'")
                .Must(pair => pair.ToUpperInvariant() == "BTC/USD" || pair.ToUpperInvariant() == "BTC-USD")
                .WithMessage("Only BTC/USD pair is supported");

            RuleFor(x => x.StartTicks)
                .GreaterThan(0)
                .WithMessage("Start time must be greater than 0")
                .Must(ticks =>
                {
                    try
                    {
                        var timestamp = new DateTimeOffset(ticks, TimeSpan.Zero);
                        return timestamp <= DateTimeOffset.UtcNow;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .WithMessage("Start time cannot be in the future");

            RuleFor(x => x.EndTicks)
                .GreaterThan(0)
                .WithMessage("End time must be greater than 0")
                .Must(ticks =>
                {
                    try
                    {
                        var timestamp = new DateTimeOffset(ticks, TimeSpan.Zero);
                        return timestamp <= DateTimeOffset.UtcNow;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .WithMessage("End time cannot be in the future");

            RuleFor(x => x.EndTicks)
                .Must((query, endTicks) =>
                {
                    try
                    {
                        var startTime = new DateTimeOffset(query.StartTicks, TimeSpan.Zero);
                        var endTime = new DateTimeOffset(endTicks, TimeSpan.Zero);
                        return startTime <= endTime;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .WithMessage("End time must be after start time");

            RuleFor(x => x.StartTicks)
                .Must((query, startTicks) =>
                {
                    try
                    {
                        var startTime = new DateTimeOffset(startTicks, TimeSpan.Zero);
                        var endTime = new DateTimeOffset(query.EndTicks, TimeSpan.Zero);
                        return (endTime - startTime).TotalDays <= 30;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .WithMessage("Time range cannot exceed 30 days");
        }
    }
} 