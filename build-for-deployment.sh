#!/bin/bash

echo "Building Next.js application for static export..."
cd web
npm run build:static
if [ $? -ne 0 ]; then
    echo "Next.js build failed!"
    exit 1
fi
cd ..

echo "Building ASP.NET Core API with static files..."
dotnet build src/TeamStride.Api/TeamStride.Api.csproj --configuration Release
if [ $? -ne 0 ]; then
    echo "ASP.NET Core build failed!"
    exit 1
fi

echo "Build complete! The API now includes the web application."
echo "You can now deploy the contents of src/TeamStride.Api/bin/Release/net8.0/" 