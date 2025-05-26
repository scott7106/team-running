import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Only enable static export for production builds
  ...(process.env.NODE_ENV === 'production' && {
    output: 'export',
    trailingSlash: true,
    images: {
      unoptimized: true
    }
  }),
  
  // For development, keep the default behavior
  ...(process.env.NODE_ENV === 'development' && {
    // Add any development-specific config here if needed
  })
};

export default nextConfig;
