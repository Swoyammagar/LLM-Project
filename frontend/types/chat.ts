import type { Message } from "@/lib/types";

// --- Raw backend shapes (System.Text.Json defaults to camelCase) ---
export interface RawRetrievedChunk {
  chunkId: string;
  documentId: string;
  retrievalIndex: number;
  content: string;
  similarityScore: number;
}

export interface RawChatResponse {
  question: string;
  answer: string;
  retrievedChunks: RawRetrievedChunk[];
  contextTokensUsed: number;
  generatedAt: string;
  conversationId: string;
  messageId: string;
}

export interface RawConversationDto {
  id: string;
  title: string;
  description: string | null;
  messageCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface RawMessageDto {
  id: string;
  question: string;
  answer: string;
  chunksUsed: number;
  tokensUsed: number;
  createdAt: string;
}

export interface RawConversationDetailDto {
  id: string;
  title: string;
  description: string | null;
  createdAt: string;
  updatedAt: string;
  messages: RawMessageDto[];
}

// --- Frontend domain types ---
export interface RetrievedChunk {
  chunkId: string;
  documentId: string;
  retrievalIndex: number;
  content: string;
  similarityScore: number;
}

export interface ConversationSummary {
  id: string;
  title: string;
  description: string | null;
  messageCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface ConversationDetail {
  id: string;
  title: string;
  description: string | null;
  createdAt: string;
  updatedAt: string;
  messages: Message[]; // flattened — each backend Message becomes 2 chat bubbles
}

function formatTime(iso: string): string {
  return new Date(iso).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}

// A backend "Message" row is one Q+A pair — split into user + ai bubbles for the UI.
export function mapMessageDtoToBubbles(raw: RawMessageDto): Message[] {
  const time = formatTime(raw.createdAt);
  return [
    { id: `${raw.id}-q`, role: "user", content: raw.question, time },
    {
      id: `${raw.id}-a`,
      role: "ai",
      content: raw.answer,
      time,
      sources: raw.chunksUsed > 0 ? [`${raw.chunksUsed} source${raw.chunksUsed > 1 ? "s" : ""} referenced`] : undefined,
    },
  ];
}

export function mapConversationDetail(raw: RawConversationDetailDto): ConversationDetail {
  return {
    id: raw.id,
    title: raw.title,
    description: raw.description,
    createdAt: raw.createdAt,
    updatedAt: raw.updatedAt,
    messages: raw.messages.flatMap(mapMessageDtoToBubbles),
  };
}

export function mapConversationSummary(raw: RawConversationDto): ConversationSummary {
  return {
    id: raw.id,
    title: raw.title,
    description: raw.description,
    messageCount: raw.messageCount,
    createdAt: raw.createdAt,
    updatedAt: raw.updatedAt,
  };
}

// Live chat response -> just the AI bubble (the user bubble is added optimistically
// by the caller the moment they hit send, before the network round-trip completes).
export function mapChatResponseToAiMessage(raw: RawChatResponse): Message {
  return {
    id: raw.messageId,
    role: "ai",
    content: raw.answer,
    time: formatTime(raw.generatedAt),
    sources: raw.retrievedChunks.length
      ? [`${raw.retrievedChunks.length} source${raw.retrievedChunks.length > 1 ? "s" : ""} referenced`]
      : undefined,
  };
}