namespace RagApi.Tests.Support;

public sealed class StubHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

    public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        _responder = responder;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return _responder(request, cancellationToken);
    }
}