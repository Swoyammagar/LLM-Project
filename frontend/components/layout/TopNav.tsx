"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Search, Sun, Moon, Bell } from "lucide-react";
import { Input } from "@/components/ui2/Input";
import { Button } from "@/components/ui/button";
import { Avatar } from "@/components/ui2/Avatar";
import { useTheme } from "@/contexts/theme-context";

export function TopNav() {
  const { darkMode, setDarkMode } = useTheme();
  const [search, setSearch] = useState("");
  const router = useRouter();

  return (
    <header className="h-16 border-b border-border bg-card flex items-center px-6 gap-4 flex-shrink-0">
      <div className="flex-1 max-w-md">
        <Input
          placeholder="Search documents..."
          icon={<Search className="w-4 h-4" />}
          suffix={<kbd className="text-[10px] bg-muted px-1.5 py-0.5 rounded font-mono text-muted-foreground">⌘K</kbd>}
          value={search}
          onChange={e => setSearch(e.target.value)}
        />
      </div>
      <div className="ml-auto flex items-center gap-2">
        <Button variant="ghost" size="icon" onClick={() => setDarkMode(!darkMode)}>
          {darkMode ? <Sun className="w-4 h-4" /> : <Moon className="w-4 h-4" />}
        </Button>
        <Button variant="ghost" size="icon" className="relative">
          <Bell className="w-4 h-4" />
          <span className="absolute top-1.5 right-1.5 w-2 h-2 bg-primary rounded-full" />
        </Button>
        <button onClick={() => router.push("/profile")} className="pl-2">
          <Avatar name="Sarah Chen" size="sm" />
        </button>
      </div>
    </header>
  );
}
