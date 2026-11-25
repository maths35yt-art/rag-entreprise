using RagApi.Models;

namespace RagApi.Services;

public interface IRagClient
{
    Task<RagResponse> AskAsync(string question, CancellationToken cancellationToken);
}