# UserContextMenu Component

The `UserContextMenu` component provides a universal user context menu that appears on all authenticated pages in the TeamStride application. It offers quick access to user actions and navigation options based on the user's current context and permissions.

## Features

- **Switch Teams**: Allows users to switch between different teams or global admin access (only shows if user has multiple options)
- **Dashboard**: Navigates to the appropriate dashboard based on current context (team dashboard or admin dashboard)
- **TeamStride**: Navigates to the main home page (no team context)
- **User Profile**: Takes the user to their profile page (placeholder for future implementation)
- **Logout**: Signs the user out and clears session data

## Props

```typescript
interface UserContextMenuProps {
  /** Optional custom styling class */
  className?: string;
  /** Show as button (default) or inline menu */
  variant?: 'button' | 'inline';
}
```

### Variants

- **`button`** (default): Shows as a dropdown button with user avatar, name, and context info
- **`inline`**: Shows menu items directly without dropdown (useful for sidebars)

## Usage Examples

### Basic Usage (Dropdown Button)

```tsx
import UserContextMenu from '@/components/user-context-menu';

export default function MyPage() {
  return (
    <div className="flex justify-between items-center p-4">
      <h1>My Page</h1>
      <UserContextMenu />
    </div>
  );
}
```

### Inline Usage (Sidebar)

```tsx
import UserContextMenu from '@/components/user-context-menu';

export default function Sidebar() {
  return (
    <div className="sidebar">
      {/* Other sidebar content */}
      <div className="mt-auto">
        <UserContextMenu variant="inline" />
      </div>
    </div>
  );
}
```

### With Custom Styling

```tsx
import UserContextMenu from '@/components/user-context-menu';

export default function Header() {
  return (
    <header className="bg-white shadow">
      <div className="flex justify-between items-center px-4 py-2">
        <h1>TeamStride</h1>
        <UserContextMenu className="ml-auto" />
      </div>
    </header>
  );
}
```

### Using AuthenticatedLayout

For simple authenticated pages, you can use the `AuthenticatedLayout` component which automatically includes the `UserContextMenu`:

```tsx
import AuthenticatedLayout from '@/components/authenticated-layout';

export default function MyAuthenticatedPage() {
  return (
    <AuthenticatedLayout title="My Page">
      <div className="p-6">
        <p>Your page content here...</p>
      </div>
    </AuthenticatedLayout>
  );
}
```

## Integration in Existing Layouts

### AdminLayout Integration

The `AdminLayout` component has been updated to use `UserContextMenu`:

```tsx
// In the top bar
<UserContextMenu className="lg:hidden" />

// In the desktop area
<div className="hidden lg:flex items-center space-x-4">
  <UserContextMenu />
</div>

// In the sidebar (inline variant)
<UserContextMenu variant="inline" />
```

### Team Page Integration

The team page has been updated to include the `UserContextMenu` in both the sidebar and top bar:

```tsx
// Top bar
<div className="flex items-center space-x-4">
  <UserContextMenu className="lg:hidden" />
  <div className="hidden lg:block">
    <UserContextMenu />
  </div>
</div>

// Sidebar
<UserContextMenu variant="inline" />
```

## Behavior

### Switch Teams Option

The "Switch Teams" option only appears when:
- User has global admin access AND access to 1+ teams, OR
- User has access to 2+ teams

When clicked, it navigates to the login page which handles the team selection logic.

### Dashboard Navigation

The Dashboard option navigates based on current context:
- If user has team context: navigates to `/team`
- If user is global admin: navigates to `/admin`
- Fallback: navigates to `/`

### Context Display

The component shows the user's current context:
- Team context: Shows the team subdomain/name
- Global admin: Shows "Global Admin"
- No team: Shows "No Team"

## Authentication Requirements

The component automatically:
- Checks for valid authentication tokens
- Loads user information from JWT tokens
- Determines available team access
- Hides itself if user is not authenticated

## Styling

The component uses Tailwind CSS classes and follows the existing design system:
- Blue color scheme (`bg-blue-600`, `text-blue-600`)
- Consistent spacing and typography
- Hover states and transitions
- Responsive design (hidden/shown based on screen size)

## Dependencies

- `@fortawesome/react-fontawesome` for icons
- `next/navigation` for routing
- Custom auth utilities (`@/utils/auth`)

## Future Enhancements

- User profile page implementation
- Team-specific branding/colors
- Notification badges
- Quick actions based on user role 