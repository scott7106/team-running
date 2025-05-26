# TeamStride

TeamStride is a mobile-first, multi-tenant SaaS application that empowers coaches to efficiently manage running teams. The application provides features for managing rosters, schedules, training plans, communications, gear, payments, and integrations with Garmin and MileSplit.

## Project Structure

### Backend (.NET 8)
The backend follows Clean Architecture principles with the following projects:

- **TeamStride.Domain**: Core domain entities, interfaces, and business logic
- **TeamStride.Infrastructure**: Data access, external services integration
- **TeamStride.Application**: Application services, DTOs, and business workflows
- **TeamStride.Api**: Web API endpoints and configuration

### Frontend (Next.js)
Located in the `web` directory:
- Next.js 14+
- TypeScript
- Tailwind CSS
- ESLint configuration
- Mobile-first responsive design

## Technology Stack

### Backend
- .NET 8
- SQL Server (Local Development)
- Entity Framework Core
- Microsoft Identity Framework
- OAuth2 (Microsoft, Google, Facebook, Twitter providers)

### Frontend
- Next.js
- TypeScript
- Tailwind CSS
- Mobile-first design principles

## Development Setup

### Prerequisites
- .NET 8 SDK
- SQL Server Developer Edition
- Node.js 18+ and npm
- Git

### Getting Started

1. Clone the repository
2. Set up the backend:
   ```bash
   dotnet restore
   dotnet build
   ```

3. Set up the frontend:
   ```bash
   cd web
   npm install
   npm run dev
   ```

### External Authentication Configuration

1. Configure provider credentials in `appsettings.json`:
   ```json
   {
     "Authentication": {
       "ExternalProviders": {
         "BaseUrl": "https://api.teamstride.com",
         "Microsoft": {
           "ClientId": "your_microsoft_client_id_here",
           "ClientSecret": "your_microsoft_client_secret_here"
         },
         "Google": {
           "ClientId": "your_google_client_id_here",
           "ClientSecret": "your_google_client_secret_here"
         },
         "Facebook": {
           "ClientId": "your_facebook_client_id_here",
           "ClientSecret": "your_facebook_client_secret_here"
         },
         "Twitter": {
           "ClientId": "your_twitter_client_id_here",
           "ClientSecret": "your_twitter_client_secret_here"
         }
       }
     }
   }
   ```

2. Register your application with each provider:
   - [Microsoft Azure Portal](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade)
   - [Google Cloud Console](https://console.cloud.google.com/apis/credentials)
   - [Facebook Developers](https://developers.facebook.com/apps/)
   - [Twitter Developer Portal](https://developer.twitter.com/en/portal/dashboard)

3. Configure redirect URIs for each provider:
   ```
   https://api.teamstride.com/api/authentication/external-login/microsoft/callback
   https://api.teamstride.com/api/authentication/external-login/google/callback
   https://api.teamstride.com/api/authentication/external-login/facebook/callback
   https://api.teamstride.com/api/authentication/external-login/twitter/callback
   ```

### Using External Authentication

The API provides the following endpoints for external authentication:

1. Get Login URL:
   ```http
   GET /api/authentication/external-login/{provider}?tenantId={optional_tenant_id}
   ```
   Returns the URL to redirect users for authentication with the specified provider.

2. Handle External Login:
   ```http
   POST /api/authentication/external-login
   Content-Type: application/json

   {
     "provider": "microsoft|google|facebook|twitter",
     "accessToken": "provider_access_token",
     "tenantId": "optional_tenant_id"
   }
   ```
   Returns JWT and refresh tokens for API access.

Example Flow:
1. Frontend requests login URL for chosen provider
2. User is redirected to provider's login page
3. After successful login, provider redirects back with auth code
4. Frontend exchanges auth code for access token (provider-specific)
5. Frontend sends access token to our API
6. API validates token, creates/updates user, and returns JWT

Notes:
- External users are automatically registered on first login
- Email verification is skipped for external users (provider verified)
- Default role is "Athlete" for external registrations
- Tenant ID is optional but recommended for proper team association

## Project Roadmap

1. Initial Setup ✅
   - Project structure
   - Basic configuration
   - README documentation

2. Backend Infrastructure
   - Project dependencies
   - Database context
   - Multi-tenant infrastructure
   - Cross-cutting concerns (logging, error handling)

3. Frontend Development
   - Marketing homepage
   - Authentication integration
   - Mobile-first responsive design

4. Authentication & Authorization ✅
   - Microsoft Identity setup
   - OAuth2 providers (Microsoft, Google, Facebook, Twitter)
   - Role-based access control
   - External authentication

5. Multi-tenancy Implementation
   - Tenant isolation
   - Subdomain routing
   - Tenant-specific branding

## Branching Strategy

1. Initial Development
   - Direct commits to main branch during initial setup

2. Feature Development
   - Feature branches from main
   - Pull request workflow for merging back to main

## Additional Information

For detailed information about the project requirements and specifications, refer to the Product Requirements Document (PRD) in the repository. 