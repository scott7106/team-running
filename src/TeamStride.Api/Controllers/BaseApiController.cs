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
        return StatusCode(500, new { message = "An unexpected error occurred.", traceId = HttpContext.TraceIdentifier });
    }

    protected IActionResult NotFound(string message = "Resource not found")
    {
        return NotFound(new { message, traceId = HttpContext.TraceIdentifier });
    }

    protected IActionResult BadRequest(string message)
    {
        return BadRequest(new { message, traceId = HttpContext.TraceIdentifier });
    }
} 