import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Subdomain architecture requires runtime server - static export disabled
  // ...(process.env.NODE_ENV === 'production' && {
  //   output: 'export',
  //   trailingSlash: true,
  //   images: {
  //     unoptimized: true
  //   }
  // }),
  
  // For development, proxy API requests to the backend
  ...(process.env.NODE_ENV === 'development' && {
    async rewrites() {
      return [
        {
          source: '/api/:path*',
          destination: 'http://localhost:5295/api/:path*'
        }
      ];
    }
  })
};

export default nextConfig;
