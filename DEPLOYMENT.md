# TeamStride Deployment Guide

This project supports both separate development environments and combined deployment for test/production environments.

## Development Environment

In development, run the applications separately to maintain full development experience:

### API (ASP.NET Core)
```bash
cd src/TeamStride.Api
dotnet run
```
- Runs on `https://localhost:7000` and `http://localhost:5000`
- Swagger UI available at `/swagger`
- Full hot reload and debugging capabilities

### Web Application (Next.js)
```bash
cd web
npm run dev
```
- Runs on `http://localhost:3000`
- Full Next.js development features (hot reload, fast refresh, etc.)

## Production/Test Deployment

For production and test environments, the applications are combined into a single deployment unit.

### Build Process

#### Windows (PowerShell)
```powershell
.\build-for-deployment.ps1
```

#### Linux/macOS (Bash)
```bash
chmod +x build-for-deployment.sh
./build-for-deployment.sh
```

#### Manual Build Steps
1. Build Next.js for static export:
   ```bash
   cd web
   npm run build:static
   cd ..
   ```

2. Build ASP.NET Core API with static files:
   ```bash
   dotnet build src/TeamStride.Api/TeamStride.Api.csproj --configuration Release
   ```

### How It Works

1. **Next.js Configuration**: The `next.config.ts` conditionally enables static export only for production builds
2. **ASP.NET Core Integration**: The `Program.cs` serves static files and provides SPA fallback routing in non-development environments
3. **Build Integration**: The `.csproj` file automatically includes Next.js build output in Release builds

### Deployment Output

After building, deploy the contents of:
```
src/TeamStride.Api/bin/Release/net8.0/
```

This directory contains:
- The ASP.NET Core API executable
- All dependencies
- The Next.js static files in `wwwroot/`

### Routing in Production

- `/api/*` - Routes to ASP.NET Core API controllers
- `/swagger` - API documentation (if enabled for the environment)
- `/*` - All other routes serve the Next.js application
- Fallback to `index.html` for client-side routing

### Environment Configuration

The application automatically detects the environment:
- **Development**: Separate servers, Swagger enabled, no static file serving
- **Production/Test**: Combined application, static file serving, SPA fallback routing

### Benefits

✅ **Development**: Full development experience with hot reload  
✅ **Deployment**: Single artifact, simplified hosting  
✅ **Performance**: No Node.js required in production  
✅ **CORS**: No cross-origin issues in production  
✅ **Maintenance**: Single application to deploy and monitor 