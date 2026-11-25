namespace RagApi.Data;

public class ExchangeEntity
{
    public int Id { get; set; }

    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public string PassagesJson { get; set; } = "[]";

    public DateTime CreatedAtUtc { get; set; }
}