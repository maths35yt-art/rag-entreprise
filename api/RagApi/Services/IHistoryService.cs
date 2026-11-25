using RagApi.Models;

namespace RagApi.Services;

public interface IHistoryService
{
    Task<HistoryItem> SaveAsync(string question, RagResponse response, CancellationToken cancellationToken);

    Task<IReadOnlyList<HistoryItem>> ListAsync(int limit, CancellationToken cancellationToken);

    Task ClearAsync(CancellationToken cancellationToken);
}