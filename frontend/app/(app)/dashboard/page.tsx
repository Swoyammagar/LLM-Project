"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { Upload, FileUp, ChevronRight, Eye, FileText, Database, MessageSquare, Bot } from "lucide-react";
import { Button } from "@/components/ui2/Button";
import { CardShell } from "@/components/ui2/CardShell";
import { StatusDot } from "@/components/ui2/StatusDot";
import { FileTypeBadge } from "@/components/ui2/FileTypeBadge";
import { StatCard } from "@/components/dashboard/StatCard";
import { DOCUMENTS, CHAT_SESSIONS } from "@/lib/mock-data";

export default function DashboardPage() {
  const router = useRouter();

  return (
    <div className="p-6 max-w-6xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold text-foreground">Good morning, Sarah ☀️</h1>
          <p className="text-sm text-muted-foreground mt-0.5">Tuesday, July 15 — Here's what's happening with your documents.</p>
        </div>
        <Button variant="primary" size="md" icon={<Upload className="w-3.5 h-3.5" />} onClick={() => router.push("/upload")}>
          Upload Document
        </Button>
      </div>

      <div className="grid grid-cols-3 gap-4">
        <StatCard icon={<FileText className="w-5 h-5 text-blue-600" />} label="Documents Uploaded" value="47" sub="+3 this week" color="bg-blue-50 dark:bg-blue-900/20" />
        <StatCard icon={<MessageSquare className="w-5 h-5 text-teal-600" />} label="Questions Asked" value="1,234" sub="+89 today" color="bg-teal-50 dark:bg-teal-900/20" />
        <StatCard icon={<Database className="w-5 h-5 text-purple-600" />} label="Storage Used" value="2.8 GB" sub="of 10 GB · 28% used" color="bg-purple-50 dark:bg-purple-900/20" />
      </div>

      <div className="grid grid-cols-3 gap-4">
        <Link href="/upload" className="col-span-1 block">
          <CardShell className="p-5 h-full border-dashed border-2 border-primary/30 bg-accent/30 hover:border-primary/60 hover:bg-accent/50 transition-all cursor-pointer group">
            <div className="flex flex-col items-center justify-center gap-3 py-4 text-center">
              <div className="w-12 h-12 rounded-xl bg-primary/10 flex items-center justify-center group-hover:bg-primary/20 transition-colors">
                <FileUp className="w-6 h-6 text-primary" />
              </div>
              <div>
                <p className="font-medium text-foreground text-sm">Upload a Document</p>
                <p className="text-xs text-muted-foreground mt-1">PDF, DOCX, or TXT · up to 50 MB</p>
              </div>
              <Button variant="outline" size="sm">Choose File</Button>
            </div>
          </CardShell>
        </Link>

        <CardShell className="col-span-2 p-5">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-sm font-semibold text-foreground">Recent Documents</h2>
            <Link href="/documents" className="text-xs text-primary hover:underline flex items-center gap-1">
              View all <ChevronRight className="w-3 h-3" />
            </Link>
          </div>
          <div className="space-y-2">
            {DOCUMENTS.slice(0, 4).map(doc => (
              <Link key={doc.id} href={`/documents/${doc.id}`} className="flex items-center gap-3 p-2.5 rounded-lg hover:bg-muted transition-colors cursor-pointer group">
                <FileTypeBadge type={doc.type} />
                <div className="flex-1 min-w-0">
                  <p className="text-sm text-foreground truncate font-medium">{doc.name}</p>
                  <p className="text-xs text-muted-foreground">{doc.date} · {doc.size}</p>
                </div>
                <StatusDot status={doc.status} />
                <Button variant="ghost" size="icon" className="opacity-0 group-hover:opacity-100 h-7 w-7">
                  <Eye className="w-3.5 h-3.5" />
                </Button>
              </Link>
            ))}
          </div>
        </CardShell>
      </div>

      <CardShell className="p-5">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-sm font-semibold text-foreground">Recent Chats</h2>
          <Link href="/chat" className="text-xs text-primary hover:underline flex items-center gap-1">
            View all <ChevronRight className="w-3 h-3" />
          </Link>
        </div>
        <div className="grid grid-cols-2 gap-3">
          {CHAT_SESSIONS.map(session => (
            <Link key={session.id} href={`/chat?session=${session.id}`} className="flex items-start gap-3 p-3 rounded-lg border border-border hover:border-primary/30 hover:bg-accent/30 transition-all cursor-pointer group">
              <div className="w-8 h-8 rounded-lg bg-teal-50 dark:bg-teal-900/20 flex items-center justify-center flex-shrink-0">
                <Bot className="w-4 h-4 text-teal-600" />
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-foreground truncate">{session.title}</p>
                <p className="text-xs text-muted-foreground truncate mt-0.5">{session.document}</p>
                <div className="flex items-center gap-2 mt-1.5">
                  <span className="text-[10px] text-muted-foreground">{session.date}</span>
                  <span className="text-[10px] text-muted-foreground">·</span>
                  <span className="text-[10px] text-muted-foreground">{session.messages} messages</span>
                </div>
              </div>
              <ChevronRight className="w-3.5 h-3.5 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity mt-1" />
            </Link>
          ))}
        </div>
      </CardShell>
    </div>
  );
}
