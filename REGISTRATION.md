# TeamStride Registration System

## Overview
The registration system handles three types of registrations:
1. User account creation
2. Team creation with subscription plan
3. Team membership registration

## Registration Types

### 1. User Account Registration
- Available from both www and team subdomains
- Required for all other registration types
- One email per account
- No account merging
- Email verification required
- Rate limiting for registration attempts

### 2. Team Creation Registration
- Only available from www site
- Creates user account and team in one process
- User becomes team owner (TeamRole.TeamOwner)
- Plan selection required (TeamTier: Free, Standard, Premium)
- Plan features preview available
- Plan fees collection (UI only for now)
- No trial period
- Creates:
  - Team record with selected tier
  - UserTeam record with TeamRole.TeamOwner and MemberType.Coach
  - Sets ExpiresOn date for paid tiers

### 3. Team Membership Registration
- Only available during team registration window
- Requires team passcode
- Supports multiple athlete registration per guardian
- Same emergency contact for all athletes
- Code of conduct acknowledgment (if team has one)
- Waitlist support when max registrations reached
- Creates:
  - Athlete record(s) for each registered athlete
  - UserTeam record with TeamRole.TeamMember and MemberType.Athlete
  - AthleteProfile record(s) for additional athlete information

## Registration Windows

### Team Registration Window
- Multiple non-overlapping windows per year
- Configurable by team coaches/admins
- Properties:
  - Start date
  - End date
  - Maximum Athletes (registrations)
  - Registration passcode
  - Active status (read-only)

### Waitlist Management
- First-come-first-served base ordering
- Coaches can prioritize/reorder entries
- Notification system (email/SMS) when spots available
- Notifications triggered by coach/admin approval
- Waitlist entries remain until explicitly approved or removed

## Data Collection

### User Account
- Email (unique)
- Password
- First name
- Last name

### Team Creation
- Team name
- Subdomain
- Plan selection (TeamTier)
- Team colors
- Owner information (determined by user creating team)

### Team Membership
- Guardian information
  - Name
  - Email
  - Emergency contact name
  - Emergency contact phone
- Athlete information
  - Name
  - Birthdate
  - Grade Level
  - (Additional athlete info collected post-registration)
- Code of conduct acknowledgment
- Team passcode

## Security

### Rate Limiting
- IP-based rate limiting
- Device-based rate limiting
- Configurable thresholds
- Automatic blocking of suspicious activity

### Passcode Validation
- Team passcode required for registration
- Coaches can change passcode per season
- Passcode validation on registration submission

## Communication

### Email Notifications
- Registration confirmation
- Waitlist status updates
- Registration approval/rejection
- Team passcode changes sent to coaches/admins

### SMS Notifications (Optional)
- Registration confirmation
- Waitlist status updates
- Registration approval/rejection

## Database Schema

### TeamRegistrationWindow
```csharp
public class TeamRegistrationWindow
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxRegistrations { get; set; }
    public string RegistrationPasscode { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? ModifiedOn { get; set; }
    
    public virtual Team Team { get; set; }
    public virtual ICollection<TeamRegistration> Registrations { get; set; }
}
```

### TeamRegistration
```csharp
public class TeamRegistration
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public Guid? UserId { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string EmergencyContactName { get; set; }
    public string EmergencyContactPhone { get; set; }
    public bool CodeOfConductAccepted { get; set; }
    public DateTime CodeOfConductAcceptedOn { get; set; }
    public RegistrationStatus Status { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? ModifiedOn { get; set; }
    
    public virtual Team Team { get; set; }
    public virtual ApplicationUser? User { get; set; }
    public virtual ICollection<AthleteRegistration> Athletes { get; set; }
}
```

### AthleteRegistration
```csharp
public class AthleteRegistration
{
    public Guid Id { get; set; }
    public Guid RegistrationId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime Birthdate {get; set;}
    public string GradeLevel {get; set;}
    public DateTime CreatedOn { get; set; }
    
    public virtual TeamRegistration Registration { get; set; }
}
```

## API Endpoints

### Team Registration Window Management
```csharp
[HttpPost("api/teams/{teamId}/registration-windows")]
Task<TeamRegistrationWindowDto> CreateRegistrationWindow(CreateRegistrationWindowDto dto);

[HttpPut("api/teams/{teamId}/registration-windows/{windowId}")]
Task<TeamRegistrationWindowDto> UpdateRegistrationWindow(Guid windowId, UpdateRegistrationWindowDto dto);

[HttpGet("api/teams/{teamId}/registration-windows")]
Task<List<TeamRegistrationWindowDto>> GetRegistrationWindows(Guid teamId);
```

### Team Registration
```csharp
[HttpPost("api/teams/{teamId}/registrations")]
Task<TeamRegistrationDto> SubmitRegistration(SubmitRegistrationDto dto);

[HttpPut("api/teams/{teamId}/registrations/{registrationId}/status")]
Task<TeamRegistrationDto> UpdateRegistrationStatus(Guid registrationId, UpdateRegistrationStatusDto dto);

[HttpGet("api/teams/{teamId}/registrations")]
Task<List<TeamRegistrationDto>> GetRegistrations(Guid teamId);

[HttpGet("api/teams/{teamId}/registrations/waitlist")]
Task<List<TeamRegistrationDto>> GetWaitlist(Guid teamId);
```

## UI Components

### Registration Forms
1. User Account Registration
   - Email
   - Password
   - First name
   - Last name

2. Team Creation
   - Team name
   - Subdomain
   - Plan selection
   - Team colors
   - Owner information

3. Team Membership
   - Guardian information
   - Athlete information (multiple)
   - Emergency contact
   - Code of conduct
   - Team passcode

### Admin Interfaces
1. Registration Window Management
   - Create/edit windows
   - Set passcodes
   - Configure limits

2. Registration Management
   - View registrations
   - Approve/reject
   - Manage waitlist
   - Send notifications

## Implementation Phases

### Phase 1: Core Registration
- User account creation
- Team creation with tier selection
- Basic team membership registration
- Registration window management

### Phase 2: Waitlist & Notifications
- Waitlist functionality
- Email notifications
- SMS notifications
- Registration status management

### Phase 3: Security & Validation
- Rate limiting
- Passcode validation
- Additional security measures

### Phase 4: Admin Tools
- Registration management interface
- Waitlist management
- Notification controls
- Reporting

## Integration with Existing Models

### Team Creation Process
1. Create ApplicationUser for owner
2. Create Team record with:
   - Selected TeamTier
   - ExpiresOn date (null for Free, 1 year for paid tiers)
   - OwnerId set to new user
3. Create UserTeam record:
   - TeamRole.TeamOwner
   - MemberType.Coach
   - IsActive = true
   - JoinedOn = current date

### Athlete Registration Process
1. Create/Find ApplicationUser for guardian
2. Create Athlete record(s) for each athlete
3. Create UserTeam record:
   - TeamRole.TeamMember
   - MemberType.Athlete
   - IsActive = true
   - JoinedOn = current date
4. Create AthleteProfile record(s) for additional info
5. Update team athlete count and check against tier limits

### Tier Limits
- Free: 7 athletes, 2 admins, 2 coaches
- Standard: 30 athletes, 5 admins, 5 coaches
- Premium: Unlimited athletes, admins, and coaches 