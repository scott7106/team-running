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

## Validated Code Organization (After Proof of Concept)
```
web/src/
├── app/
│   ├── page.tsx                 # Single root page - conditional rendering by subdomain
│   ├── register/
│   │   └── page.tsx             # Single register page - conditional rendering  
│   ├── (www)/                   # Site/marketing context (www.teamstride.net)
│   │   ├── components/          # Site-specific components
│   │   │   ├── SiteHomePage.tsx
│   │   │   └── SiteRegisterPage.tsx
│   │   └── layout.tsx           # Marketing layout
│   ├── (app)/                   # Global admin context (app.teamstride.net)  
│   │   ├── components/          # Admin-specific components
│   │   │   ├── AdminHomePage.tsx
│   │   └── layout.tsx           # Admin layout
│   ├── (team)/                  # Team context ([team].teamstride.net)
│   │   ├── components/          # Team-specific components
│   │   │   ├── TeamHomePage.tsx
│   │   │   ├── TeamRegisterPage.tsx
│   │   │   └── TeamThemeProvider.tsx
│   │   └── layout.tsx           # Team layout with theming
│   ├── middleware.ts            # Subdomain detection + context headers
│   └── layout.tsx               # Root layout (auth providers only)
```

**Key Architecture Changes:**
- **Single page.tsx files** that conditionally render based on `x-context` header
- **Route groups for organization only** - contain components and layouts, not pages
- **Middleware sets context headers** - pages read headers and render appropriate components
- **CSS custom properties** for dynamic team theming via TeamThemeProvider
- **No parallel route conflicts** - each URL path has only one page.tsx

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

### Step 1: Migrate Existing Content from *-temp Directories  
**Status:** ✅ Complete (Validated)
**Prompt:** "Migrate content from existing *-temp directories to the new validated conditional rendering structure."

**Tasks:**
- [x] Migrate `www-temp/` content to `(www)/components/` directory
- [x] Migrate `app-temp/` content to `(app)/components/` directory  
- [x] Migrate `team-temp/` content to `(team)/components/` directory
- [x] Create wrapper components for existing pages (SiteHomePage, AdminHomePage, TeamHomePage)
- [x] Update any hardcoded routes to use conditional rendering pattern
- [x] Preserve existing functionality while adapting to new structure
- [ ] Test that all migrated content works with subdomain routing

**Success Criteria:**
- All existing functionality is preserved
- No broken pages or components
- Conditional rendering works for all migrated content
- Subdomain routing correctly serves appropriate content

**Migration Strategy:**
- Don't regenerate existing complex components - wrap them
- Preserve all existing logic and state management
- Update imports and exports to match new component structure

---

### Step 2: Implement Conditional Page Routing
**Status:** ✅ Complete (Validated)
**Prompt:** "Create single page.tsx files that conditionally render based on subdomain context headers from middleware."

**Validated Implementation:**
```typescript
// app/page.tsx - Single root page
export default async function RootPage() {
  const headersList = await headers();
  const context = headersList.get('x-context');
  
  switch(context) {
    case 'app': return <AdminHomePage />;
    case 'team': return <TeamHomePage />;
    case 'www':
    default: return <SiteHomePage />;
  }
}

// app/register/page.tsx - Single register page  
export default async function RegisterPage() {
  const headersList = await headers();
  const context = headersList.get('x-context');
  
  switch(context) {
    case 'team': return <TeamRegisterPage />;
    case 'www':
    default: return <SiteRegisterPage />;
  }
}
```

**Tasks:**
- [x] Create single `app/page.tsx` with conditional rendering
- [x] Create single `app/register/page.tsx` with conditional rendering
- [x] Create context-specific components in route group directories
- [x] Test conditional rendering works with subdomain context
- [x] Verify no "parallel pages" conflicts

**Success Criteria:**
- Single page serves different content based on subdomain
- No route conflicts or build errors
- Context switching works properly
- All subdomains serve appropriate content at same URL paths

---

### Step 3: Implement Subdomain Middleware with Context Headers
**Status:** ✅ Complete (Validated)
**Prompt:** "Implement Next.js middleware to detect subdomains and set context headers for conditional page rendering."

**Validated Implementation:**
```typescript
// middleware.ts - Context header approach (not URL rewrites)
export async function middleware(request: NextRequest) {
  const hostname = request.headers.get('host') || '';
  const subdomain = hostnameWithoutPort.split('.')[0];

  if (subdomain === 'www' || subdomain === '' || subdomain === 'localhost') {
    const response = NextResponse.next();
    response.headers.set('x-context', 'www');
    return response;
  }

  if (subdomain === 'app') {
    const response = NextResponse.next();
    response.headers.set('x-context', 'app');
    return response;
  }

  // Team subdomain
  const team = await resolveTeamBySubdomain(subdomain);
  if (team) {
    const response = NextResponse.next();
    response.headers.set('x-context', 'team');
    response.headers.set('x-team-id', team.id);
    response.headers.set('x-team-name', team.name);
    response.headers.set('x-team-primary-color', team.theme.primaryColor);
    response.headers.set('x-team-secondary-color', team.theme.secondaryColor);
    response.headers.set('x-team-logo-url', team.theme.logoUrl);
    return response;
  }
}
```

**Tasks:**
- [x] Create `src/middleware.ts` with subdomain detection
- [x] Set context headers (`x-context`, `x-team-*`) instead of URL rewrites
- [x] Add mock team resolution with theme data
- [x] Handle www, app, and team subdomain routing
- [x] Test with localhost development

**Success Criteria:**
- Middleware sets correct context headers for each subdomain
- Team theme data passed via headers
- No URL rewriting conflicts
- Local development works with subdomain patterns

---

## Phase 2: Team Theming System

### Step 4: Implement Dynamic Team Theming
**Status:** ✅ Complete (Validated)
**Prompt:** "Create a team theming system that applies primary/secondary colors and logos dynamically via CSS custom properties."

**Validated Implementation:**
```typescript
// TeamThemeProvider.tsx - CSS custom properties approach
export default async function TeamThemeProvider({ children }: TeamThemeProviderProps) {
  const headersList = await headers();
  const primaryColor = headersList.get('x-team-primary-color');
  const secondaryColor = headersList.get('x-team-secondary-color');

  const themeStyles = {
    '--team-primary': primaryColor || '#10B981',
    '--team-secondary': primaryColor ? `${primaryColor}20` : '#D1FAE5',
    '--team-primary-bg': secondaryColor || '#F0FDF4',
  } as React.CSSProperties;

  return (
    <div style={themeStyles} className="team-themed">
      {children}
    </div>
  );
}

// Usage in components
<h1 style={{ color: 'var(--team-primary)' }}>Team Name</h1>
<div style={{ backgroundColor: 'var(--team-primary-bg)' }}>Content</div>
```

**Tasks:**
- [x] Create `TeamThemeProvider.tsx` component
- [x] Implement CSS custom properties for dynamic theming  
- [x] Read theme data from middleware headers
- [x] Apply team colors to UI elements
- [x] Add team logo display capability
- [x] Test with multiple team subdomains (wildcats, eagles, lightning)

**Success Criteria:**
- Different team subdomains show different colors automatically
- CSS custom properties apply correctly at runtime
- Theme provider works with server components
- Logo display integration ready
- Multiple teams tested with distinct themes

**Validated Teams:**
- Wildcats: Brown primary (#8B5A3C), cream secondary (#F4E4C1)
- Eagles: Blue primary (#1E40AF), light blue secondary (#DBEAFE)  
- Lightning: Dark red primary (#7C2D12), yellow secondary (#FEF3C7)

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



