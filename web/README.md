# TeamStride Web Application

This is a [Next.js](https://nextjs.org) project built with TypeScript, TailwindCSS, and the App Router pattern.

## Project Structure

```
src/
├── app/                          # Next.js App Router pages and layouts
│   ├── admin/                    # Admin feature pages
│   │   ├── components/           # Admin-specific components
│   │   │   ├── teams/           # Team management components
│   │   │   └── users/           # User management components
│   │   ├── teams/               # Team management pages
│   │   ├── users/               # User management pages
│   │   └── page.tsx             # Admin dashboard
│   ├── login/                   # Authentication pages
│   ├── team/                    # Team member pages
│   ├── no-team-access/          # Access denied pages
│   ├── team-access-denied/      # Team-specific access denied
│   ├── layout.tsx               # Root layout with providers
│   ├── page.tsx                 # Home page
│   └── globals.css              # Global styles
├── components/                   # Shared components library
│   ├── ui/                      # Generic UI components
│   │   ├── Modal.tsx            # Base modal component
│   │   ├── FormModal.tsx        # Form-specific modal
│   │   └── ConfirmationModal.tsx # Confirmation dialog
│   ├── layouts/                 # Page layout components
│   │   └── AdminLayout.tsx      # Admin pages layout
│   ├── auth/                    # Authentication components
│   │   ├── SessionSecurityProvider.tsx
│   │   ├── IdleTimeoutProvider.tsx
│   │   ├── IdleTimeout.tsx
│   │   └── IdleTimeoutModal.tsx
│   └── shared/                  # Shared business components
│       └── UserContextMenu.tsx  # User menu component
├── types/                       # TypeScript type definitions
├── utils/                       # Utility functions and API clients
└── obj/                         # Build artifacts
```

## Naming Conventions

### Components
- **File Names**: Use PascalCase for all component files (e.g., `UserContextMenu.tsx`)
- **Function Names**: Use PascalCase for component function names (e.g., `export default function UserContextMenu()`)
- **Props Interfaces**: Use PascalCase with `Props` suffix (e.g., `interface UserContextMenuProps`)

### Directories
- **Feature Directories**: Use lowercase with hyphens (e.g., `team-access-denied/`)
- **Component Categories**: Use lowercase (e.g., `ui/`, `auth/`, `shared/`)
- **Feature-Specific Components**: Use lowercase (e.g., `teams/`, `users/`)

### Files
- **Pages**: Use `page.tsx` for Next.js app router pages
- **Layouts**: Use `layout.tsx` for Next.js layouts
- **Components**: Use PascalCase with `.tsx` extension

## Component Organization

### Shared Components (`src/components/`)
Components that are reusable across multiple features:

- **`ui/`**: Generic, unstyled or lightly styled components (modals, forms, buttons)
- **`layouts/`**: Page layout components that define page structure
- **`auth/`**: Authentication and session management components
- **`shared/`**: Business-specific components used across features

### Feature-Specific Components (`src/app/[feature]/components/`)
Components that are specific to a particular feature or route:

- **Co-location**: Components are placed near the features that use them
- **Feature Isolation**: Each feature maintains its own component directory
- **Import Paths**: Use relative imports for feature-specific components

## Import Conventions

### Absolute Imports (Preferred)
Use `@/` alias for shared components and utilities:
```typescript
import FormModal from '@/components/ui/FormModal';
import { usersApi } from '@/utils/api';
import { UserDto } from '@/types/user';
```

### Relative Imports
Use relative imports for feature-specific components:
```typescript
import CreateUserModal from '../components/users/CreateUserModal';
import EditUserModal from './EditUserModal';
```

## Development Guidelines

### Component Structure
- Keep components focused and single-responsibility
- Use TypeScript interfaces for all props
- Implement proper error handling and loading states
- Use semantic HTML elements where possible

### State Management
- Use React hooks for local state
- Implement proper cleanup in useEffect hooks
- Use context providers for shared state (auth, theme, etc.)

### Styling
- Use TailwindCSS for styling
- Follow responsive design principles
- Use semantic color classes over arbitrary values

### Performance
- Minimize client-side components ('use client')
- Prefer React Server Components where possible
- Implement proper loading and error states

## Getting Started

First, run the development server:

```bash
npm run dev
# or
yarn dev
# or
pnpm dev
# or
bun dev
```

Open [http://localhost:3000](http://localhost:3000) with your browser to see the result.

## Build Commands

```bash
# Development server
npm run dev

# Production build
npm run build

# Start production server
npm run start

# Lint code
npm run lint
```

## Architecture Decisions

### Next.js App Router
- Using the new App Router for better performance and developer experience
- Server components by default with selective client components
- File-based routing with layout support

### Component Co-location
- Feature-specific components are placed within their respective app routes
- Shared components remain in the global components directory
- This improves maintainability and reduces coupling

### TypeScript
- Strict type checking enabled
- Proper interface definitions for all props and API responses
- Type-safe API client implementations

## Learn More

To learn more about the technologies used:

- [Next.js Documentation](https://nextjs.org/docs) - Next.js features and API
- [TypeScript Handbook](https://www.typescriptlang.org/docs/) - TypeScript guide
- [TailwindCSS Documentation](https://tailwindcss.com/docs) - Utility-first CSS framework
- [React Server Components](https://react.dev/reference/rsc/server-components) - RSC guide
