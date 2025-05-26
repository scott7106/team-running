namespace TeamStride.Domain.Common;

public abstract class AuditedTeamEntity<TKey> : AuditedEntity<TKey>, IHasTeam where TKey : struct
{
    public Guid? TeamId { get; set; }
} 