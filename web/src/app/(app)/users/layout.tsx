import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'TeamStride - Global Administration - Users',
  description: 'Manage users across the TeamStride platform.'
};

export default function UsersLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <>{children}</>;
} 