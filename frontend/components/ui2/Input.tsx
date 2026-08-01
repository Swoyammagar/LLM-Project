import type { InputHTMLAttributes, ReactNode } from "react";
import { cn } from "@/lib/utils";

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  icon?: ReactNode;
  suffix?: ReactNode;
}

export function Input({ label, error, icon, suffix, className, id, ...rest }: InputProps) {
  const inputId = id || label?.toLowerCase().replace(/\s/g, "-");
  return (
    <div className="flex flex-col gap-1.5">
      {label && (
        <label htmlFor={inputId} className="text-sm font-medium text-foreground">
          {label}
        </label>
      )}
      <div className="relative flex items-center">
        {icon && (
          <span className="absolute left-3 text-muted-foreground pointer-events-none">{icon}</span>
        )}
        <input
          id={inputId}
          className={cn(
            "w-full h-9 rounded-lg border border-border bg-card text-foreground text-sm placeholder:text-muted-foreground",
            "focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary/60 transition-all duration-150",
            icon ? "pl-9" : "pl-3",
            suffix ? "pr-10" : "pr-3",
            error ? "border-destructive focus:ring-destructive/30" : "",
            className
          )}
          {...rest}
        />
        {suffix && <span className="absolute right-3 text-muted-foreground">{suffix}</span>}
      </div>
      {error && <p className="text-xs text-destructive">{error}</p>}
    </div>
  );
}
