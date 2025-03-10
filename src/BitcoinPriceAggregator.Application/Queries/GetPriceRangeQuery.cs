using System;
using System.Collections.Generic;
using MediatR;
using BitcoinPriceAggregator.Application.DTOs;

namespace BitcoinPriceAggregator.Application.Queries
{
    public class GetPriceRangeQuery : IRequest<IEnumerable<BitcoinPriceDto>>
    {
        public long StartTicks { get; set; }
        public long EndTicks { get; set; }
        public required string Pair { get; set; }
    }
} 