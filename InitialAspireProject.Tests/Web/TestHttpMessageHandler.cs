using System.Text;
using System.Text.Json;

namespace InitialAspireProject.Tests.Web;

internal class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public TestHttpMessageHandler(HttpResponseMessage response)
        : this(_ => response)
    {
    }

    public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_handler(request));

    public static HttpClient CreateJsonClient<T>(T data) where T : class
    {
        var json = JsonSerializer.Serialize(data);
        var handler = new TestHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
    }
}
