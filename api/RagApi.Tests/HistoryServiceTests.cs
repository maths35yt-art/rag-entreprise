using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RagApi.Data;
using RagApi.Models;
using RagApi.Services;
using RagApi.Tests.Support;

namespace RagApi.Tests;

public sealed class HistoryServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection;
    private readonly RagDbContext _dbContext;
    private readonly HistoryService _service;

    public HistoryServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<RagDbContext>().UseSqlite(_connection).Options;
        _dbContext = new RagDbContext(options);
        _dbContext.Database.EnsureCreated();
        _service = new HistoryService(_dbContext, new FixedTimeProvider(Now));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private static RagResponse Response(string answer, params RagPassage[] passages)
    {
        return new RagResponse(answer, passages, Array.Empty<RagStep>());
    }

    [Fact]
    public async Task SaveAsync_returns_item_with_id_and_fixed_timestamp()
    {
        var item = await _service.SaveAsync("Q1", Response("A1", new RagPassage(1, 0.5, "texte")), CancellationToken.None);

        Assert.True(item.Id > 0);
        Assert.Equal("Q1", item.Question);
        Assert.Equal("A1", item.Answer);
        Assert.Equal(Now.UtcDateTime, item.CreatedAtUtc);
        Assert.Single(item.Passages);
    }

    [Fact]
    public async Task ListAsync_returns_newest_first()
    {
        await _service.SaveAsync("A", Response("rA"), CancellationToken.None);
        await _service.SaveAsync("B", Response("rB"), CancellationToken.None);
        await _service.SaveAsync("C", Response("rC"), CancellationToken.None);

        var items = await _service.ListAsync(10, CancellationToken.None);

        Assert.Equal(new[] { "C", "B", "A" }, items.Select(item => item.Question));
    }

    [Fact]
    public async Task ListAsync_respects_limit()
    {
        await _service.SaveAsync("A", Response("rA"), CancellationToken.None);
        await _service.SaveAsync("B", Response("rB"), CancellationToken.None);
        await _service.SaveAsync("C", Response("rC"), CancellationToken.None);

        var items = await _service.ListAsync(2, CancellationToken.None);

        Assert.Equal(2, items.Count);
        Assert.Equal("C", items[0].Question);
    }

    [Fact]
    public async Task ListAsync_restores_passages_exactly()
    {
        var passages = new[]
        {
            new RagPassage(12, 0.734, "Hô Chi Minh-Ville est la plus grande ville."),
            new RagPassage(4, 0.5, "Hanoï est la capitale.")
        };
        await _service.SaveAsync("Q", Response("R", passages), CancellationToken.None);

        var items = await _service.ListAsync(10, CancellationToken.None);

        Assert.Equal(passages, items[0].Passages);
    }

    [Fact]
    public async Task ClearAsync_removes_all_items()
    {
        await _service.SaveAsync("A", Response("rA"), CancellationToken.None);
        await _service.SaveAsync("B", Response("rB"), CancellationToken.None);

        await _service.ClearAsync(CancellationToken.None);

        Assert.Empty(await _service.ListAsync(10, CancellationToken.None));
    }
}