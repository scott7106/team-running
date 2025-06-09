'use client';

import { useEffect, useState } from 'react';
import { generateFingerprint, validateSessionFingerprint, logoutForSecurityViolation } from '@/utils/auth';

export default function SessionSecurityProvider({ children }: { children: React.ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isInitialized, setIsInitialized] = useState(false);

  // Check authentication status
  useEffect(() => {
    // Ensure we're running on the client side
    if (typeof window === 'undefined') {
      setIsInitialized(true);
      return;
    }

    try {
      const token = localStorage.getItem('token');
      const isAuth = !!token;
      setIsAuthenticated(isAuth);

      if (isAuth) {
        // Initialize session fingerprint
        const storedFingerprint = localStorage.getItem('sessionFingerprint');
        if (!storedFingerprint) {
          try {
            const fingerprint = generateFingerprint();
            localStorage.setItem('sessionFingerprint', fingerprint);
          } catch (error) {
            console.error('Failed to generate session fingerprint:', error);
          }
        }
      }
    } catch (error) {
      console.error('Error during authentication check:', error);
    }

    setIsInitialized(true);
  }, []);

  // Heartbeat mechanism
  useEffect(() => {
    if (!isAuthenticated || typeof window === 'undefined') return;

    let heartbeatInterval: NodeJS.Timeout | null = null;
    let missedHeartbeatCount = 0;
    const HEARTBEAT_INTERVAL = 90 * 1000; // 90 seconds
    const MAX_MISSED_HEARTBEATS = 5;

    const performHeartbeat = async () => {
      try {
        const token = localStorage.getItem('token');
        if (!token) {
          return;
        }

        const response = await fetch('/api/authentication/heartbeat', {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            fingerprint: generateFingerprint()
          })
        });

        if (response.ok) {
          missedHeartbeatCount = 0;
        } else {
          missedHeartbeatCount++;
          
          if (missedHeartbeatCount >= MAX_MISSED_HEARTBEATS) {
            logoutForSecurityViolation('Heartbeat validation failed');
          }
        }
      } catch {
        missedHeartbeatCount++;
        
        if (missedHeartbeatCount >= MAX_MISSED_HEARTBEATS) {
          logoutForSecurityViolation('Heartbeat validation failed');
        }
      }
    };

    // Start heartbeat
    performHeartbeat(); // Initial heartbeat
    heartbeatInterval = setInterval(performHeartbeat, HEARTBEAT_INTERVAL);

    return () => {
      if (heartbeatInterval) {
        clearInterval(heartbeatInterval);
      }
    };
  }, [isAuthenticated]);

  // Focus validation
  useEffect(() => {
    if (!isAuthenticated || typeof window === 'undefined') return;

    const handleVisibilityChange = async () => {
      if (document.hidden) {
        return;
      }

      try {
        const isValid = validateSessionFingerprint();
        if (!isValid) {
          logoutForSecurityViolation('Session fingerprint validation failed');
        }
      } catch (error) {
        console.error('Session validation error:', error);
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
    };
  }, [isAuthenticated]);

  if (!isInitialized) {
    return null;
  }

  return <>{children}</>;
} 