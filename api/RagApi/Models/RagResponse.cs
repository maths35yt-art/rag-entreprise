namespace RagApi.Models;

public record RagPassage(int Index, double Score, string Text);

public record RagStep(string Kind, string Text);

public record RagResponse(
    string Answer,
    IReadOnlyList<RagPassage> Passages,
    IReadOnlyList<RagStep> Steps);