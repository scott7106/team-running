namespace TeamStride.Domain.Identity;

public class DevelopmentTestDataConfiguration
{
    public bool SeedTestData { get; set; } = false;
    public List<TestTeam> Teams { get; set; } = new();
    public List<TestUser> Users { get; set; } = new();
}

public class TestTeam
{
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
}

public class TestUser
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsGlobalAdmin { get; set; } = false;
    public List<TestUserTeamMembership> TeamMemberships { get; set; } = new();
}

public class TestUserTeamMembership
{
    public string TeamSubdomain { get; set; } = string.Empty;
    public string Role { get; set; } = "TeamMember"; // TeamOwner, TeamAdmin, TeamMember
    public string MemberType { get; set; } = "Coach"; // Coach, Athlete, Parent
    public bool IsDefault { get; set; } = false;
} 