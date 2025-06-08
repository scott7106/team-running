# TeamStride Subdomain Architecture Implementation TODO

## Overview
Implementing three-subdomain architecture:
- `www.teamstride.net` - Marketing/signup site
- `app.teamstride.net` - Global admin functions  
- `[team].teamstride.net` - Team-specific pages with theming and widgets

## Architecture Decisions Made
- **Single Next.js app** with route groups (not separate apps)
- **Team context via middleware headers** (not JWT tokens)
- **CSS custom properties** for dynamic theming
- **Widget system** designed for extensibility
- **Single API subdomain** `api.teamstride.net` with grouped endpoints
- **Local development** using hosts file entries (www.localhost, app.localhost, [team].localhost)

---

## Planned Code Organization
```
web/src/
├── app/
│   ├── (www)/                    # Marketing site (www.teamstride.net)
│   │   ├── page.tsx             # Marketing homepage
│   │   ├── signup/              # Team creation
│   │   ├── pricing/
│   │   └── layout.tsx           # Marketing layout
│   ├── (app)/                   # Global admin (app.teamstride.net)  
│   │   ├── page.tsx             # Admin dashboard
│   │   ├── teams/               # Team management
│   │   ├── users/               # User management
│   │   └── layout.tsx           # Admin layout
│   ├── (team)/                  # Team pages ([team].teamstride.net)
│   │   ├── page.tsx             # Public team homepage
│   │   ├── join/                # Parent/guardian signup
│   │   ├── dashboard/           # Team management (authenticated)
│   │   ├── widgets/             # Widget components
│   │   └── layout.tsx           # Team layout with theming
│   ├── middleware.ts            # Subdomain routing logic
│   └── layout.tsx               # Root layout (auth providers)
```

## Planned API Structure
```
api.teamstride.net/
├── admin/
│   ├── teams/
│   ├── users/
│   └── analytics/
├── team/
│   ├── roster/
│   ├── schedules/
│   └── widgets/
└── shared/
    ├── auth/
    └── profile/
```

---

# IMPLEMENTATION STEPS

## Phase 1: Foundation & Route Structure

### Step 1: Create Route Groups Structure
**Status:** ❌ Not Started | ✅ Complete
**Prompt:** "Create the Next.js route groups structure for (www), (app), and (team) subdomains. Move the existing pages to appropriate route groups."

**Tasks:**
- [ ] Create `src/app/(www)/` directory
- [ ] Create `src/app/(app)/` directory  
- [ ] Create `src/app/(team)/` directory
- [ ] Move current `page.tsx` to `(www)/page.tsx`, do this via file move and then edit instead of regenerating file
- [ ] Move current `team/page.tsx` to `(team)/dashboard/page.tsx`, do this via file move and then edit instead of regenerating files
- [ ] Move current `src/app/admin/*` to `src/app/(app)/*`, do this via file move instead of regenerating files
- [ ] Create placeholder pages for each route group

**Success Criteria:**
- Route groups exist and pages render without errors
- URL structure remains functional during transition
- No broken imports or missing components

---

### Step 2: Implement Subdomain Middleware
**Status:** ❌ Not Started | ✅ Complete
**Prompt:** "Implement Next.js middleware to handle subdomain routing between www, app, and team contexts with team resolution from database."

**Implementation Reference:**
```typescript
// middleware.ts
export async function middleware(request: NextRequest) {
  const hostname = request.headers.get('host') || '';
  const subdomain = hostname.split('.')[0];
  
  if (subdomain !== 'www' && subdomain !== 'app') {
    // Team subdomain - resolve team
    const team = await resolveTeamBySubdomain(subdomain);
    
    if (!team) {
      // Subdomain doesn't exist - redirect to 404 or main site
      return NextResponse.redirect(new URL('https://www.teamstride.net/404'));
    }
    
    // Pass team context via headers
    const response = NextResponse.next();
    response.headers.set('x-team-id', team.id);
    response.headers.set('x-team-subdomain', subdomain);
    return response;
  }
}
```

**Tasks:**
- [ ] Create `src/middleware.ts`
- [ ] Implement subdomain detection logic
- [ ] Add team resolution from subdomain (mock for now)
- [ ] Set up headers for team context (`x-team-id`, `x-team-subdomain`)
- [ ] Handle fallbacks for non-existent teams
- [ ] Test with local hosts file entries

**Success Criteria:**
- Middleware correctly routes based on subdomain
- Team context headers are set for team subdomains
- Non-existent subdomains redirect appropriately
- Local development works with hosts file

**Context Notes:**
- Use `localhost` pattern for local development
- Mock team resolution initially, will integrate with API later
- Consider caching strategy for team lookups

---

### Step 3: Create Subdomain-Specific Layouts
**Status:** ❌ Not Started | ✅ Complete  
**Prompt:** "Create layout.tsx files for each route group (www, app, team) with appropriate styling, navigation, and theme support."

**Tasks:**
- [ ] Create `(www)/layout.tsx` - marketing site layout
- [ ] Create `(app)/layout.tsx` - admin dashboard layout
- [ ] Create `(team)/layout.tsx` - team-themed layout
- [ ] Update root `layout.tsx` to be minimal (auth providers only)
- [ ] Ensure shared components work across all layouts

**Success Criteria:**
- Each subdomain has distinct layout and styling
- Authentication providers work across all layouts
- Navigation is appropriate for each context
- No duplicate provider nesting

---

## Phase 2: Team Theming System

### Step 4: Implement Team Theme Provider
**Status:** ❌ Not Started | ✅ Complete
**Prompt:** "Create a team theming system that applies primary/secondary colors and logos dynamically across team subdomain pages."

**Tasks:**
- [ ] Create `src/contexts/ThemeContext.tsx`
- [ ] Create `src/utils/theme.ts` with theme utilities
- [ ] Implement CSS custom properties for dynamic theming
- [ ] Create theme provider for team layouts
- [ ] Add theme resolution from team context headers

**Success Criteria:**
- Team colors apply throughout team subdomain pages
- Theme switches automatically based on subdomain
- CSS custom properties update correctly
- Logo integration points are ready

**Context Notes:**
- Use CSS custom properties for runtime theme changes
- Support both light/dark variants if needed
- Consider accessibility for color contrast

---

### Step 5: Update Shared Components for Multi-Context
**Status:** ❌ Not Started | ✅ Complete
**Prompt:** "Update UserContextMenu and other shared components to work across www, app, and team contexts with appropriate styling and navigation."

**Tasks:**
- [ ] Update `UserContextMenu` to detect current context
- [ ] Create `AppHeader` component with context-aware styling
- [ ] Update navigation logic for context switching
- [ ] Ensure authentication state works across all subdomains
- [ ] Add team branding to headers where appropriate

**Success Criteria:**
- Shared components render correctly in all contexts
- Context switching works (www ↔ app ↔ team)
- Team branding appears in team context
- Navigation is appropriate for each context

---

## Phase 3: Team Widget System

### Step 6: Create Widget Infrastructure
**Status:** ❌ Not Started | ✅ Complete
**Prompt:** "Create the foundation for team widgets including base components, types, and a widget renderer that team owners can configure."

**Tasks:**
- [ ] Create `src/types/widgets.ts` with widget type definitions
- [ ] Create `src/components/widgets/` directory structure
- [ ] Create base widget component and renderer
- [ ] Implement widget configuration interface
- [ ] Create initial widget types (announcements, events, race-results, training-schedule)

**Success Criteria:**
- Widget system can render different widget types
- Widget configuration interface exists
- Foundation is extensible for new widget types
- Team owner permissions are considered in design

**Context Notes:**
- Start with 4 basic widget types: announcements, upcoming-events, race-results, training-schedule
- Design for future widget marketplace/library
- Consider drag-and-drop for widget positioning
- Only team owners can configure which widgets appear
- Team admins can edit widget content

---

### Step 7: Create Team Public Homepage
**Status:** ❌ Not Started | ✅ Complete
**Prompt:** "Create the public team homepage that displays team information, configurable widgets, and signup options for parents/guardians."

**Tasks:**
- [ ] Create `(team)/page.tsx` for public team homepage
- [ ] Implement widget display system
- [ ] Add parent/guardian signup functionality
- [ ] Apply team theming to all elements
- [ ] Add team information display (logo, colors, description)

**Success Criteria:**
- Public team page displays with team branding
- Widgets render based on team configuration
- Signup flow works for parents/guardians
- Page is mobile-responsive and accessible

---

## Phase 4: Admin & Management

### Step 8: Create Global Admin Interface
**Status:** ❌ Not Started | ✅ Complete
**Prompt:** "Create the global admin interface under app subdomain for managing teams, users, and platform-wide settings."

**Tasks:**
- [ ] Create `(app)/page.tsx` - admin dashboard
- [ ] Create team management interface
- [ ] Create user management interface  
- [ ] Implement admin-only navigation
- [ ] Add analytics/reporting placeholder

**Success Criteria:**
- Admin interface is only accessible to global admins
- Team and user management functions exist
- Navigation is admin-focused
- Proper authorization checks are in place

---

### Step 9: Team Management Dashboard
**Status:** ❌ Not Started | ✅ Complete
**Prompt:** "Create team management dashboard for team owners and admins to configure widgets, team settings, and manage their team."

**Tasks:**
- [ ] Create `(team)/dashboard/page.tsx`
- [ ] Implement widget management interface (team owners only)
- [ ] Add team settings configuration (colors, logo, etc.)
- [ ] Create team member management
- [ ] Implement role-based permissions

**Success Criteria:**
- Team owners can configure widgets
- Team settings can be updated (colors, logo, etc.)
- Team member management works
- Permissions are properly enforced

---

## Phase 5: API Integration

### Step 10: Update API Structure for Team Context
**Status:** ❌ Not Started | ✅ Complete
**Prompt:** "Update the API structure to handle team context from headers and implement proper authorization for multi-subdomain access."

**Implementation Reference:**
```typescript
// API endpoint pattern
export async function GET(request: Request) {
  const teamId = request.headers.get('x-team-id');
  const userToken = request.headers.get('authorization');
  
  // 1. Authenticate user (from JWT)
  const user = await authenticateUser(userToken);
  
  // 2. Authorize user for this team  
  const hasAccess = await checkTeamAccess(user.id, teamId);
  
  if (!hasAccess) {
    return Response.json({ error: 'Unauthorized' }, { status: 403 });
  }
  
  // 3. Proceed with team-scoped operation
  const data = await getTeamRoster(teamId);
  return Response.json(data);
}
```

**Tasks:**
- [ ] Update API routes to read team context from headers
- [ ] Implement team-scoped authorization middleware
- [ ] Create team resolution API endpoints
- [ ] Update existing team APIs for new context system
- [ ] Add error handling for team access

**Success Criteria:**
- APIs correctly read team context from middleware headers
- Authorization works for team-scoped operations
- Team resolution works from subdomain
- Error handling is appropriate for context mismatches

---

### Step 11: Implement Team Widget APIs
**Status:** ❌ Not Started | ✅ Complete
**Prompt:** "Create API endpoints for team widget management including CRUD operations and team owner permissions."

**Tasks:**
- [ ] Create widget CRUD API endpoints
- [ ] Implement team owner permission checks for widget configuration
- [ ] Implement team admin permission checks for widget content
- [ ] Add widget configuration validation
- [ ] Create widget content management APIs
- [ ] Add widget ordering/positioning APIs

**Success Criteria:**
- Widget management APIs work correctly
- Permissions properly restrict widget management (owners) vs content editing (admins)
- Widget configurations are validated
- Widget positioning can be saved/loaded

---

## Phase 6: Testing & Polish

### Step 12: Local Development Setup
**Status:** ❌ Not Started | ✅ Complete
**Prompt:** "Set up local development environment with hosts file entries and test all subdomain functionality."

**Local Development Setup:**
```
# Add to hosts file (/etc/hosts or C:\Windows\System32\drivers\etc\hosts)
127.0.0.1 www.localhost
127.0.0.1 app.localhost  
127.0.0.1 wildcats.localhost
127.0.0.1 eagles.localhost
```

**Tasks:**
- [ ] Document hosts file setup
- [ ] Test all subdomain routing
- [ ] Verify theme switching works
- [ ] Test authentication across subdomains
- [ ] Validate widget system functionality

**Success Criteria:**
- Local development works with subdomain setup
- All routing functions correctly
- Authentication state persists across subdomains
- Theming works properly for different teams

---

### Step 13: Error Handling & Edge Cases
**Status:** ❌ Not Started | ✅ Complete
**Prompt:** "Implement comprehensive error handling for subdomain routing, team resolution, and authentication edge cases."

**Tasks:**
- [ ] Handle non-existent team subdomains
- [ ] Add proper 404 pages for each context
- [ ] Implement team access denied pages
- [ ] Add loading states for team resolution
- [ ] Handle authentication edge cases across subdomains

**Success Criteria:**
- Non-existent teams redirect appropriately
- Error pages match the context they're in
- Loading states provide good UX
- Authentication edge cases are handled gracefully

---

### Step 14: Mobile Responsiveness & Accessibility
**Status:** ❌ Not Started | ✅ Complete
**Prompt:** "Ensure all subdomain contexts are mobile-responsive and meet accessibility standards."

**Tasks:**
- [ ] Test mobile layouts for all contexts
- [ ] Verify team theming works on mobile
- [ ] Test widget responsiveness
- [ ] Run accessibility audits
- [ ] Fix any responsive/accessibility issues

**Success Criteria:**
- All pages work well on mobile devices
- Team theming is mobile-friendly
- Widgets are responsive
- Accessibility standards are met
- Touch interactions work properly

---

## Phase 7: Final Integration

### Step 15: End-to-End Testing
**Status:** ❌ Not Started | ✅ Complete
**Prompt:** "Perform comprehensive end-to-end testing of the complete subdomain architecture including user flows and edge cases."

**Tasks:**
- [ ] Test complete user signup flow
- [ ] Test team creation and setup
- [ ] Test context switching between subdomains
- [ ] Test widget management workflow
- [ ] Test authentication flows across all contexts
- [ ] Performance testing with team theming

**Success Criteria:**
- All user flows work end-to-end
- Context switching is seamless
- Performance is acceptable
- No critical bugs remain
- User experience is smooth across all contexts

---

## Authentication Strategy
**Shared JWT secret** across all subdomains
- **Domain-scoped cookies** (`.teamstride.net`) for seamless transitions
- **Context switching endpoint** that redirects with proper tokens
- **Role-based redirects** after login

## Widget Permissions Strategy
- **Team Owners**: Can configure which widgets appear on team pages
- **Team Admins**: Can edit content within existing widgets
- **Future**: Widget-based permissions for granular control

## Notes for Implementation

### Prerequisites
- Understanding of Next.js App Router and middleware
- Knowledge of CSS custom properties for theming
- Familiarity with the existing codebase structure

### Testing Strategy
- Test each phase before moving to the next
- Use hosts file for local subdomain testing
- Validate responsive design at each step
- Test authentication edge cases thoroughly

### Rollback Plan
- Keep original files backed up during restructuring
- Implement feature flags for gradual rollout
- Test subdomain routing before moving pages
- Maintain API backward compatibility during transition



