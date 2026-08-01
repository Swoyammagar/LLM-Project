import type { Document } from "@/lib/types";
import { cn } from "@/lib/utils";

export function FileTypeBadge({ type }: { type: Document["type"] }) {
  const config = {
    PDF: "bg-red-50 text-red-700 dark:bg-red-900/30 dark:text-red-400",
    DOCX: "bg-blue-50 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400",
    TXT: "bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400",
  };
  return (
    <span className={cn("px-1.5 py-0.5 rounded text-[10px] font-bold tracking-wide uppercase", config[type])}>
      {type}
    </span>
  );
}
