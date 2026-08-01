import { Loader2 } from "lucide-react";
import type { ButtonHTMLAttributes, ReactNode } from "react";
import { cn } from "@/lib/utils";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "ghost" | "danger" | "outline";
  size?: "sm" | "md" | "lg" | "icon";
  loading?: boolean;
  icon?: ReactNode;
  iconRight?: boolean;
}

export function Button({
  variant = "primary", size = "md", loading, icon, iconRight,
  className, children, disabled, ...rest
}: ButtonProps) {
  const base = "inline-flex items-center justify-center gap-2 font-medium rounded-lg transition-all duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed select-none";
  const variants = {
    primary: "bg-primary text-primary-foreground hover:bg-primary/90 focus-visible:ring-primary/40 shadow-sm",
    secondary: "bg-secondary text-secondary-foreground hover:bg-secondary/80 focus-visible:ring-border",
    ghost: "bg-transparent text-foreground hover:bg-muted focus-visible:ring-border",
    danger: "bg-destructive text-destructive-foreground hover:bg-destructive/90 focus-visible:ring-destructive/40",
    outline: "bg-transparent border border-border text-foreground hover:bg-muted focus-visible:ring-border",
  };
  const sizes = {
    sm: "h-8 px-3 text-xs",
    md: "h-9 px-4 text-sm",
    lg: "h-11 px-5 text-sm",
    icon: "h-9 w-9 text-sm",
  };
  return (
    <button
      className={cn(base, variants[variant], sizes[size], className)}
      disabled={disabled || loading}
      {...rest}
    >
      {loading ? (
        <Loader2 className="w-3.5 h-3.5 animate-spin" />
      ) : icon && !iconRight ? icon : null}
      {children}
      {!loading && icon && iconRight ? icon : null}
    </button>
  );
}
