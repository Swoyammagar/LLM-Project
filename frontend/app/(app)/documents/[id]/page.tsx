"use client";

import { useEffect, useRef, useState } from "react";
import { useParams } from "next/navigation";
import { Download, Bot, Send } from "lucide-react";
import { Badge } from "@/components/ui2/Badge";
import { Button } from "@/components/ui2/Button";
import { ChatMessage } from "@/components/chat/ChatMessage";
import { TypingIndicator } from "@/components/chat/TypingIndicator";
import { documentService } from "@/services/documentService";
import { INITIAL_MESSAGES, AI_RESPONSES } from "@/lib/mock-data";
import type { Message } from "@/lib/types";
import type { DocumentItem } from "@/types/document";

export default function DocumentViewerPage() {
  const params = useParams<{ id: string }>();

  const [messages, setMessages] = useState<Message[]>(INITIAL_MESSAGES.slice(0, 2));
  const [input, setInput] = useState("");
  const [isTyping, setIsTyping] = useState(false);
  const [document, setDocument] = useState<DocumentItem | null>(null);
  const [fileUrl, setFileUrl] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages, isTyping]);

  useEffect(() => {
    const id = params.id;
    if (!id) return;

    let active = true;

    const loadDocument = async () => {
      setLoading(true);
      setError(null);

      try {
        const [docMeta, fileBlob] = await Promise.all([
          documentService.getById(id),
          documentService.getFileById(id),
        ]);

        if (!active) return;

        setDocument(docMeta);

        const blobUrl = URL.createObjectURL(fileBlob);
        setFileUrl((prev) => {
          if (prev) URL.revokeObjectURL(prev);
          return blobUrl;
        });
      } catch {
        if (active) setError("Unable to load this document.");
      } finally {
        if (active) setLoading(false);
      }
    };

    loadDocument();

    return () => {
      active = false;
      setFileUrl((prev) => {
        if (prev) URL.revokeObjectURL(prev);
        return null;
      });
    };
  }, [params.id]);

  const sendMessage = () => {
    if (!input.trim()) return;
    const now = new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
    setMessages((prev) => [...prev, { id: Math.random().toString(), role: "user", content: input, time: now }]);
    setInput("");
    setIsTyping(true);

    setTimeout(() => {
      setMessages((prev) => [
        ...prev,
        {
          id: Math.random().toString(),
          role: "ai",
          content: AI_RESPONSES[0],
          time: new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }),
          sources: ["Document summary"],
        },
      ]);
      setIsTyping(false);
    }, 1200);
  };

  return (
    <div className="flex h-full overflow-hidden">
      <div className="flex-1 flex flex-col border-r border-border min-w-0 bg-[#525659]">
        <div className="h-12 bg-[#3C3F41] flex items-center px-4 gap-3 shrink-0">
          <span className="text-white/70 text-xs font-medium">
            {document?.name ?? "Loading..."}
          </span>

          <a
            href={fileUrl ?? "#"}
            download={document?.name ?? "document.pdf"}
            className="ml-auto text-white/60 hover:text-white transition-colors p-1.5 rounded"
          >
            <Download className="w-3.5 h-3.5" />
          </a>
        </div>

        <div className="flex-1 overflow-y-auto py-6 px-4">
          {loading && <div className="text-white">Loading document...</div>}
          {error && <div className="text-red-300">{error}</div>}

          {!loading && !error && fileUrl && (
            <iframe
              src={fileUrl}
              className="w-full h-full min-h-[70vh] rounded-lg border-0"
              title={document?.name ?? "Document"}
            />
          )}
        </div>
      </div>

      <div className="w-96 flex flex-col shrink-0">
        <div className="h-12 border-b border-border flex items-center px-4 gap-2 bg-card shrink-0">
          <Bot className="w-4 h-4 text-teal-600" />
          <span className="text-sm font-semibold text-foreground">Ask about this document</span>
          <Badge variant="teal" className="ml-auto">Live</Badge>
        </div>

        <div className="flex-1 overflow-y-auto px-4 py-4 space-y-4">
          {messages.map((msg) => (
            <ChatMessage key={msg.id} msg={msg} />
          ))}
          {isTyping && <TypingIndicator />}
          <div ref={bottomRef} />
        </div>

        <div className="p-3 border-t border-border bg-card">
          <div className="flex gap-2">
            <input
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") sendMessage();
              }}
              placeholder="Ask about page..."
              className="flex-1 h-9 px-3 bg-background border border-border rounded-lg text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/30 transition-all"
            />
            <Button variant="primary" size="icon" onClick={sendMessage} disabled={!input.trim() || isTyping}>
              <Send className="w-4 h-4" />
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}