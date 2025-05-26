using System;

namespace TeamStride.Domain.Common;

public abstract class AuditedEntity<TKey> : Entity<TKey>, IAuditedEntity where TKey : struct
{
    public DateTime CreatedOn { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public Guid? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
    public Guid? DeletedBy { get; set; }
} 