using TeamStride.Domain.Entities;

namespace TeamStride.Application.Teams.Dtos;

public class TeamTierLimitsDto
{
    public TeamTier Tier { get; set; }
    public int MaxAthletes { get; set; }
    public int MaxAdmins { get; set; }
    public int MaxCoaches { get; set; }
    public bool AllowCustomBranding { get; set; }
    public bool AllowAdvancedReporting { get; set; }
} 