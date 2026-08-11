using System.Net;

namespace FreelaHunter.Infrastructure.Tests;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public List<HttpRequestMessage> Requests { get; } = [];

    public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return await _handler(request, cancellationToken);
    }

    public static FakeHttpMessageHandler ReturningAnalysisJson(
        string analysisJson,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new FakeHttpMessageHandler(async (request, ct) =>
        {
            await Task.CompletedTask;
            var serialized = System.Text.Json.JsonSerializer.Serialize(analysisJson);
            var chatResponse = "{\"id\":\"chatcmpl-test\",\"choices\":[{\"message\":{\"content\":" + serialized + "}}],\"usage\":{\"prompt_tokens\":100,\"completion_tokens\":200,\"total_tokens\":300}}";
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(chatResponse, System.Text.Encoding.UTF8, "application/json")
            };
        });
    }

    public static FakeHttpMessageHandler ReturningRawJson(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new FakeHttpMessageHandler(async (request, ct) =>
        {
            await Task.CompletedTask;
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });
    }

    public static FakeHttpMessageHandler ReturningStatus(HttpStatusCode statusCode)
    {
        return new FakeHttpMessageHandler(async (request, ct) =>
        {
            await Task.CompletedTask;
            return new HttpResponseMessage(statusCode);
        });
    }

    public static FakeHttpMessageHandler Throwing<TException>() where TException : Exception, new()
    {
        return new FakeHttpMessageHandler((request, ct) =>
        {
            throw new TException();
        });
    }

    public static FakeHttpMessageHandler TimingOut()
    {
        return new FakeHttpMessageHandler((request, ct) =>
        {
            throw new OperationCanceledException(ct);
        });
    }
}
