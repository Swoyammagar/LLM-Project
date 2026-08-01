export interface DocumentItem {
  id: string;
  name: string;
  extension: string;   // "PDF" | "DOCX" | "TXT" — derived, not from backend
  contentType: string;
  sizeBytes: number;
  uploadDate: string;
}

export interface PaginatedDocuments {
  items: DocumentItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number; // computed client-side, backend doesn't send this
}

// --- Raw backend shapes (match DocumentDto / DocumentListDto exactly) ---
export interface RawDocumentDto {
  id: string;
  originalFileName: string;
  fileSize: number;
  contentType: string;
  uploadDate: string;
}

export interface RawDocumentListDto {
  documents: RawDocumentDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

// Derives a display extension from filename, falling back to contentType
function deriveExtension(fileName: string, contentType: string): string {
  const fromName = fileName.split(".").pop()?.toUpperCase();
  if (fromName && fromName.length <= 5) return fromName;

  if (contentType.includes("pdf")) return "PDF";
  if (contentType.includes("word") || contentType.includes("docx")) return "DOCX";
  if (contentType.includes("text")) return "TXT";
  return "FILE";
}

// Single source of truth for backend -> frontend shape translation.
export function mapDocument(raw: RawDocumentDto): DocumentItem {
  return {
    id: raw.id,
    name: raw.originalFileName,
    extension: deriveExtension(raw.originalFileName, raw.contentType),
    contentType: raw.contentType,
    sizeBytes: raw.fileSize,
    uploadDate: raw.uploadDate,
  };
}

export function mapPaginatedDocuments(raw: RawDocumentListDto): PaginatedDocuments {
  return {
    items: raw.documents.map(mapDocument),
    totalCount: raw.totalCount,
    pageNumber: raw.pageNumber,
    pageSize: raw.pageSize,
    totalPages: Math.max(1, Math.ceil(raw.totalCount / raw.pageSize)),
  };
}