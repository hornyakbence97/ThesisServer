using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ThesisServer.Infrastructure.Configuration;
using ThesisServer.Infrastructure.Middleware.Helper;

namespace ThesisServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            RegisterOptions.Register(services, Configuration);

            var provider = services.BuildServiceProvider();

            var dataBaseOption = provider.GetService<IOptions<DatabaseOption>>().Value;

            RegisterServices.Register(services, dataBaseOption);

            RegisterMappers.Register(services);

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IOptions<WebSocketOption> webSocketOptions)
        {
            app.UseDeveloperExceptionPage();

            app.UseHandledExceptionHandler();

            app.UseHttpsRedirection();

            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMilliseconds(webSocketOptions.Value.ReceiveBufferSize),
                ReceiveBufferSize = webSocketOptions.Value.KeepAliveIntervalInMillisecs
            });

            app.UseWebSocketHandler();

            app.UseMvc();
        }
    }
}
