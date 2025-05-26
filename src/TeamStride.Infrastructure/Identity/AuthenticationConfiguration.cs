namespace TeamStride.Infrastructure.Identity;

public class AuthenticationConfiguration
{
    public required string JwtSecret { get; set; }
    public int JwtExpirationMinutes { get; set; }
    public required string JwtIssuer { get; set; }
    public required string JwtAudience { get; set; }

    public required ExternalProviderConfiguration Microsoft { get; set; }
    public required ExternalProviderConfiguration Google { get; set; }
    public required ExternalProviderConfiguration Facebook { get; set; }
    public required ExternalProviderConfiguration Twitter { get; set; }
}

public class ExternalProviderConfiguration
{
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
} 