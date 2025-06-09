# Subdomain Routing Testing Guide

## Overview
The TeamStride subdomain architecture is now implemented with middleware that routes between:
- `www` subdomain (or localhost) - Marketing site
- `app` subdomain - Global admin interface  
- `[team]` subdomains - Team-specific pages

## Current Routes Working

### 1. Marketing Site (www)
- **URL**: `http://localhost:3000`
- **Routes to**: `(www)/page.tsx` - Marketing homepage
- **Features**: Marketing layout with sign-up options

### 2. Global Admin (app)
- **URL**: `http://app.localhost:3000` (requires hosts file)
- **Root redirects to**: `/dashboard` 
- **Routes to**: `(app)/dashboard/page.tsx` - Admin dashboard
- **Features**: Admin layout with team/user management

### 3. Team Pages (any team subdomain)
- **URLs**: 
  - `http://wildcats.localhost:3000` - Public team homepage
  - `http://eagles.localhost:3000` - Public team homepage  
  - `http://lightning.localhost:3000` - Public team homepage
- **Root routes to**: `(team)/team-home/page.tsx`
- **Dashboard**: `http://wildcats.localhost:3000/dashboard` → `(team)/team-dashboard/page.tsx`
- **Features**: Team-themed layout with dynamic branding

## Setting Up Local Development

### Option 1: Use localhost:3000 directly
This works for the marketing site (www) without any setup:
```
http://localhost:3000  → Marketing site
```

### Option 2: Set up hosts file for subdomain testing
To test app and team subdomains, add these entries to your hosts file:

**Windows**: `C:\Windows\System32\drivers\etc\hosts`
**Mac/Linux**: `/etc/hosts`

```
127.0.0.1 app.localhost
127.0.0.1 wildcats.localhost
127.0.0.1 eagles.localhost
127.0.0.1 lightning.localhost
```

Then you can access:
```
http://app.localhost:3000        → Admin dashboard
http://wildcats.localhost:3000   → Wildcats team homepage
http://eagles.localhost:3000     → Eagles team homepage
http://lightning.localhost:3000  → Lightning team homepage
```

## Testing the Middleware

### 1. Test Subdomain Detection
Open browser developer tools and check the console. You should see middleware logs like:
```
[Middleware] Processing request for hostname: wildcats.localhost:3000, subdomain: wildcats, path: /
[Middleware] Team resolved: Wildcats Running Team (ID: 1)
```

### 2. Test Team Context Headers
In team pages, check that the team information is displayed correctly:
- Team name in header
- Team subdomain detection
- Team ID context

### 3. Test Route Rewrites
- `/` on team subdomain → Team homepage
- `/dashboard` on team subdomain → Team dashboard
- `/dashboard` on app subdomain → Admin dashboard

## Mock Teams Available
The middleware currently has these mock teams:
- `wildcats` - Wildcats Running Team (ID: 1)
- `eagles` - Eagles Track Club (ID: 2)  
- `lightning` - Lightning Runners (ID: 3)

## Error Handling
- Non-existent team subdomains redirect to team-not-found page
- Invalid paths show appropriate 404 pages

## Current Implementation Status
✅ **Step 1**: Route groups structure created  
✅ **Step 2**: Subdomain middleware implemented  
✅ **Step 3**: Subdomain-specific layouts created

## Next Steps
- Implement team theming system (Step 4)
- Update shared components for multi-context (Step 5)
- Create widget infrastructure (Step 6) 