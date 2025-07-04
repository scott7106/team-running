import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import IdleTimeoutProvider from "@/components/auth/idle-timeout-provider";
import { AuthProvider } from '@/contexts/auth-context';

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "TeamStride - Running Team Management Made Simple",
  description: "Empower coaches to efficiently manage running teams with rosters, schedules, training plans, communications, and more. Mobile-first team management platform.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body
        className={`${geistSans.variable} ${geistMono.variable} antialiased`}
      >
        <AuthProvider>
          <IdleTimeoutProvider>
            {children}
          </IdleTimeoutProvider>
        </AuthProvider>
      </body>
    </html>
  );
}
