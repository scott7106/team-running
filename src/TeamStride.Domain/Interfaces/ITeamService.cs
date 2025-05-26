namespace TeamStride.Domain.Interfaces;

public interface ITeamService
{
    Guid CurrentTeamId { get; }
    string? CurrentTeamSubdomain { get; }
    void SetCurrentTeam(Guid teamId);
    void SetCurrentTeam(string subdomain);
    void ClearCurrentTeam();
} 