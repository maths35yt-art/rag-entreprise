using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RagApi.Data;
using RagApi.Models;

namespace RagApi.Services;

public class HistoryService : IHistoryService
{
    private readonly RagDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public HistoryService(RagDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<HistoryItem> SaveAsync(string question, RagResponse response, CancellationToken cancellationToken)
    {
        var entity = new ExchangeEntity
        {
            Question = question,
            Answer = response.Answer,
            PassagesJson = JsonSerializer.Serialize(response.Passages),
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
        };

        _dbContext.Exchanges.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToItem(entity);
    }

    public async Task<IReadOnlyList<HistoryItem>> ListAsync(int limit, CancellationToken cancellationToken)
    {
        var entities = await _dbContext.Exchanges
            .AsNoTracking()
            .OrderByDescending(exchange => exchange.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return entities.Select(ToItem).ToList();
    }

    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        await _dbContext.Exchanges.ExecuteDeleteAsync(cancellationToken);
    }

    private static HistoryItem ToItem(ExchangeEntity entity)
    {
        var passages = JsonSerializer.Deserialize<List<RagPassage>>(entity.PassagesJson) ?? new List<RagPassage>();
        return new HistoryItem(entity.Id, entity.Question, entity.Answer, passages, entity.CreatedAtUtc);
    }
}