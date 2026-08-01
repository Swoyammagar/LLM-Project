"use client";

import { Suspense, useCallback, useEffect, useRef, useState } from "react";
import { useSearchParams } from "next/navigation";
import { SquarePen, Eye, Paperclip, Send, Zap } from "lucide-react";
import { Badge } from "@/components/ui2/Badge";
import { Button } from "@/components/ui2/Button";
import { FileTypeBadge } from "@/components/ui2/FileTypeBadge";
import { ChatMessage } from "@/components/chat/ChatMessage";
import { TypingIndicator } from "@/components/chat/TypingIndicator";
import { CHAT_SESSIONS, INITIAL_MESSAGES, SUGGESTED_QUESTIONS, AI_RESPONSES } from "@/lib/mock-data";
import { cn } from "@/lib/utils";
import type { Message } from "@/lib/types";

function ChatPageInner() {
  const searchParams = useSearchParams();
  const initialSession = searchParams.get("session") ?? "1";

  const [messages, setMessages] = useState<Message[]>(INITIAL_MESSAGES);
  const [input, setInput] = useState("");
  const [isTyping, setIsTyping] = useState(false);
  const [activeSession, setActiveSession] = useState(initialSession);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => { bottomRef.current?.scrollIntoView({ behavior: "smooth" }); }, [messages, isTyping]);

  const sendMessage = useCallback((text?: string) => {
    const content = text || input.trim();
    if (!content) return;
    const now = new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
    setMessages(prev => [...prev, { id: Math.random().toString(), role: "user", content, time: now }]);
    setInput("");
    setIsTyping(true);
    setTimeout(() => {
      setMessages(prev => [...prev, {
        id: Math.random().toString(), role: "ai",
        content: AI_RESPONSES[Math.floor(Math.random() * AI_RESPONSES.length)],
        time: new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }),
        sources: ["Page 5 — Key Findings"],
      }]);
      setIsTyping(false);
    }, 1500 + Math.random() * 800);
  }, [input]);

  return (
    <div className="flex h-full overflow-hidden">
      <div className="w-64 border-r border-border flex flex-col bg-sidebar flex-shrink-0">
        <div className="p-3 border-b border-sidebar-border">
          <Button variant="primary" size="md" className="w-full" icon={<SquarePen className="w-3.5 h-3.5" />}>New Chat</Button>
        </div>
        <div className="flex-1 overflow-y-auto py-2 px-2 space-y-0.5">
          {CHAT_SESSIONS.map(s => (
            <button key={s.id} onClick={() => setActiveSession(s.id)}
              className={cn("w-full text-left px-3 py-2.5 rounded-lg transition-all", activeSession === s.id ? "bg-accent text-accent-foreground" : "hover:bg-muted")}>
              <p className="text-sm font-medium text-foreground truncate">{s.title}</p>
              <p className="text-xs text-muted-foreground truncate mt-0.5">{s.document}</p>
              <div className="flex items-center justify-between mt-1">
                <span className="text-[10px] text-muted-foreground">{s.date}</span>
                <span className="text-[10px] text-muted-foreground">{s.messages} msgs</span>
              </div>
            </button>
          ))}
        </div>
      </div>

      <div className="flex-1 flex flex-col min-w-0">
        <div className="h-14 border-b border-border flex items-center px-5 gap-3 flex-shrink-0 bg-card">
          <FileTypeBadge type="PDF" />
          <div>
            <p className="text-sm font-semibold text-foreground">Q3 Financial Report.pdf</p>
            <p className="text-xs text-muted-foreground">24 pages · 2.4 MB</p>
          </div>
          <div className="ml-auto flex items-center gap-2">
            <Badge variant="teal"><Zap className="w-3 h-3" />AI Ready</Badge>
            <Button variant="outline" size="sm" icon={<Eye className="w-3.5 h-3.5" />}>View Doc</Button>
          </div>
        </div>

        <div className="flex-1 overflow-y-auto px-6 py-5 space-y-5">
          {messages.map(msg => <ChatMessage key={msg.id} msg={msg} />)}
          {isTyping && <TypingIndicator />}
          <div ref={bottomRef} />
        </div>

        {!isTyping && (
          <div className="px-6 pb-2 flex flex-wrap gap-2">
            {SUGGESTED_QUESTIONS.map((q, i) => (
              <button key={i} onClick={() => sendMessage(q)}
                className="px-3 py-1.5 bg-muted text-muted-foreground text-xs rounded-full hover:bg-accent hover:text-accent-foreground border border-border hover:border-primary/30 transition-all">
                {q}
              </button>
            ))}
          </div>
        )}

        <div className="p-4 border-t border-border bg-card">
          <div className="flex items-end gap-3 bg-background border border-border rounded-xl p-3 focus-within:border-primary/40 focus-within:ring-2 focus-within:ring-primary/10 transition-all">
            <button className="text-muted-foreground hover:text-foreground transition-colors self-end pb-0.5">
              <Paperclip className="w-4 h-4" />
            </button>
            <textarea
              value={input}
              onChange={e => setInput(e.target.value)}
              onKeyDown={e => { if (e.key === "Enter" && !e.shiftKey) { e.preventDefault(); sendMessage(); } }}
              placeholder="Ask anything about this document..."
              rows={1}
              className="flex-1 resize-none bg-transparent text-sm text-foreground placeholder:text-muted-foreground outline-none leading-relaxed max-h-32"
            />
            <Button variant="primary" size="icon" className="flex-shrink-0" onClick={() => sendMessage()} disabled={!input.trim() || isTyping}>
              <Send className="w-4 h-4" />
            </Button>
          </div>
          <p className="text-center text-[10px] text-muted-foreground mt-2">
            DocuAI can make mistakes. Verify important information.
          </p>
        </div>
      </div>
    </div>
  );
}

export default function ChatPage() {
  return (
    <Suspense fallback={null}>
      <ChatPageInner />
    </Suspense>
  );
}
