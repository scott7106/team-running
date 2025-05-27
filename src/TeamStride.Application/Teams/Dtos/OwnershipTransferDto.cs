using TeamStride.Domain.Entities;

namespace TeamStride.Application.Teams.Dtos;

public class OwnershipTransferDto
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public Guid InitiatedByUserId { get; set; }
    public string InitiatedByUserName { get; set; } = string.Empty;
    public string NewOwnerEmail { get; set; } = string.Empty;
    public string? NewOwnerFirstName { get; set; }
    public string? NewOwnerLastName { get; set; }
    public Guid? ExistingMemberId { get; set; }
    public string? Message { get; set; }
    public DateTime InitiatedOn { get; set; }
    public DateTime ExpiresOn { get; set; }
    public OwnershipTransferStatus Status { get; set; }
    public string TransferToken { get; set; } = string.Empty;
} 