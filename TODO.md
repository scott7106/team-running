Add remaining unit tests for the TeamManagementService
Roles should be Owner, Admin, Coach, Parent, Athlete (Host will be a shadow role for IsGlobalAdmin users)
Implement a custom Role and RoleManager which has User, Role and Team. Remove UserTeams as it will not be needed.
Implement custom UserManager with IsInTeamAsync, IsGlobalAdminAsync and IsInRoleAsync which accounts for team correctly
Implement a way to disable Team global filter without disabling the deleted filter for global admins
Create a set of unit tests for the IHasTenant interface which tests visibility (keep this separate from specific unit tests for things like athletes, teams, etc.)
IsGlobalAdmin disables the global team filter when the user selects HOST as their selected team (this sets CurrentTeamService.TeamId to null). Otherwise the filter applies.
Refactor TeamManagementService into several partial classes, put the get functions into the main class, the crud operations into a separate file and helpers into a third file