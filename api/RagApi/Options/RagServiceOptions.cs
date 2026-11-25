namespace RagApi.Options;

public class RagServiceOptions
{
    public const string SectionName = "RagService";

    public string BaseUrl { get; set; } = "http://localhost:8000/";

    public int TimeoutSeconds { get; set; } = 300;
}