import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

interface CardShellProps {
  children: ReactNode;
  className?: string;
  onClick?: () => void;
}

export function CardShell({ children, className, onClick }: CardShellProps) {
  return (
    <div className={cn("bg-card border border-border rounded-xl shadow-sm", className)} onClick={onClick}>
      {children}
    </div>
  );
}
