"use client";

import type { ReactNode } from "react";
import { useTheme } from "@/contexts/theme-context";

export function DarkModeWrapper({ children }: { children: ReactNode }) {
  const { darkMode } = useTheme();
  return (
    <div className={darkMode ? "dark" : ""}>
      <div className="min-h-screen bg-background text-foreground antialiased">
        {children}
      </div>
    </div>
  );
}
