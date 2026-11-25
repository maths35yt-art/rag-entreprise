using System.Net;
using System.Net.Http.Json;
using RagApi.Models;
using RagApi.Services;
using RagApi.Tests.Support;

namespace RagApi.Tests;

public sealed class ChatApiTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public ChatApiTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private Task<HttpResponseMessage> AskAsync(string question)
    {
        return _client.PostAsJsonAsync("/api/chat", new { question });
    }

    private async Task<List<HistoryItem>> GetHistoryAsync(string query = "")
    {
        var items = await _client.GetFromJsonAsync<List<HistoryItem>>("/api/history" + query);
        return items ?? new List<HistoryItem>();
    }

    [Fact]
    public async Task Ask_returns_answer_and_passages()
    {
        var response = await AskAsync("Quelle est la capitale ?");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RagResponse>();
        Assert.NotNull(body);
        Assert.Equal("Réponse à Quelle est la capitale ?", body.Answer);
        Assert.Single(body.Passages);
    }

    [Fact]
    public async Task Ask_forwards_question_to_rag_client()
    {
        await AskAsync("Bonjour");

        Assert.Equal(new[] { "Bonjour" }, _factory.Rag.Questions);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Ask_rejects_blank_question(string question)
    {
        var response = await AskAsync(question);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(_factory.Rag.Questions);
    }

    [Fact]
    public async Task Ask_rejects_too_long_question()
    {
        var response = await AskAsync(new string('a', 2001));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(_factory.Rag.Questions);
    }

    [Fact]
    public async Task Ask_returns_503_and_saves_nothing_when_rag_is_unavailable()
    {
        _factory.Rag.Failure = new RagUnavailableException("service éteint");

        var response = await AskAsync("Bonjour");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Empty(await GetHistoryAsync());
    }

    [Fact]
    public async Task History_lists_newest_first()
    {
        await AskAsync("premiere");
        await AskAsync("deuxieme");

        var items = await GetHistoryAsync();

        Assert.Equal(new[] { "deuxieme", "premiere" }, items.Select(item => item.Question));
    }

    [Fact]
    public async Task History_keeps_passages()
    {
        _factory.Rag.Responder = _ => new RagResponse(
            "ok",
            new[] { new RagPassage(7, 0.5, "alpha"), new RagPassage(9, 0.25, "beta") },
            Array.Empty<RagStep>());

        await AskAsync("Bonjour");
        var items = await GetHistoryAsync();

        Assert.Equal(
            new[] { new RagPassage(7, 0.5, "alpha"), new RagPassage(9, 0.25, "beta") },
            items[0].Passages);
    }

    [Fact]
    public async Task History_respects_limit()
    {
        await AskAsync("premiere");
        await AskAsync("deuxieme");

        var items = await GetHistoryAsync("?limit=1");

        Assert.Single(items);
        Assert.Equal("deuxieme", items[0].Question);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task History_clamps_limit_below_one(int limit)
    {
        await AskAsync("premiere");
        await AskAsync("deuxieme");

        var items = await GetHistoryAsync("?limit=" + limit);

        Assert.Single(items);
    }

    [Fact]
    public async Task History_clear_empties_history()
    {
        await AskAsync("premiere");

        var response = await _client.DeleteAsync("/api/history");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await GetHistoryAsync());
    }
}