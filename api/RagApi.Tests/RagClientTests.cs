using System.Net;
using System.Text;
using RagApi.Services;
using RagApi.Tests.Support;

namespace RagApi.Tests;

public class RagClientTests
{
    private static RagClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        var httpClient = new HttpClient(new StubHandler(responder))
        {
            BaseAddress = new Uri("http://rag.test/")
        };
        return new RagClient(httpClient);
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public async Task AskAsync_sends_question_and_returns_parsed_response()
    {
        HttpMethod? method = null;
        string? path = null;
        string? body = null;

        var client = CreateClient(async (request, _) =>
        {
            method = request.Method;
            path = request.RequestUri?.AbsolutePath;
            body = await request.Content!.ReadAsStringAsync();
            return Json("{\"answer\":\"Hanoï\",\"passages\":[{\"index\":3,\"score\":0.75,\"text\":\"Texte\"}],\"steps\":[{\"kind\":\"tool\",\"text\":\"get_weather\"}]}");
        });

        var response = await client.AskAsync("Bonjour", CancellationToken.None);

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("/ask", path);
        Assert.Contains("\"question\":\"Bonjour\"", body);
        Assert.Equal("Hanoï", response.Answer);
        Assert.Equal(3, response.Passages[0].Index);
        Assert.Equal(0.75, response.Passages[0].Score);
        Assert.Equal("Texte", response.Passages[0].Text);
        Assert.Equal("tool", response.Steps[0].Kind);
    }

    [Fact]
    public async Task AskAsync_throws_when_service_returns_error_status()
    {
        var client = CreateClient((_, _) => Task.FromResult(Json("{}", HttpStatusCode.InternalServerError)));

        await Assert.ThrowsAsync<RagUnavailableException>(() => client.AskAsync("Bonjour", CancellationToken.None));
    }

    [Fact]
    public async Task AskAsync_throws_when_service_is_unreachable()
    {
        var client = CreateClient((_, _) => throw new HttpRequestException("connexion refusée"));

        await Assert.ThrowsAsync<RagUnavailableException>(() => client.AskAsync("Bonjour", CancellationToken.None));
    }

    [Fact]
    public async Task AskAsync_throws_when_body_is_not_valid_json()
    {
        var client = CreateClient((_, _) => Task.FromResult(Json("pas du json")));

        await Assert.ThrowsAsync<RagUnavailableException>(() => client.AskAsync("Bonjour", CancellationToken.None));
    }

    [Fact]
    public async Task AskAsync_throws_when_body_is_empty_json()
    {
        var client = CreateClient((_, _) => Task.FromResult(Json("null")));

        await Assert.ThrowsAsync<RagUnavailableException>(() => client.AskAsync("Bonjour", CancellationToken.None));
    }

    [Fact]
    public async Task AskAsync_throws_when_request_times_out()
    {
        var client = CreateClient((_, _) => throw new TaskCanceledException("délai dépassé"));

        await Assert.ThrowsAsync<RagUnavailableException>(() => client.AskAsync("Bonjour", CancellationToken.None));
    }

    [Fact]
    public async Task AskAsync_propagates_cancellation_requested_by_caller()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

        var client = CreateClient((_, token) =>
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(Json("{}"));
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.AskAsync("Bonjour", source.Token));
    }
}