import type { Document } from "@/lib/types";
import { Badge } from "./Badge";
import { cn } from "@/lib/utils";

export function StatusDot({ status }: { status: Document["status"] }) {
  const config = {
    ready: { color: "bg-emerald-500", label: "Ready" },
    processing: { color: "bg-amber-500 animate-pulse", label: "Processing" },
    error: { color: "bg-red-500", label: "Error" },
  };
  const { color, label } = config[status];
  return (
    <Badge variant={status === "ready" ? "success" : status === "processing" ? "warning" : "error"}>
      <span className={cn("w-1.5 h-1.5 rounded-full", color)} />
      {label}
    </Badge>
  );
}
