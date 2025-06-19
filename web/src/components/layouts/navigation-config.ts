import { IconDefinition } from '@fortawesome/fontawesome-svg-core';
import { 
  faHome,
  faBuilding,
  faUsers,
  faRunning,
  faTrophy,
  faChartLine,
  faTshirt,
  faCalendar,
  faDollarSign
} from '@fortawesome/free-solid-svg-icons';

export interface NavigationItem {
  key: string;
  label: string;
  href: string;
  icon: IconDefinition;
  isImplemented?: boolean; // For showing "coming soon" states
}

// Admin navigation
export const ADMIN_NAV_ITEMS: NavigationItem[] = [
  { key: 'dashboard', label: 'Dashboard', href: '/', icon: faHome },
  { key: 'teams', label: 'Manage Teams', href: '/teams', icon: faBuilding },
  { key: 'users', label: 'Manage Users', href: '/users', icon: faUsers }
];

// Team navigation  
export const TEAM_NAV_ITEMS: NavigationItem[] = [
  { key: 'dashboard', label: 'Dashboard', href: '/', icon: faHome },
  { key: 'roster', label: 'Roster', href: '/roster', icon: faUsers, isImplemented: true },
  { key: 'practices', label: 'Practices', href: '/practices', icon: faRunning },
  { key: 'races', label: 'Races', href: '/races', icon: faTrophy },
  { key: 'training', label: 'Training', href: '/training', icon: faChartLine },
  { key: 'uniforms', label: 'Uniforms', href: '/uniforms', icon: faTshirt },
  { key: 'events', label: 'Events', href: '/events', icon: faCalendar },
  { key: 'fees', label: 'Fees', href: '/fees', icon: faDollarSign }
]; 