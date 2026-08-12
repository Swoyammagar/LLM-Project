import { api } from "@/lib/axios";
import {
  DocumentItem,
  PaginatedDocuments,
  RawDocumentDto,
  RawDocumentListDto,
  mapDocument,
  mapPaginatedDocuments,
} from "@/types/document";

export const documentService = {
  upload: async (
    file: File,
    onProgress?: (percent: number) => void
  ): Promise<DocumentItem> => {
    const formData = new FormData();
    formData.append("file", file);

    const { data } = await api.post<{ data: RawDocumentDto }>("/document/upload", formData, {
      headers: { "Content-Type": "undefined" },
      onUploadProgress: (event) => {
        if (onProgress && event.total) {
          onProgress(Math.round((event.loaded * 100) / event.total));
        }
      },
    });

    return mapDocument(data.data);
  },

  list: async (pageNumber = 1, pageSize = 10): Promise<PaginatedDocuments> => {
    const { data } = await api.get<{ data: RawDocumentListDto }>("/document", {
      params: { pageNumber, pageSize },
    });
    return mapPaginatedDocuments(data.data);
  },

  getById: async (id: string): Promise<DocumentItem> => {
    const { data } = await api.get<{ data: RawDocumentDto }>(`/document/${id}`);
    return mapDocument(data.data);
  },

  remove: async (id: string): Promise<void> => {
    await api.delete(`/document/${id}`);
  },

  getFileById: async (id: string): Promise<Blob> => {
    const { data } = await api.get(`/document/${id}/file`, {
      responseType: "blob",
    });
    return data;
  }
};