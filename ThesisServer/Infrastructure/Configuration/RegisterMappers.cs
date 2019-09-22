using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using ThesisServer.Infrastructure.Helpers;

namespace ThesisServer.Infrastructure.Configuration
{
    public static class RegisterMappers
    {
        public static void Register(IServiceCollection services)
        {
            var mappingConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfile(new MapperProfile());
            });

            IMapper mapper = mappingConfig.CreateMapper();

            services.AddSingleton(mapper);
        }
    }
}
