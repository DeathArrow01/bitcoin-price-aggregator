using System;
using MediatR;
using BitcoinPriceAggregator.Application.DTOs;

namespace BitcoinPriceAggregator.Application.Queries
{
    public record GetAggregatedPriceQuery : IRequest<BitcoinPriceDto>
    {
        public long UtcTicks { get; init; }
        public string Pair { get; init; } = "BTC/USD";

        public GetAggregatedPriceQuery(long utcTicks, string pair)
        {
            UtcTicks = utcTicks;
            Pair = pair;
        }
    }
} 