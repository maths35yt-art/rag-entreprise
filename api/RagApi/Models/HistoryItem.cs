namespace RagApi.Models;

public record HistoryItem(
    int Id,
    string Question,
    string Answer,
    IReadOnlyList<RagPassage> Passages,
    DateTime CreatedAtUtc);