namespace TeamStride.Domain.Interfaces;

public interface ICurrentTeamService
{
    Guid TeamId { get; }
    string? GetSubdomain { get; }
    bool IsTeamSet { get; }
    void SetTeamId(Guid teamId);
    void SetTeamSubdomain(string subdomain);
    Task<bool> SetTeamFromSubdomainAsync(string subdomain);
    bool SetTeamFromJwtClaims();
    void ClearTeam();
} 