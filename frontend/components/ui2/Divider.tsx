export function Divider({ label }: { label?: string }) {
  return (
    <div className="flex items-center gap-3 my-2">
      <div className="flex-1 h-px bg-border" />
      {label && <span className="text-xs text-muted-foreground">{label}</span>}
      <div className="flex-1 h-px bg-border" />
    </div>
  );
}
