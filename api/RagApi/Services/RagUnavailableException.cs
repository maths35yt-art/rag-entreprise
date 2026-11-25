namespace RagApi.Services;

public class RagUnavailableException : Exception
{
    public RagUnavailableException(string message) : base(message)
    {
    }

    public RagUnavailableException(string message, Exception innerException) : base(message, innerException)
    {
    }
}