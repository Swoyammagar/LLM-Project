import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { chatService } from "@/services/chatService";
import { ConversationSummary } from "@/types/chat";
import type { Message } from "@/lib/types";
import type { RootState } from "@/store/store";

interface ChatState {
  conversations: ConversationSummary[];
  conversationsLoading: boolean;
  activeConversationId: string | null;
  messages: Message[];
  messagesLoading: boolean;
  sending: boolean;
  error: string | null;
}

const initialState: ChatState = {
  conversations: [],
  conversationsLoading: false,
  activeConversationId: null,
  messages: [],
  messagesLoading: false,
  sending: false,
  error: null,
};

function extractErrorMessage(err: any): string {
  return (
    err?.response?.data?.message ||
    err?.response?.data ||
    "Something went wrong. Please try again."
  );
}

function nowLabel(): string {
  return new Date().toLocaleTimeString([], {
    hour: "2-digit",
    minute: "2-digit",
  });
}

export const fetchConversations = createAsyncThunk<
  ConversationSummary[],
  void,
  { rejectValue: string }
>(
  "chat/fetchConversations",
  async (_, { rejectWithValue }) => {
    try {
      return await chatService.listConversations();
    } catch (err) {
      return rejectWithValue(extractErrorMessage(err));
    }
  }
);

export const fetchConversation = createAsyncThunk<
  Awaited<ReturnType<typeof chatService.getConversation>>,
  string,
  { rejectValue: string }
>(
  "chat/fetchConversation",
  async (id, { rejectWithValue }) => {
    try {
      return await chatService.getConversation(id);
    } catch (err) {
      return rejectWithValue(extractErrorMessage(err));
    }
  }
);

export const sendChatMessage = createAsyncThunk<
  { conversationId: string; aiMessage: Message },
  {
    question: string;
    maxContextChunks?: number;
    similarityThreshold?: number;
    documentId?: string;
  },
  {
    state: RootState;
    rejectValue: string;
  }
>(
  "chat/sendMessage",
  async (
    { question, maxContextChunks, similarityThreshold, documentId },
    { getState, rejectWithValue }
  ) => {
    try {
      const conversationId =
        getState().chat.activeConversationId ?? undefined;

      return await chatService.sendMessage({
        question,
        conversationId,
        maxContextChunks,
        similarityThreshold,
        documentId,
      });
    } catch (err) {
      return rejectWithValue(extractErrorMessage(err));
    }
  }
);

export const deleteConversation = createAsyncThunk<
  string,
  string,
  { rejectValue: string }
>(
  "chat/deleteConversation",
  async (id, { rejectWithValue }) => {
    try {
      await chatService.deleteConversation(id);
      return id;
    } catch (err) {
      return rejectWithValue(extractErrorMessage(err));
    }
  }
);

const chatSlice = createSlice({
  name: "chat",
  initialState,
  reducers: {
    startNewConversation(state) {
      state.activeConversationId = null;
      state.messages = [];
    },

    clearChatError(state) {
      state.error = null;
    },
  },

  extraReducers: (builder) => {
    builder
      // Conversations
      .addCase(fetchConversations.pending, (state) => {
        state.conversationsLoading = true;
        state.error = null;
      })
      .addCase(fetchConversations.fulfilled, (state, action) => {
        state.conversationsLoading = false;
        state.conversations = action.payload;
      })
      .addCase(fetchConversations.rejected, (state, action) => {
        state.conversationsLoading = false;
        state.error = action.payload ?? "Failed to load conversations";
      })

      // Conversation messages
      .addCase(fetchConversation.pending, (state) => {
        state.messagesLoading = true;
        state.error = null;
        state.messages = [];
      })
      .addCase(fetchConversation.fulfilled, (state, action) => {
        state.messagesLoading = false;
        state.activeConversationId = action.payload.id;
        state.messages = action.payload.messages;
      })
      .addCase(fetchConversation.rejected, (state, action) => {
        state.messagesLoading = false;
        state.error = action.payload ?? "Failed to load conversation";
      })

      // Send message
      .addCase(sendChatMessage.pending, (state, action) => {
        state.sending = true;
        state.error = null;

        state.messages.push({
          id: `temp-user-${Date.now()}`,
          role: "user",
          content: action.meta.arg.question,
          time: nowLabel(),
        });
      })
      .addCase(sendChatMessage.fulfilled, (state, action) => {
        state.sending = false;

        state.activeConversationId = action.payload.conversationId;
        state.messages.push(action.payload.aiMessage);
      })
      .addCase(sendChatMessage.rejected, (state, action) => {
        state.sending = false;
        state.error = action.payload ?? "Failed to send message";

        const last = state.messages[state.messages.length - 1];

        if (last?.id.startsWith("temp-user-")) {
          state.messages.pop();
        }
      })

      // Delete conversation
      .addCase(deleteConversation.fulfilled, (state, action) => {
        state.conversations = state.conversations.filter(
          (c) => c.id !== action.payload
        );

        if (state.activeConversationId === action.payload) {
          state.activeConversationId = null;
          state.messages = [];
        }
      })
      .addCase(deleteConversation.rejected, (state, action) => {
        state.error = action.payload ?? "Failed to delete conversation";
      });
  },
});

export const { startNewConversation, clearChatError } =
  chatSlice.actions;

export default chatSlice.reducer;