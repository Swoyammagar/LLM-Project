import type { Document, ChatSession, Message } from "./types";

export const DOCUMENTS: Document[] = [
  { id: "1", name: "Q3 Financial Report.pdf", date: "Jul 10, 2026", size: "2.4 MB", status: "ready", type: "PDF", pages: 24 },
  { id: "2", name: "Product Roadmap 2026.docx", date: "Jul 8, 2026", size: "1.1 MB", status: "ready", type: "DOCX", pages: 12 },
  { id: "3", name: "Engineering Spec v2.pdf", date: "Jul 5, 2026", size: "4.7 MB", status: "processing", type: "PDF", pages: 48 },
  { id: "4", name: "Customer Research Notes.txt", date: "Jul 1, 2026", size: "0.3 MB", status: "ready", type: "TXT", pages: 6 },
  { id: "5", name: "Legal Agreement Draft.pdf", date: "Jun 28, 2026", size: "1.8 MB", status: "error", type: "PDF", pages: 18 },
  { id: "6", name: "Sales Pipeline Q3.docx", date: "Jun 25, 2026", size: "0.9 MB", status: "ready", type: "DOCX", pages: 9 },
];

export const CHAT_SESSIONS: ChatSession[] = [
  { id: "1", title: "Q3 Financial Analysis", document: "Q3 Financial Report.pdf", date: "Jul 14", messages: 12 },
  { id: "2", title: "Product Feature Questions", document: "Product Roadmap 2026.docx", date: "Jul 12", messages: 8 },
  { id: "3", title: "Engineering Spec Review", document: "Engineering Spec v2.pdf", date: "Jul 9", messages: 15 },
  { id: "4", title: "Customer Insights Summary", document: "Customer Research Notes.txt", date: "Jul 3", messages: 6 },
];

export const INITIAL_MESSAGES: Message[] = [
  {
    id: "1", role: "user",
    content: "What were the key financial highlights from Q3?",
    time: "2:30 PM",
  },
  {
    id: "2", role: "ai",
    content: "Based on the Q3 Financial Report, here are the key highlights:\n\n**Revenue:** Total revenue reached $48.2M, representing a **23% year-over-year increase** compared to Q3 2025.\n\n**Gross Margin:** Improved to 67.3%, up from 64.1% in Q2 2026, driven by improved operational efficiency.\n\n**Operating Expenses:** Increased by 18% to $31.4M, primarily from accelerated R&D investment in AI features.",
    time: "2:30 PM",
    sources: ["Page 3 — Executive Summary", "Page 7 — Revenue Breakdown"],
  },
  {
    id: "3", role: "user",
    content: "What drove the revenue growth?",
    time: "2:32 PM",
  },
  {
    id: "4", role: "ai",
    content: "Revenue growth was driven by three primary factors:\n\n1. **New Customer Acquisition** — Enterprise segment grew by 34%, adding 127 new enterprise accounts in Q3.\n\n2. **Expansion Revenue** — Existing customer upsells contributed $12.1M (25% of total revenue), with NRR reaching 118%.\n\n3. **Product Expansion** — The new Analytics suite launched in July contributed $3.2M in its first full quarter.",
    time: "2:32 PM",
    sources: ["Page 9 — Growth Drivers", "Page 11 — Segment Analysis"],
  },
];

export const SUGGESTED_QUESTIONS = [
  "What are the key risks mentioned?",
  "How does Q3 compare to Q2?",
  "What is the Q4 revenue forecast?",
];

export const AI_RESPONSES = [
  "Based on the document, I can see that this section covers several important aspects. The data indicates a positive trend over the measured period, with key metrics showing consistent improvement across all major segments.",
  "According to the document, the primary factors here include operational efficiency gains, strategic investments in growth areas, and improved customer retention metrics that collectively drive the observed outcomes.",
  "The document outlines three main considerations for this topic: first, the structural changes implemented in early 2026; second, market dynamics that have shifted competitive positioning; and third, internal process improvements that reduced overhead by approximately 15%.",
];
