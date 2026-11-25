using Microsoft.AspNetCore.Mvc;
using RagApi.Models;
using RagApi.Services;

namespace RagApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HistoryController : ControllerBase
{
    private const int MaxLimit = 200;

    private readonly IHistoryService _historyService;

    public HistoryController(IHistoryService historyService)
    {
        _historyService = historyService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<HistoryItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var items = await _historyService.ListAsync(Math.Clamp(limit, 1, MaxLimit), cancellationToken);
        return Ok(items);
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        await _historyService.ClearAsync(cancellationToken);
        return NoContent();
    }
}