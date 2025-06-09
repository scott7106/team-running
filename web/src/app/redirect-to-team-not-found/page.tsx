'use client'

import { useEffect } from 'react'

export default function RedirectToTeamNotFound() {
  useEffect(() => {
    // Perform client-side redirect to avoid middleware cross-subdomain issues
    const targetUrl = window.location.hostname.includes('localhost') 
      ? 'http://localhost:3000/team-not-found'
      : 'https://www.teamstride.net/team-not-found'
    
    window.location.href = targetUrl
  }, [])

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center">
      <div className="text-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
        <p className="text-gray-600">Redirecting...</p>
      </div>
    </div>
  )
} 