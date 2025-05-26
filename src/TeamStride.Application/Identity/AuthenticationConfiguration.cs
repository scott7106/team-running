namespace TeamStride.Infrastructure.Identity;

public class AuthenticationConfiguration
{
    public string JwtSecret { get; set; }
    public int JwtExpirationMinutes { get; set; }
    public string JwtIssuer { get; set; }
    public string JwtAudience { get; set; }

    public OAuthProviderConfiguration Microsoft { get; set; }
    public OAuthProviderConfiguration Google { get; set; }
    public OAuthProviderConfiguration Facebook { get; set; }
    public OAuthProviderConfiguration Twitter { get; set; }
}

public class OAuthProviderConfiguration
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
} 