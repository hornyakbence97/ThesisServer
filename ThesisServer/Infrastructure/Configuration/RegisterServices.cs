using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ThesisServer.BL.Services;
using ThesisServer.Data.Repository.Db;
using ThesisServer.Data.Repository.Memory;

namespace ThesisServer.Infrastructure.Configuration
{
    public static class RegisterServices
    {
        public static void Register(IServiceCollection services, DatabaseOption databaseOption)
        {
            services.AddDbContext<VirtualNetworkDbContext>(options =>
                options.UseSqlServer(databaseOption.ConnectionString));

            services.AddSingleton<IWebSocketRepository, WebSocketRepository>();
            services.AddScoped<IWebSocketHandler, WebSocketHandler>();
            services.AddSingleton<DebugRepository>();
            services.AddScoped<INetworkService, NetworkService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IFileService, FileService>();
            services.AddSingleton<OnlineUserRepository>();
        }
    }
}