using Microsoft.AspNetCore.Mvc;
using RagApi.Models;
using RagApi.Services;

namespace RagApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IRagClient _ragClient;
    private readonly IHistoryService _historyService;

    public ChatController(IRagClient ragClient, IHistoryService historyService)
    {
        _ragClient = ragClient;
        _historyService = historyService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(RagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ask(AskRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _ragClient.AskAsync(request.Question, cancellationToken);
            await _historyService.SaveAsync(request.Question, response, cancellationToken);
            return Ok(response);
        }
        catch (RagUnavailableException exception)
        {
            return Problem(
                title: "Service RAG indisponible",
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}