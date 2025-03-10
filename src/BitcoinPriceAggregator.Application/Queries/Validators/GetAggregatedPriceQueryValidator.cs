using System;
using FluentValidation;
using System.Text.RegularExpressions;

namespace BitcoinPriceAggregator.Application.Queries.Validators
{
    public class GetAggregatedPriceQueryValidator : AbstractValidator<GetAggregatedPriceQuery>
    {
        private static readonly Regex PairFormatRegex = new(@"^[A-Za-z]{3}[/-][A-Za-z]{3}$", RegexOptions.Compiled);

        public GetAggregatedPriceQueryValidator()
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

            RuleFor(x => x.UtcTicks)
                .GreaterThan(0)
                .WithMessage("Timestamp must be greater than 0")
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
                .WithMessage("Timestamp cannot be in the future");
        }
    }
}