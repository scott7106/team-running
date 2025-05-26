#!/usr/bin/env pwsh

Write-Host "Building Next.js application for static export..." -ForegroundColor Green
Set-Location web
npm run build:static
if ($LASTEXITCODE -ne 0) {
    Write-Host "Next.js build failed!" -ForegroundColor Red
    exit 1
}
Set-Location ..

Write-Host "Building ASP.NET Core API with static files..." -ForegroundColor Green
dotnet build src/TeamStride.Api/TeamStride.Api.csproj --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "ASP.NET Core build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build complete! The API now includes the web application." -ForegroundColor Green
Write-Host "You can now deploy the contents of src/TeamStride.Api/bin/Release/net8.0/" -ForegroundColor Yellow 