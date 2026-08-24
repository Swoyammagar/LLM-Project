"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { SquarePen, Send, FileText, Eye, X, ChevronDown } from "lucide-react";
import { Button } from "@/components/ui2/Button";
import { ChatMessage } from "@/components/chat/ChatMessage";
import { TypingIndicator } from "@/components/chat/TypingIndicator";
import { SUGGESTED_QUESTIONS } from "@/lib/mock-data";
import { cn } from "@/lib/utils";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import {
  fetchConversations,
  fetchConversation,
  sendChatMessage,
  startNewConversation,
} from "@/store/slices/chatSlice";
import { fetchDocuments } from "@/store/slices/documentSlice";

export default function ChatPage() {
  const dispatch = useAppDispatch();
  const router = useRouter();

  const {
    conversations,
    conversationsLoading,
    activeConversationId,
    messages,
    messagesLoading,
    sending,
  } = useAppSelector((state) => state.chat);
  const { items: documents } = useAppSelector((state) => state.documents);

  const [input, setInput] = useState("");
  const [selectedDocumentId, setSelectedDocumentId] = useState<string | null>(null);
  const [pickerOpen, setPickerOpen] = useState(false);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    dispatch(fetchConversations());
    dispatch(fetchDocuments({ pageNumber: 1, pageSize: 100 }));
  }, [dispatch]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages, sending]);

  const selectedDocument = documents.find((d) => d.id === selectedDocumentId) ?? null;

  const sendMessage = useCallback(
    async (text?: string) => {
      const content = (text ?? input).trim();
      if (!content) return;
      setInput("");
      const result = await dispatch(
        sendChatMessage({ question: content, documentId: selectedDocumentId ?? undefined })
      );
      if (sendChatMessage.fulfilled.match(result)) {
        dispatch(fetchConversations());
      }
    },
    [dispatch, input, selectedDocumentId]
  );

  return (
    <div className="flex h-full overflow-hidden">
      <div className="w-64 border-r border-border flex flex-col bg-sidebar flex-shrink-0">
        <div className="p-3 border-b border-sidebar-border">
          <Button
            variant="primary"
            size="md"
            className="w-full"
            icon={<SquarePen className="w-3.5 h-3.5" />}
            onClick={() => {
              dispatch(startNewConversation());
              setSelectedDocumentId(null);
            }}
          >
            New Chat
          </Button>
        </div>
        <div className="flex-1 overflow-y-auto py-2 px-2 space-y-0.5">
          {conversationsLoading && (
            <p className="text-xs text-muted-foreground px-3 py-2">Loading conversations...</p>
          )}
          {!conversationsLoading && conversations.length === 0 && (
            <p className="text-xs text-muted-foreground px-3 py-2">No conversations yet.</p>
          )}
          {conversations.map((c) => (
            <button
              key={c.id}
              onClick={() => dispatch(fetchConversation(c.id))}
              className={cn(
                "w-full text-left px-3 py-2.5 rounded-lg transition-all",
                activeConversationId === c.id ? "bg-accent text-accent-foreground" : "hover:bg-muted"
              )}
            >
              <p className="text-sm font-medium text-foreground truncate">{c.title}</p>
              <div className="flex items-center justify-between mt-1">
                <span className="text-[10px] text-muted-foreground">
                  {new Date(c.updatedAt).toLocaleDateString()}
                </span>
                <span className="text-[10px] text-muted-foreground">{c.messageCount} msgs</span>
              </div>
            </button>
          ))}
        </div>
      </div>

      <div className="flex-1 flex flex-col min-w-0">
        <div className="h-14 border-b border-border flex items-center px-5 gap-3 flex-shrink-0 bg-card">
          <div className="min-w-0">
            <p className="text-sm font-semibold text-foreground truncate">
              {activeConversationId
                ? conversations.find((c) => c.id === activeConversationId)?.title ?? "Conversation"
                : "New Chat"}
            </p>
            <p className="text-xs text-muted-foreground">
              {selectedDocument
                ? `Scoped to ${selectedDocument.name}`
                : "Answers are drawn from all of your uploaded documents"}
            </p>
          </div>

          <div className="ml-auto flex items-center gap-2 relative">
            {selectedDocument && (
              <Button
                variant="outline"
                size="sm"
                icon={<Eye className="w-3.5 h-3.5" />}
                onClick={() => router.push(`/documents/${selectedDocument.id}`)}
              >
                View Doc
              </Button>
            )}

            <div className="relative">
              <Button
                variant="outline"
                size="sm"
                icon={<FileText className="w-3.5 h-3.5" />}
                onClick={() => setPickerOpen((prev) => !prev)}
              >
                {selectedDocument ? selectedDocument.name : "All Documents"}
                <ChevronDown className="w-3 h-3 ml-1" />
              </Button>

              {pickerOpen && (
                <div className="absolute right-0 top-full mt-1 w-64 bg-card border border-border rounded-lg shadow-lg z-10 py-1 max-h-72 overflow-y-auto">
                  <button
                    onClick={() => {
                      setSelectedDocumentId(null);
                      setPickerOpen(false);
                    }}
                    className={cn(
                      "w-full text-left px-3 py-2 text-sm hover:bg-muted transition-colors",
                      !selectedDocumentId && "bg-accent text-accent-foreground"
                    )}
                  >
                    All Documents
                  </button>
                  <div className="border-t border-border my-1" />
                  {documents.length === 0 && (
                    <p className="px-3 py-2 text-xs text-muted-foreground">No documents uploaded yet.</p>
                  )}
                  {documents.map((doc) => (
                    <button
                      key={doc.id}
                      onClick={() => {
                        setSelectedDocumentId(doc.id);
                        setPickerOpen(false);
                      }}
                      className={cn(
                        "w-full text-left px-3 py-2 text-sm hover:bg-muted transition-colors truncate",
                        selectedDocumentId === doc.id && "bg-accent text-accent-foreground"
                      )}
                    >
                      {doc.name}
                    </button>
                  ))}
                </div>
              )}
            </div>

            {selectedDocument && (
              <button
                onClick={() => setSelectedDocumentId(null)}
                className="text-muted-foreground hover:text-foreground transition-colors p-1"
                title="Clear document scope"
              >
                <X className="w-3.5 h-3.5" />
              </button>
            )}
          </div>
        </div>

        <div className="flex-1 overflow-y-auto px-6 py-5 space-y-5">
          {messagesLoading && <p className="text-sm text-muted-foreground">Loading messages...</p>}
          {!messagesLoading && messages.length === 0 && (
            <p className="text-sm text-muted-foreground">Ask a question to get started.</p>
          )}
          {messages.map((msg) => (
            <ChatMessage key={msg.id} msg={msg} />
          ))}
          {sending && <TypingIndicator />}
          <div ref={bottomRef} />
        </div>

        {!sending && messages.length === 0 && (
          <div className="px-6 pb-2 flex flex-wrap gap-2">
            {SUGGESTED_QUESTIONS.map((q, i) => (
              <button
                key={i}
                onClick={() => sendMessage(q)}
                className="px-3 py-1.5 bg-muted text-muted-foreground text-xs rounded-full hover:bg-accent hover:text-accent-foreground border border-border hover:border-primary/30 transition-all"
              >
                {q}
              </button>
            ))}
          </div>
        )}

        <div className="p-4 border-t border-border bg-card">
          <div className="flex items-end gap-3 bg-background border border-border rounded-xl p-3 focus-within:border-primary/40 focus-within:ring-2 focus-within:ring-primary/10 transition-all">
            <textarea
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter" && !e.shiftKey) {
                  e.preventDefault();
                  sendMessage();
                }
              }}
              placeholder={
                selectedDocument
                  ? `Ask anything about ${selectedDocument.name}...`
                  : "Ask anything about your documents..."
              }
              rows={1}
              className="flex-1 resize-none bg-transparent text-sm text-foreground placeholder:text-muted-foreground outline-none leading-relaxed max-h-32"
            />
            <Button
              variant="primary"
              size="icon"
              className="flex-shrink-0"
              onClick={() => sendMessage()}
              disabled={!input.trim() || sending}
            >
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