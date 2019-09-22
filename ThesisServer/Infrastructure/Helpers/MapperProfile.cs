using AutoMapper;
using Microsoft.AspNetCore.Routing.Constraints;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Model.DTO.Output;

namespace ThesisServer.Infrastructure.Helpers
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<UserEntity, UserDto>()
                .ForMember(
                    dest => dest.Token1, 
                    map => map.MapFrom(source => source.Token1))
                .ForMember(
                    dest => dest.Token2,
                    map => map.MapFrom(source => source.Token2))
                .ForMember(dest => dest.FriendlyName,
                    map => map.MapFrom(src => src.FriendlyName))
                .ForMember(dest => dest.NetworkId, map => map.MapFrom(src => src.NetworkId))
                .ForMember(dest => dest.MaxSpace, map => map.MapFrom(src => src.MaxSpace))
                .ForAllOtherMembers(x => x.Ignore());

            CreateMap<NetworkEntity, NetworkCreateDto>()
                .ForMember(dest => dest.NetworkName, map => map.MapFrom(src => src.NetworkName))
                .ForMember(dest => dest.NetworkId, map => map.MapFrom(src => src.NetworkId))
                .ForAllOtherMembers(dest => dest.Ignore());
        }
    }
}
