using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using ThesisServer;

namespace UnitTests
{
    public class TestFixture : IDisposable
    {
        private readonly TestServer _server;

        public TestFixture()
        {
            var builder = new WebHostBuilder()
                .UseEnvironment("Development")
                .ConfigureAppConfiguration(
                    (builderContext, config) =>
                    {
                        var env = builderContext.HostingEnvironment;
                        config.Sources.Clear();
                        config.SetBasePath(env.ContentRootPath);
                        config.AddEnvironmentVariables();
                        config.AddJsonFile(@"C:\Users\Hornyák Bence\source\repos\ThesisServer\ThesisServer\bin\Debug\netcoreapp2.1\appsettings.json", optional: false);
                        config.AddJsonFile($@"C:\Users\Hornyák Bence\source\repos\ThesisServer\ThesisServer\bin\Debug\netcoreapp2.1\appsettings.{env.EnvironmentName}.json", optional: false);
                    })
                .UseStartup<Startup>();

            _server = new TestServer(builder);

            Client = _server.CreateClient();
            WebSocketClient = _server.CreateWebSocketClient();
            Client.BaseAddress = _server.BaseAddress;
            Services = _server.Host.Services;
        }

        public HttpClient Client { get; }
        public WebSocketClient WebSocketClient { get; }
        public IServiceProvider Services { get; }

        public void Dispose()
        {
            Client.Dispose();
            _server.Dispose();
        }
    }
}
