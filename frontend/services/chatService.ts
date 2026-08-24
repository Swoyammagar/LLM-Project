import { api } from "@/lib/axios";
import {
  RawChatResponse,
  RawConversationDetailDto,
  RawConversationDto,
  ConversationDetail,
  ConversationSummary,
  mapChatResponseToAiMessage,
  mapConversationDetail,
  mapConversationSummary,
} from "@/types/chat";
import type { Message } from "@/lib/types";

interface SendMessageParams {
  question: string;
  conversationId?: string;
  maxContextChunks?: number;
  similarityThreshold?: number;
  documentId?: string;
}

interface SendMessageResult {
  conversationId: string;
  aiMessage: Message;
}

export const chatService = {
  sendMessage: async ({
    question,
    conversationId,
    maxContextChunks,
    similarityThreshold,
    documentId,
  }: SendMessageParams): Promise<SendMessageResult> => {
    const { data } = await api.post<RawChatResponse>(
      "/chat/chat",
      { question, documentId },
      { params: { conversationId, maxContextChunks, similarityThreshold } }
    );
    return {
      conversationId: data.conversationId,
      aiMessage: mapChatResponseToAiMessage(data),
    };
  },

  listConversations: async (skip = 0, take = 20): Promise<ConversationSummary[]> => {
    const { data } = await api.get<RawConversationDto[]>("/chat/conversations", {
      params: { skip, take },
    });
    return data.map(mapConversationSummary);
  },

  getConversation: async (id: string): Promise<ConversationDetail> => {
    const { data } = await api.get<RawConversationDetailDto>(`/chat/conversations/${id}`);
    return mapConversationDetail(data);
  },

  deleteConversation: async (id: string): Promise<void> => {
    await api.delete(`/chat/conversations/${id}`);
  },
};