import { cn } from "@/lib/utils";

interface ToggleProps {
  checked: boolean;
  onChange: (value: boolean) => void;
}

export function Toggle({ checked, onChange }: ToggleProps) {
  return (
    <button
      role="switch"
      aria-checked={checked}
      onClick={() => onChange(!checked)}
      className={cn(
        "relative rounded-full transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-primary/30 focus:ring-offset-2",
        checked ? "bg-primary" : "bg-border"
      )}
      style={{ width: "40px", height: "22px" }}
    >
      <span
        className={cn("absolute top-0.5 rounded-full bg-white shadow-sm transition-transform duration-200", checked ? "translate-x-[19px]" : "translate-x-0.5")}
        style={{ width: "18px", height: "18px" }}
      />
    </button>
  );
}
