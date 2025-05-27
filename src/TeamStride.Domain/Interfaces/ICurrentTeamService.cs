namespace TeamStride.Domain.Interfaces;

public interface ICurrentTeamService
{
    Guid TeamId { get; }
    string? GetSubdomain { get; }
    void SetTeamId(Guid teamId);
    void SetTeamSubdomain(string subdomain);
    void ClearTeam();
} 