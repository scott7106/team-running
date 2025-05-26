using System;

namespace TeamStride.Domain.Common;

public interface IAuditedEntity
{
    DateTime CreatedOn { get; set; }
    Guid? CreatedBy { get; set; }
    DateTime? ModifiedOn { get; set; }
    Guid? ModifiedBy { get; set; }
    bool IsDeleted { get; set; }
    DateTime? DeletedOn { get; set; }
    Guid? DeletedBy { get; set; }
} 