namespace TeamStride.Domain.Common;

public abstract class TeamEntity<TKey> : Entity<TKey>, IHasTeam where TKey : struct
{
    public Guid? TeamId { get; set; }
} 