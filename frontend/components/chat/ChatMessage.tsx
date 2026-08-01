import { Bot } from "lucide-react";
import { Bookmark } from "lucide-react";
import { cn } from "@/lib/utils";
import { Avatar } from "@/components/ui2/Avatar";
import type { Message } from "@/lib/types";

export function ChatMessage({ msg }: { msg: Message }) {
  const isUser = msg.role === "user";
  const lines = msg.content.split("\n");
  return (
    <div className={cn("flex items-start gap-3", isUser && "flex-row-reverse")}>
      {isUser ? (
        <Avatar name="Sarah Chen" size="sm" className="flex-shrink-0" />
      ) : (
        <div className="w-7 h-7 rounded-full bg-teal-100 dark:bg-teal-900/30 flex items-center justify-center flex-shrink-0">
          <Bot className="w-3.5 h-3.5 text-teal-600" />
        </div>
      )}
      <div className={cn("max-w-[70%] space-y-2", isUser && "items-end flex flex-col")}>
        <div className={cn("px-4 py-3 rounded-2xl text-sm leading-relaxed", isUser ? "bg-primary text-white rounded-br-sm" : "bg-card border border-border text-foreground rounded-bl-sm")}>
          {lines.map((line, i) => {
            const bold = line.replace(/\*\*(.*?)\*\*/g, (_, m) => `<strong>${m}</strong>`);
            return (
              <span key={i}>
                <span dangerouslySetInnerHTML={{ __html: bold }} />
                {i < lines.length - 1 && <br />}
              </span>
            );
          })}
        </div>
        {!isUser && msg.sources && msg.sources.length > 0 && (
          <div className="flex flex-wrap gap-1.5">
            {msg.sources.map((src, i) => (
              <button key={i} className="flex items-center gap-1 px-2 py-1 bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-300 rounded-lg text-xs hover:bg-blue-100 dark:hover:bg-blue-900/40 transition-colors">
                <Bookmark className="w-3 h-3" />
                {src}
              </button>
            ))}
          </div>
        )}
        <span className="text-[10px] text-muted-foreground px-1">{msg.time}</span>
      </div>
    </div>
  );
}
