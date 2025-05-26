using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace TeamStride.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "v1")]
public class HealthController : BaseApiController
{
    public HealthController(ILogger<HealthController> logger) : base(logger)
    {
    }

    /// <summary>
    /// Gets the health status of the API
    /// </summary>
    /// <returns>Health status and timestamp</returns>
    /// <response code="200">API is healthy</response>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        Logger.LogInformation("Health check endpoint called");
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
} 