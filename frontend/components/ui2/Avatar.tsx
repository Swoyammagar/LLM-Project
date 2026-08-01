import { cn } from "@/lib/utils";

interface AvatarProps {
  name: string;
  size?: "sm" | "md" | "lg";
  className?: string;
}

export function Avatar({ name, size = "md", className }: AvatarProps) {
  const initials = name.split(" ").map(n => n[0]).join("").slice(0, 2).toUpperCase();
  const sizes = { sm: "w-7 h-7 text-xs", md: "w-9 h-9 text-sm", lg: "w-16 h-16 text-xl" };
  return (
    <div className={cn("rounded-full bg-primary flex items-center justify-center text-primary-foreground font-semibold flex-shrink-0", sizes[size], className)}>
      {initials}
    </div>
  );
}
