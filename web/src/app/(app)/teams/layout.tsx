import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'TeamStride - Global Administration - Teams',
  description: 'Manage teams across the TeamStride platform.'
};

export default function TeamsLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <>{children}</>;
} 