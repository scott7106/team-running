namespace TeamStride.Infrastructure.Configuration;

public class AppConfiguration
{
    public const string SectionName = "App";

    public string TeamUrl { get; set; } = "https://{team}.teamstride.net/team";
} 