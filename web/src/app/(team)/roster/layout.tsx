import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'TeamStride - Team Roster',
  description: 'Manage your team athletes and roster.'
};

export default function RosterLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <>{children}</>;
} 