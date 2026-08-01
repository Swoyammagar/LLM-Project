import type { ReactNode } from "react";
import { cn } from "@/lib/utils";
import { CardShell } from "@/components/ui2/CardShell";

interface StatCardProps {
  icon: ReactNode;
  label: string;
  value: string;
  sub: string;
  color: string;
}

export function StatCard({ icon, label, value, sub, color }: StatCardProps) {
  return (
    <CardShell className="p-5 hover:shadow-md transition-shadow">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm text-muted-foreground">{label}</p>
          <p className="text-2xl font-semibold text-foreground mt-1">{value}</p>
          <p className="text-xs text-muted-foreground mt-0.5">{sub}</p>
        </div>
        <div className={cn("w-10 h-10 rounded-xl flex items-center justify-center", color)}>{icon}</div>
      </div>
    </CardShell>
  );
}
