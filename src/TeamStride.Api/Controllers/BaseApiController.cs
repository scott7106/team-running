using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace TeamStride.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected readonly ILogger<BaseApiController> Logger;

    protected BaseApiController(ILogger<BaseApiController> logger)
    {
        Logger = logger;
    }

    protected IActionResult HandleError(Exception ex, string message = "An error occurred while processing the request.")
    {
        Logger.LogError(ex, message);
        var traceId = HttpContext?.TraceIdentifier ?? "N/A";
        return StatusCode(500, new { message = "An unexpected error occurred.", traceId });
    }

    protected IActionResult NotFound(string message = "Resource not found")
    {
        var traceId = HttpContext?.TraceIdentifier ?? "N/A";
        return base.NotFound(new { message, traceId });
    }

    protected IActionResult BadRequest(string message)
    {
        var traceId = HttpContext?.TraceIdentifier ?? "N/A";
        return base.BadRequest(new { message, traceId });
    }

    protected IActionResult Forbid(string message)
    {
        var traceId = HttpContext?.TraceIdentifier ?? "N/A";
        return base.Forbid();
    }
} 