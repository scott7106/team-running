# TeamStride Unified Layout Refactoring Guide

## Overview

This document outlines the refactoring approach for creating consistent layout, look, and feel across TeamStride's admin and team sites while maintaining theme flexibility for team branding.

## Current State Analysis

### Admin Section (app)
- Uses `AdminLayout` component with dark sidebar navigation
- Teams page has excellent mobile-first card layout pattern (lines 556+ in teams/page.tsx)
- Consistent header with logo, page title, and user context menu
- Professional card-based content sections with proper spacing
- Well-implemented search/filter patterns

### Team Section (team)
- Uses `TeamThemeProvider` for custom styling via CSS variables
- Inconsistent layout structure compared to admin pages
- Missing unified navigation patterns
- Different header implementations between pages

## Navigation Structure

### Admin Navigation (dark sidebar)
```
- Dashboard
- Manage Teams  
- Manage Users
```

### Team Navigation (themed sidebar)
```
- Dashboard
- Roster (implemented)
- Practices
- Races  
- Training
- Uniforms
- Events
- Fees
```

## Branding & Theming Strategy

### Admin Site Branding
- **Name**: "TeamStride" 
- **Logo**: TeamStride logo
- **Colors**: Current blue-based admin color scheme
- **Styling**: Consistent with current admin site (dark sidebar, etc.)

### Team Site Branding
- **Name**: Team Name (e.g., "Eagles Running Club")
- **Logo**: Team logo (if provided), fallback to team initial in colored circle
- **Themed Elements**: 
  - Navbar background color (using `--team-primary`)
  - Main content background color (using `--team-primary-bg`)
- **Cards**: Keep consistent with admin site styling (white background, same shadows, borders)
- **Sidebar**: Team-colored background instead of dark gray

## Theme Integration

### Limited Theme Application
```css
/* Only apply team colors to these elements */
.team-navbar { 
  background-color: var(--team-primary); 
}

.team-main-bg { 
  background-color: var(--team-primary-bg); 
}

.team-sidebar {
  background-color: var(--team-primary);
}

/* Cards remain consistent - NO theming */
.card {
  background: white;
  border: 1px solid rgb(229 231 235);
  box-shadow: 0 1px 3px 0 rgb(0 0 0 / 0.1);
  border-radius: 0.5rem;
}
```

## Component Specifications

### Base Layout Component

```typescript
interface BaseLayoutProps {
  children: ReactNode;
  pageTitle: string;
  currentSection?: string;
  variant: 'admin' | 'team';
  navigationItems: NavigationItem[];
  // Branding props
  siteName: string;
  logoUrl?: string;
  showTeamTheme?: boolean;
}

interface NavigationItem {
  key: string;
  label: string;
  href: string;
  icon: IconDefinition;
  isImplemented?: boolean; // For showing "coming soon" states
}
```

### Navigation Items Configuration

```typescript
// Admin navigation
const ADMIN_NAV_ITEMS: NavigationItem[] = [
  { key: 'dashboard', label: 'Dashboard', href: '/', icon: faHome },
  { key: 'teams', label: 'Manage Teams', href: '/teams', icon: faBuilding },
  { key: 'users', label: 'Manage Users', href: '/users', icon: faUsers }
];

// Team navigation  
const TEAM_NAV_ITEMS: NavigationItem[] = [
  { key: 'dashboard', label: 'Dashboard', href: '/', icon: faHome },
  { key: 'roster', label: 'Roster', href: '/roster', icon: faUsers, isImplemented: true },
  { key: 'practices', label: 'Practices', href: '/practices', icon: faRunning },
  { key: 'races', label: 'Races', href: '/races', icon: faTrophy },
  { key: 'training', label: 'Training', href: '/training', icon: faChartLine },
  { key: 'uniforms', label: 'Uniforms', href: '/uniforms', icon: faTshirt },
  { key: 'events', label: 'Events', href: '/events', icon: faCalendar },
  { key: 'fees', label: 'Fees', href: '/fees', icon: faDollarSign }
];
```

### Header Component

```typescript
interface UnifiedHeaderProps {
  variant: 'admin' | 'team';
  siteName: string;
  logoUrl?: string;
  onMobileMenuToggle: () => void;
  showTeamTheme?: boolean;
}
```

**Header Layout**:
- **Left**: Logo (if provided) + Site Name
- **Right**: Login button OR User Context Menu
- **Background**: White for admin, `var(--team-primary)` for team sites
- **Text Color**: Adjust based on background (white text on colored team backgrounds)

### Sidebar Component

```typescript
interface NavigationSidebarProps {
  items: NavigationItem[];
  currentSection?: string;
  variant: 'admin' | 'team';
  isOpen: boolean;
  onClose: () => void;
}
```

**Sidebar Styling**:
- **Admin**: Dark gray background (`bg-gray-900`) as current
- **Team**: Team primary color background (`var(--team-primary)`)
- **Text**: White text for both variants
- **Active item**: Slightly darker/lighter background to show selection
- **Not implemented items**: Show with disabled styling + "Coming Soon" indicator

## Layout Patterns

### Standard Card Pattern

All cards maintain admin site styling regardless of context:

```tsx
<div className="bg-white rounded-lg shadow-sm border border-gray-200">
  <div className="p-6">
    {/* Card header */}
    <div className="flex items-center justify-between mb-6">
      <div className="flex items-center">
        <FontAwesomeIcon icon={cardIcon} className="w-5 h-5 text-blue-600 mr-2" />
        <h2 className="text-xl font-semibold text-gray-900">{cardTitle}</h2>
      </div>
      {/* Optional actions */}
    </div>
    
    {/* Card content */}
    {children}
  </div>
</div>
```

### Main Content Structure

#### Authenticated Users (Private Content)
```tsx
<main className="flex-1 p-8">
  <div className="max-w-7xl mx-auto">
    {/* Page header */}
    <div className="mb-8">
      <h1 className="text-3xl font-bold text-gray-900">{pageTitle}</h1>
      <p className="text-gray-600 mt-1">{pageDescription}</p>
    </div>
    
    {/* Cards grid */}
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      {/* Card components */}
    </div>
  </div>
</main>
```

#### Unauthenticated Users (Public Content)
```tsx
<main className="flex-1 p-0">
  {/* Hero section */}
  <HeroSection />
  
  {/* Cards section */}
  <section className="py-16 px-8">
    <div className="max-w-6xl mx-auto">
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {/* Feature cards */}
      </div>
    </div>
  </section>
</main>
```

### Main Content Background

```tsx
<main className={`flex-1 ${variant === 'team' ? 'team-main-bg' : 'bg-gray-50'}`}>
  <div className="p-8">
    <div className="max-w-7xl mx-auto">
      {children}
    </div>
  </div>
</main>
```

## Responsive Design Standards

### Breakpoints
- **Mobile**: < 640px (hamburger menu, single column cards)
- **Tablet**: 640px - 1024px (2-column cards, collapsible sidebar)
- **Desktop**: 1024px+ (3+ column cards, full sidebar)

### Card Grid Pattern
```tsx
<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
```

## Design System Tokens

```css
/* Spacing */
--layout-header-height: 4rem;
--layout-sidebar-width: 16rem;
--content-padding: 2rem;
--card-padding: 1.5rem;
--card-gap: 1.5rem;

/* Borders */
--border-radius: 0.5rem;
--border-color: rgb(229 231 235);

/* Shadows */
--card-shadow: 0 1px 3px 0 rgb(0 0 0 / 0.1);
--card-shadow-hover: 0 4px 6px -1px rgb(0 0 0 / 0.1);
```

## Coming Soon Pattern

For unimplemented navigation items:

```tsx
<button 
  className="w-full flex items-center px-4 py-3 rounded-lg text-gray-300 cursor-not-allowed opacity-60"
  disabled
>
  <FontAwesomeIcon icon={item.icon} className="w-5 h-5 mr-3" />
  <span>{item.label}</span>
  <span className="ml-auto text-xs bg-gray-700 px-2 py-1 rounded">Soon</span>
</button>
```

## Implementation Roadmap

### Phase 1: Base Components ✅ COMPLETE
1. ✅ Create `BaseLayout` component
2. ✅ Create `UnifiedHeader` component  
3. ✅ Create `NavigationSidebar` component
4. ✅ Define navigation items configuration

### Phase 2: Admin Migration ✅ COMPLETE
1. ✅ Replace `AdminLayout` with `BaseLayout` (variant="admin")
2. ✅ Update admin pages to use new components
3. ✅ Test admin functionality

### Phase 3: Team Migration ✅ IN PROGRESS
1. ✅ Update team pages to use `BaseLayout` (variant="team")
2. ✅ Apply proper theming (navbar and main background only)
3. ✅ Implement navigation with "coming soon" states
4. ✅ Test theme integration

### Phase 4: Testing & Cleanup
1. Responsive testing across all breakpoints
2. Theme color testing with various team colors
3. Remove unused components and styles
4. Performance testing

## Specific Page Updates

### Team Home Page (`team-home-page.tsx`)
- Replace custom header with `UnifiedHeader`
- Implement `NavigationSidebar` with team navigation
- Use standard card grid for team member dashboard
- Apply limited theme support (navbar + main background only)

### Team Roster Page (`roster/page.tsx`)
- Apply admin teams page card layout pattern
- Use unified header and navigation
- Maintain existing functionality while standardizing UI
- Keep cards white regardless of team theme

### Admin Home Page (`admin-home-page.tsx`)
- Update to use `BaseLayout` with admin variant
- Ensure consistent card spacing matches teams page
- Minor cleanup for consistency

## Modal Consistency

Continue using the existing `Modal` component (`components/ui/modal.tsx`) with:
- Same sizing classes across all contexts
- Consistent button styling
- Mobile-optimized layouts
- Theme support for team sites (if needed)

## Success Criteria

- [ ] Consistent header across all pages
- [ ] Unified navigation pattern (admin vs team)
- [ ] Mobile-first responsive design
- [ ] Cards maintain admin styling regardless of theme
- [ ] Team colors only apply to navbar and main background
- [ ] "Coming soon" indicators for unimplemented features
- [ ] Smooth theme transitions
- [ ] No functionality regressions 