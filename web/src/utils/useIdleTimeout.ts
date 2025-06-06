import { useState, useEffect, useRef, useCallback } from 'react';

interface UseIdleTimeoutOptions {
  timeout: number; // in milliseconds
  warningTime: number; // in milliseconds before timeout to show warning
  onIdle?: () => void;
  onActive?: () => void;
  onWarning?: () => void;
}

interface UseIdleTimeoutReturn {
  isIdle: boolean;
  showWarning: boolean;
  remainingTime: number;
  reset: () => void;
  extend: () => void;
}

const ACTIVITY_EVENTS = [
  'mousedown',
  'mousemove', 
  'keypress',
  'scroll',
  'touchstart',
  'click'
] as const;

export function useIdleTimeout({
  timeout,
  warningTime,
  onIdle,
  onActive,
  onWarning
}: UseIdleTimeoutOptions): UseIdleTimeoutReturn {
  // Use refs for timer IDs to prevent re-render issues
  const timeoutRef = useRef<NodeJS.Timeout | null>(null);
  const warningTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const countdownIntervalRef = useRef<NodeJS.Timeout | null>(null);
  const lastActiveRef = useRef<number>(Date.now());
  
  // Use refs for state that shouldn't cause re-renders of the timer logic
  const isIdleRef = useRef(false);
  const showWarningRef = useRef(false);
  
  // Only use state for UI updates
  const [uiState, setUiState] = useState({
    isIdle: false,
    showWarning: false,
    remainingTime: 0
  });

  const clearAllTimers = useCallback(() => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
      timeoutRef.current = null;
    }
    if (warningTimeoutRef.current) {
      clearTimeout(warningTimeoutRef.current);
      warningTimeoutRef.current = null;
    }
    if (countdownIntervalRef.current) {
      clearInterval(countdownIntervalRef.current);
      countdownIntervalRef.current = null;
    }
  }, []);

  const startCountdown = useCallback(() => {
    const startTime = Date.now();
    const endTime = startTime + warningTime;
    
    const updateCountdown = () => {
      const now = Date.now();
      const remaining = Math.max(0, Math.ceil((endTime - now) / 1000));
      
      setUiState(prev => ({ ...prev, remainingTime: remaining }));
      
      if (remaining <= 0) {
        clearInterval(countdownIntervalRef.current!);
        countdownIntervalRef.current = null;
      }
    };
    
    updateCountdown(); // Initial update
    countdownIntervalRef.current = setInterval(updateCountdown, 1000);
  }, [warningTime]);

  const handleIdle = useCallback(() => {
    isIdleRef.current = true;
    showWarningRef.current = false;
    
    setUiState({
      isIdle: true,
      showWarning: false,
      remainingTime: 0
    });
    
    clearAllTimers();
    
    // Let the component handle the logout decision
    onIdle?.();
  }, [onIdle, clearAllTimers]);

  const handleWarning = useCallback(() => {
    showWarningRef.current = true;
    
    setUiState(prev => ({
      ...prev,
      showWarning: true,
      remainingTime: Math.ceil(warningTime / 1000)
    }));
    
    startCountdown();
    onWarning?.();
    
    // Set timeout for actual logout
    timeoutRef.current = setTimeout(handleIdle, warningTime);
  }, [warningTime, onWarning, handleIdle, startCountdown]);

  const reset = useCallback(() => {
    lastActiveRef.current = Date.now();
    isIdleRef.current = false;
    showWarningRef.current = false;
    
    setUiState({
      isIdle: false,
      showWarning: false,
      remainingTime: 0
    });
    
    clearAllTimers();
    onActive?.();
    
    // Only set timeout if not disabled
    if (timeout > 0 && warningTime > 0) {
      // Set warning timeout (timeout - warningTime)
      const warningDelay = timeout - warningTime;
      warningTimeoutRef.current = setTimeout(handleWarning, warningDelay);
    }
  }, [timeout, warningTime, onActive, handleWarning, clearAllTimers]);

  const extend = useCallback(() => {
    reset();
  }, [reset]);

  const handleActivity = useCallback(() => {
    // Don't reset if warning is showing - only explicit actions should close it
    if (showWarningRef.current) {
      return;
    }
    
    const now = Date.now();
    // Throttle activity detection to avoid excessive resets
    if (now - lastActiveRef.current > 1000) { // 1 second throttle
      reset();
    }
  }, [reset]);

  useEffect(() => {
    // Don't start timers if timeout is 0 (disabled)
    if (timeout === 0) {
      return;
    }

    // Add event listeners for user activity
    ACTIVITY_EVENTS.forEach(event => {
      document.addEventListener(event, handleActivity, true);
    });

    // Start the initial timer
    reset();

    return () => {
      // Cleanup
      ACTIVITY_EVENTS.forEach(event => {
        document.removeEventListener(event, handleActivity, true);
      });
      clearAllTimers();
    };
  }, [timeout, warningTime, handleActivity, reset, clearAllTimers]);

  return {
    isIdle: uiState.isIdle,
    showWarning: uiState.showWarning,
    remainingTime: uiState.remainingTime,
    reset,
    extend
  };
} 