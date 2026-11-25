using RagApi.Models;
using RagApi.Services;

namespace RagApi.Tests.Support;

public sealed class FakeRagClient : IRagClient
{
    public Exception? Failure { get; set; }

    public List<string> Questions { get; } = new();

    public Func<string, RagResponse> Responder { get; set; } = question => new RagResponse(
        "Réponse à " + question,
        new[] { new RagPassage(0, 0.9, "Passage de test") },
        Array.Empty<RagStep>());

    public Task<RagResponse> AskAsync(string question, CancellationToken cancellationToken)
    {
        Questions.Add(question);

        if (Failure is not null)
        {
            throw Failure;
        }

        return Task.FromResult(Responder(question));
    }
}