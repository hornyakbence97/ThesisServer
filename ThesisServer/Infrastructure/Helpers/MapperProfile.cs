using AutoMapper;
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
                .ForAllOtherMembers(x => x.Ignore());
        }
    }
}
