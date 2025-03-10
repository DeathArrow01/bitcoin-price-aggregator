using AutoMapper;
using BitcoinPriceAggregator.Application.DTOs;
using BitcoinPriceAggregator.Domain.Entities;

namespace BitcoinPriceAggregator.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<BitcoinPrice, BitcoinPriceDto>()
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => new DateTime(src.UtcTicks, DateTimeKind.Utc)))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.Pair, opt => opt.MapFrom(src => src.Pair));
        }
    }
} 