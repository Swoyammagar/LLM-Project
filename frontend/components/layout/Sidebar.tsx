"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { toast } from "sonner";
import {
  LayoutDashboard, FileText, MessageSquare, User, Settings,
  Upload, LogOut, ChevronRight, PanelLeftClose, Sparkles,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { Avatar } from "@/components/ui2/Avatar";

const NAV_ITEMS = [
  { href: "/dashboard", label: "Dashboard", icon: LayoutDashboard },
  { href: "/documents", label: "My Documents", icon: FileText },
  { href: "/chat", label: "Chat History", icon: MessageSquare },
  { href: "/profile", label: "Profile", icon: User },
  { href: "/settings", label: "Settings", icon: Settings },
] as const;

interface SidebarProps {
  collapsed: boolean;
  setCollapsed: (value: boolean) => void;
}

export function Sidebar({ collapsed, setCollapsed }: SidebarProps) {
  const pathname = usePathname();
  const router = useRouter();

  return (
    <aside className={cn("flex flex-col h-full bg-sidebar border-r border-sidebar-border transition-all duration-200", collapsed ? "w-16" : "w-60")}>
      <div className={cn("flex items-center h-16 px-4 border-b border-sidebar-border gap-3", collapsed && "justify-center px-0")}>
        <div className="w-8 h-8 rounded-lg bg-primary flex items-center justify-center flex-shrink-0">
          <Sparkles className="w-4 h-4 text-white" />
        </div>
        {!collapsed && <span className="font-semibold text-foreground tracking-tight">DocuAI</span>}
        {!collapsed && (
          <button onClick={() => setCollapsed(true)} className="ml-auto text-muted-foreground hover:text-foreground transition-colors">
            <PanelLeftClose className="w-4 h-4" />
          </button>
        )}
      </div>

      <nav className="flex-1 py-3 px-2 space-y-0.5">
        {NAV_ITEMS.map(({ href, label, icon: Icon }) => {
          const active = pathname === href;
          return (
            <Link
              key={href}
              href={href}
              className={cn(
                "w-full flex items-center gap-3 px-2.5 h-9 rounded-lg text-sm font-medium transition-all duration-100",
                active ? "bg-accent text-accent-foreground" : "text-muted-foreground hover:text-foreground hover:bg-muted",
                collapsed && "justify-center px-0"
              )}
              title={collapsed ? label : undefined}
            >
              <Icon className={cn("w-4 h-4 flex-shrink-0", active ? "text-primary" : "")} />
              {!collapsed && label}
            </Link>
          );
        })}

        <Link
          href="/upload"
          className={cn(
            "w-full flex items-center gap-3 px-2.5 h-9 rounded-lg text-sm font-medium transition-all duration-100",
            pathname === "/upload" ? "bg-accent text-accent-foreground" : "text-muted-foreground hover:text-foreground hover:bg-muted",
            collapsed && "justify-center px-0"
          )}
          title={collapsed ? "Upload" : undefined}
        >
          <Upload className={cn("w-4 h-4 flex-shrink-0", pathname === "/upload" ? "text-primary" : "")} />
          {!collapsed && "Upload Document"}
        </Link>
      </nav>

      <div className="p-2 border-t border-sidebar-border">
        {!collapsed && (
          <div
            className="flex items-center gap-3 px-2.5 py-2 rounded-lg hover:bg-muted transition-colors cursor-pointer mb-1"
            onClick={() => router.push("/profile")}
          >
            <Avatar name="Sarah Chen" size="sm" />
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-foreground truncate">Sarah Chen</p>
              <p className="text-xs text-muted-foreground truncate">sarah@acme.com</p>
            </div>
            <ChevronRight className="w-3.5 h-3.5 text-muted-foreground" />
          </div>
        )}
        <button
          onClick={() => { toast.success("Signed out successfully"); router.push("/login"); }}
          className={cn(
            "w-full flex items-center gap-3 px-2.5 h-9 rounded-lg text-sm font-medium text-muted-foreground hover:text-destructive hover:bg-red-50 dark:hover:bg-red-900/10 transition-all duration-100",
            collapsed && "justify-center px-0"
          )}
          title={collapsed ? "Logout" : undefined}
        >
          <LogOut className="w-4 h-4 flex-shrink-0" />
          {!collapsed && "Logout"}
        </button>
        {collapsed && (
          <button
            onClick={() => setCollapsed(false)}
            className="w-full flex items-center justify-center h-9 rounded-lg text-muted-foreground hover:text-foreground hover:bg-muted transition-all mt-0.5"
          >
            <ChevronRight className="w-4 h-4" />
          </button>
        )}
      </div>
    </aside>
  );
}
