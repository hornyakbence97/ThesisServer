using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ThesisServer.Infrastructure.Configuration
{
    public static class RegisterOptions
    {
        public static void Register(IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();
            services.Configure<WebSocketOption>(configuration.GetSection("WebSocket"));
            services.Configure<DatabaseOption>(configuration.GetSection("DataBase"));
        }
    }
}
