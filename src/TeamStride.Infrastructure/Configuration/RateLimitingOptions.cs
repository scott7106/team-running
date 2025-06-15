namespace TeamStride.Infrastructure.Configuration;

public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int MaxRequestsPerIp { get; set; } = 100;
    public int MaxRequestsPerDevice { get; set; } = 50;
    public int MaxRequestsPerEmail { get; set; } = 5;
    public int MaxRequestsPerTeam { get; set; } = 200;
    public int WindowMinutes { get; set; } = 15;
} 