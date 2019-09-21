using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Xunit;

namespace UnitTests
{
    public class TestApi
    {
        public HttpClient Client { get; }
        public WebSocketClient WebSocketClient { get; }
        public IServiceProvider Services { get; }

        public TestApi()
        {
            var server = new TestFixture();
            Client = server.Client;
            WebSocketClient = server.WebSocketClient;
            Services = server.Services;
        }

        public async Task<TResult> GetAsync<TResult>(
            string relativeUrl,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await Client.GetAsync(relativeUrl);

            Assert.True(response.StatusCode == expectedStatusCode);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<TResult>(content);
            }

            return default;
        }

        public async Task PostAsync(
            string relativeUrl,
            object body,
            HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        {
            var response = await Client
                .PostAsync(relativeUrl,
                    new StringContent(JsonConvert.SerializeObject(body),
                        Encoding.UTF8,
                        "application/json"));

            Assert.True(response.StatusCode == expectedStatusCode);
        }
    }
}
