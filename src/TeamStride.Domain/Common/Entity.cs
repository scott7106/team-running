namespace TeamStride.Domain.Common;

public abstract class Entity<TKey> where TKey : struct
{
    public TKey Id { get; set; }
} 