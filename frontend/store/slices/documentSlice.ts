import { createAsyncThunk, createSlice, PayloadAction } from "@reduxjs/toolkit";
import { documentService } from "@/services/documentService";
import { DocumentItem } from "@/types/document";

interface DocumentState {
  items: DocumentItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  loading: boolean;
  deletingIds: string[]; // tracks in-flight deletes so the row can show a spinner/disable
  error: string | null;
}

const initialState: DocumentState = {
  items: [],
  totalCount: 0,
  pageNumber: 1,
  pageSize: 10,
  totalPages: 0,
  loading: false,
  deletingIds: [],
  error: null,
};

function extractErrorMessage(err: any): string {
  return err?.response?.data?.message || "Something went wrong. Please try again.";
}

export const fetchDocuments = createAsyncThunk<
  Awaited<ReturnType<typeof documentService.list>>,
  { pageNumber?: number; pageSize?: number } | void,
  { rejectValue: string }
>("documents/fetchDocuments", async (args, { rejectWithValue }) => {
  try {
    return await documentService.list(args?.pageNumber, args?.pageSize);
  } catch (err) {
    return rejectWithValue(extractErrorMessage(err));
  }
});

export const deleteDocument = createAsyncThunk<string, string, { rejectValue: string }>(
  "documents/deleteDocument",
  async (id, { rejectWithValue }) => {
    try {
      await documentService.remove(id);
      return id;
    } catch (err) {
      return rejectWithValue(extractErrorMessage(err));
    }
  }
);

const documentSlice = createSlice({
  name: "documents",
  initialState,
  reducers: {
    // Called by the upload page once a file finishes uploading successfully —
    // avoids a full refetch just to show the new file in the list.
    documentAdded(state, action: PayloadAction<DocumentItem>) {
      state.items.unshift(action.payload);
      state.totalCount += 1;
    },
    clearDocumentsError(state) {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchDocuments.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchDocuments.fulfilled, (state, action) => {
        state.loading = false;
        state.items = action.payload.items;
        state.totalCount = action.payload.totalCount;
        state.pageNumber = action.payload.pageNumber;
        state.pageSize = action.payload.pageSize;
        state.totalPages = action.payload.totalPages;
      })
      .addCase(fetchDocuments.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload ?? "Failed to load documents";
      })

      .addCase(deleteDocument.pending, (state, action) => {
        state.deletingIds.push(action.meta.arg);
      })
      .addCase(deleteDocument.fulfilled, (state, action) => {
        state.deletingIds = state.deletingIds.filter((id) => id !== action.payload);
        state.items = state.items.filter((doc) => doc.id !== action.payload);
        state.totalCount = Math.max(0, state.totalCount - 1);
      })
      .addCase(deleteDocument.rejected, (state, action) => {
        state.deletingIds = state.deletingIds.filter((id) => id !== action.meta.arg);
        state.error = action.payload ?? "Failed to delete document";
      });
  },
});

export const { documentAdded, clearDocumentsError } = documentSlice.actions;
export default documentSlice.reducer;