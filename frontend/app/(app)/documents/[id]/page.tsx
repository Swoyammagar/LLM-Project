"use client";

import { useEffect, useRef, useState } from "react";
import { useParams } from "next/navigation";
import { Download, Bot, Send } from "lucide-react";

import { Badge } from "@/components/ui2/Badge";
import { Button } from "@/components/ui2/Button";
import { ChatMessage } from "@/components/chat/ChatMessage";
import { TypingIndicator } from "@/components/chat/TypingIndicator";

import { documentService } from "@/services/documentService";
import { chatService } from "@/services/chatService";

import type { Message } from "@/lib/types";
import type { DocumentItem } from "@/types/document";

export default function DocumentViewerPage() {
  const params = useParams<{ id: string }>();

  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState("");
  const [isSending, setIsSending] = useState(false);

  const [document, setDocument] = useState<DocumentItem | null>(null);
  const [fileUrl, setFileUrl] = useState<string | null>(null);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Keeps this chat threaded as one conversation.
  const [conversationId, setConversationId] = useState<string | null>(null);

  const bottomRef = useRef<HTMLDivElement>(null);

  /*
   * Scroll to the latest message whenever:
   * - a new message is added
   * - the AI starts/stops responding
   */
  useEffect(() => {
    bottomRef.current?.scrollIntoView({
      behavior: "smooth",
    });
  }, [messages, isSending]);

  /*
   * Load document metadata and the actual file.
   */
  useEffect(() => {
    const id = params.id;

    if (!id) {
      return;
    }

    let active = true;

    const loadDocument = async () => {
      setLoading(true);
      setError(null);

      try {
        const [docMeta, fileBlob] = await Promise.all([
          documentService.getById(id),
          documentService.getFileById(id),
        ]);

        if (!active) {
          return;
        }

        setDocument(docMeta);

        const blobUrl = URL.createObjectURL(fileBlob);

        setFileUrl((previousUrl) => {
          if (previousUrl) {
            URL.revokeObjectURL(previousUrl);
          }

          return blobUrl;
        });
      } catch (err) {
        console.error("Failed to load document:", err);

        if (active) {
          setError("Unable to load this document.");
        }
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    };

    loadDocument();

    return () => {
      active = false;

      setFileUrl((previousUrl) => {
        if (previousUrl) {
          URL.revokeObjectURL(previousUrl);
        }

        return null;
      });
    };
  }, [params.id]);

  /*
   * Send a message to the real backend.
   */
  const sendMessage = async () => {
    const question = input.trim();

    if (!question || isSending) {
      return;
    }

    const now = new Date().toLocaleTimeString([], {
      hour: "2-digit",
      minute: "2-digit",
    });

    // Optimistically add the user's message immediately.
    const userMessage: Message = {
      id: `temp-user-${crypto.randomUUID()}`,
      role: "user",
      content: question,
      time: now,
    };

    setMessages((previousMessages) => [
      ...previousMessages,
      userMessage,
    ]);

    setInput("");
    setIsSending(true);

    try {
      const result = await chatService.sendMessage({
        question,
        conversationId: conversationId ?? undefined,
        documentId: params.id,
      });

      // The backend creates a conversation when conversationId is not supplied.
      // Save that ID so the next question continues the same conversation.
      setConversationId(result.conversationId);

      // Add the AI response.
      setMessages((previousMessages) => [
        ...previousMessages,
        result.aiMessage,
      ]);
    } catch (err) {
      console.error("Failed to send chat message:", err);

      // Show a friendly error bubble.
      const errorMessage: Message = {
        id: `error-${crypto.randomUUID()}`,
        role: "ai",
        content:
          "Sorry, something went wrong answering that. Please try again.",
        time: new Date().toLocaleTimeString([], {
          hour: "2-digit",
          minute: "2-digit",
        }),
      };

      setMessages((previousMessages) => [
        ...previousMessages,
        errorMessage,
      ]);
    } finally {
      setIsSending(false);
    }
  };

  /*
   * Start a new conversation for this document viewer.
   * Currently unused by the UI, but useful if you later add a
   * "New chat" button.
   */
  const startNewConversation = () => {
    setConversationId(null);
    setMessages([]);
    setInput("");
  };

  return (
    <div className="flex h-full overflow-hidden">
      {/* ================================================================
          DOCUMENT VIEWER
          ================================================================ */}
      <div className="flex-1 flex flex-col border-r border-border min-w-0 bg-[#525659]">
        {/* Document toolbar */}
        <div className="h-12 bg-[#3C3F41] flex items-center px-4 gap-3 shrink-0">
          <span className="text-white/70 text-xs font-medium truncate">
            {document?.name ?? "Loading..."}
          </span>

          <a
            href={fileUrl ?? "#"}
            download={document?.name ?? "document.pdf"}
            className="ml-auto text-white/60 hover:text-white transition-colors p-1.5 rounded"
            aria-label="Download document"
          >
            <Download className="w-3.5 h-3.5" />
          </a>
        </div>

        {/* Document content */}
        <div className="flex-1 overflow-y-auto py-6 px-4">
          {loading && (
            <div className="text-white">
              Loading document...
            </div>
          )}

          {error && (
            <div className="text-red-300">
              {error}
            </div>
          )}

          {!loading && !error && fileUrl && (
            <iframe
              src={fileUrl}
              className="w-full h-full min-h-[70vh] rounded-lg border-0"
              title={document?.name ?? "Document"}
            />
          )}
        </div>
      </div>

      {/* ================================================================
          AI CHAT PANEL
          ================================================================ */}
      <div className="w-96 flex flex-col shrink-0">
        {/* Chat header */}
        <div className="h-12 border-b border-border flex items-center px-4 gap-2 bg-card shrink-0">
          <Bot className="w-4 h-4 text-teal-600" />

          <span className="text-sm font-semibold text-foreground">
            Ask about this document
          </span>

          <Badge
            variant="teal"
            className="ml-auto"
          >
            Live
          </Badge>
        </div>

        {/* Chat messages */}
        <div className="flex-1 overflow-y-auto px-4 py-4 space-y-4">
          {messages.length === 0 && (
            <p className="text-sm text-muted-foreground">
              Ask a question about your documents.
            </p>
          )}

          {messages.map((msg) => (
            <ChatMessage
              key={msg.id}
              msg={msg}
            />
          ))}

          {isSending && <TypingIndicator />}

          <div ref={bottomRef} />
        </div>

        {/* Chat input */}
        <div className="p-3 border-t border-border bg-card">
          <div className="flex gap-2">
            <input
              value={input}
              onChange={(event) => setInput(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === "Enter" && !event.shiftKey) {
                  event.preventDefault();
                  sendMessage();
                }
              }}
              placeholder="Ask a question..."
              className="flex-1 h-9 px-3 bg-background border border-border rounded-lg text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/30 transition-all"
              disabled={isSending}
            />

            <Button
              variant="primary"
              size="icon"
              onClick={sendMessage}
              disabled={!input.trim() || isSending}
              aria-label="Send message"
            >
              <Send className="w-4 h-4" />
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}