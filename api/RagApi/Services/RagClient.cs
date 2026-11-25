using System.Text.Json;
using RagApi.Models;

namespace RagApi.Services;

public class RagClient : IRagClient
{
    private readonly HttpClient _httpClient;

    public RagClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<RagResponse> AskAsync(string question, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync("ask", new { question }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new RagUnavailableException(
                    "Le service RAG a répondu avec le code " + (int)response.StatusCode);
            }

            var result = await response.Content.ReadFromJsonAsync<RagResponse>(cancellationToken);
            return result ?? throw new RagUnavailableException("Réponse vide du service RAG");
        }
        catch (HttpRequestException exception)
        {
            throw new RagUnavailableException("Service RAG injoignable", exception);
        }
        catch (JsonException exception)
        {
            throw new RagUnavailableException("Réponse du service RAG illisible", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new RagUnavailableException("Délai dépassé en attendant le service RAG", exception);
        }
    }
}