using TeamStride.Domain.Common;
using TeamStride.Domain.Identity;

namespace TeamStride.Domain.Entities;

public class OwnershipTransfer : AuditedEntity<Guid>
{
    public Guid TeamId { get; set; }
    public Guid InitiatedByUserId { get; set; }
    public string NewOwnerEmail { get; set; } = string.Empty;
    public string? NewOwnerFirstName { get; set; }
    public string? NewOwnerLastName { get; set; }
    public Guid? ExistingMemberId { get; set; }
    public string? Message { get; set; }
    public DateTime ExpiresOn { get; set; }
    public OwnershipTransferStatus Status { get; set; }
    public string TransferToken { get; set; } = string.Empty;
    public DateTime? CompletedOn { get; set; }
    public Guid? CompletedByUserId { get; set; }
    
    // Navigation properties
    public virtual Team Team { get; set; } = null!;
    public virtual ApplicationUser InitiatedByUser { get; set; } = null!;
    public virtual ApplicationUser? ExistingMember { get; set; }
    public virtual ApplicationUser? CompletedByUser { get; set; }
}

public enum OwnershipTransferStatus
{
    Pending,
    Completed,
    Cancelled,
    Expired
} 