"use client";

import { useEffect, useRef, useState } from "react";
import { useParams } from "next/navigation";
import { Download, ChevronLeft, ChevronRight, Bot, Send } from "lucide-react";
import { Badge } from "@/components/ui2/Badge";
import { Button } from "@/components/ui2/Button";
import { ChatMessage } from "@/components/chat/ChatMessage";
import { TypingIndicator } from "@/components/chat/TypingIndicator";
import { PDFMockPage } from "@/components/viewer/PDFMockPage";
import { DOCUMENTS, INITIAL_MESSAGES, AI_RESPONSES } from "@/lib/mock-data";
import type { Message } from "@/lib/types";

export default function DocumentViewerPage() {
  const params = useParams<{ id: string }>();
  const doc = DOCUMENTS.find(d => d.id === params.id) ?? DOCUMENTS[0];

  const [messages, setMessages] = useState<Message[]>(INITIAL_MESSAGES.slice(0, 2));
  const [input, setInput] = useState("");
  const [isTyping, setIsTyping] = useState(false);
  const [pageNum, setPageNum] = useState(3);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => { bottomRef.current?.scrollIntoView({ behavior: "smooth" }); }, [messages, isTyping]);

  const sendMessage = () => {
    if (!input.trim()) return;
    const now = new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
    setMessages(prev => [...prev, { id: Math.random().toString(), role: "user", content: input, time: now }]);
    setInput("");
    setIsTyping(true);
    setTimeout(() => {
      setMessages(prev => [...prev, {
        id: Math.random().toString(), role: "ai",
        content: AI_RESPONSES[0],
        time: new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }),
        sources: ["Page 3 — Summary"],
      }]);
      setIsTyping(false);
    }, 1500);
  };

  return (
    <div className="flex h-full overflow-hidden">
      <div className="flex-1 flex flex-col border-r border-border min-w-0 bg-[#525659]">
        <div className="h-12 bg-[#3C3F41] flex items-center px-4 gap-3 flex-shrink-0">
          <div className="flex items-center gap-2">
            <button onClick={() => setPageNum(p => Math.max(1, p - 1))} className="w-7 h-7 rounded bg-white/10 flex items-center justify-center text-white hover:bg-white/20 transition-colors">
              <ChevronLeft className="w-3.5 h-3.5" />
            </button>
            <div className="flex items-center gap-1.5">
              <input type="number" value={pageNum} onChange={e => setPageNum(Math.max(1, Math.min(doc.pages, parseInt(e.target.value) || 1)))}
                className="w-10 h-7 text-center bg-white/10 text-white text-xs rounded border border-white/20 focus:outline-none" />
              <span className="text-white/60 text-xs">/ {doc.pages}</span>
            </div>
            <button onClick={() => setPageNum(p => Math.min(doc.pages, p + 1))} className="w-7 h-7 rounded bg-white/10 flex items-center justify-center text-white hover:bg-white/20 transition-colors">
              <ChevronRight className="w-3.5 h-3.5" />
            </button>
          </div>
          <div className="flex-1 flex items-center justify-center">
            <span className="text-white/70 text-xs font-medium">{doc.name}</span>
          </div>
          <button className="text-white/60 hover:text-white transition-colors p-1.5 rounded">
            <Download className="w-3.5 h-3.5" />
          </button>
        </div>
        <div className="flex-1 overflow-y-auto py-6 px-4">
          <PDFMockPage pageNum={pageNum} />
        </div>
      </div>

      <div className="w-96 flex flex-col flex-shrink-0">
        <div className="h-12 border-b border-border flex items-center px-4 gap-2 bg-card flex-shrink-0">
          <Bot className="w-4 h-4 text-teal-600" />
          <span className="text-sm font-semibold text-foreground">Ask about this document</span>
          <Badge variant="teal" className="ml-auto">Live</Badge>
        </div>
        <div className="flex-1 overflow-y-auto px-4 py-4 space-y-4">
          {messages.map(msg => <ChatMessage key={msg.id} msg={msg} />)}
          {isTyping && <TypingIndicator />}
          <div ref={bottomRef} />
        </div>
        <div className="p-3 border-t border-border bg-card">
          <div className="flex gap-2">
            <input value={input} onChange={e => setInput(e.target.value)}
              onKeyDown={e => { if (e.key === "Enter") sendMessage(); }}
              placeholder="Ask about page..."
              className="flex-1 h-9 px-3 bg-background border border-border rounded-lg text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/30 transition-all" />
            <Button variant="primary" size="icon" onClick={sendMessage} disabled={!input.trim() || isTyping}>
              <Send className="w-4 h-4" />
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
