using System;
using System.ComponentModel.DataAnnotations;
using TeamStride.Domain.Identity;

namespace TeamStride.Domain.Entities;

public class UserSession
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(2000)]
    public string Fingerprint { get; set; } = string.Empty;
    
    public DateTime CreatedOn { get; set; }
    public DateTime LastActiveOn { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation property
    public ApplicationUser User { get; set; } = null!;
} 