export interface Document {
  id: string;
  name: string;
  date: string;
  size: string;
  status: "ready" | "processing" | "error";
  type: "PDF" | "DOCX" | "TXT";
  pages: number;
}

export interface ChatSession {
  id: string;
  title: string;
  document: string;
  date: string;
  messages: number;
}

export interface Message {
  id: string;
  role: "user" | "ai";
  content: string;
  time: string;
  sources?: string[];
}
