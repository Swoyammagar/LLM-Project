"use client";

import { useCallback, useRef, useState } from "react";
import { toast } from "sonner";
import { Upload, FileUp, FileText, CheckCircle2, Trash2, XCircle } from "lucide-react";
import { Button } from "@/components/ui2/Button";
import { CardShell } from "@/components/ui2/CardShell";
import { cn, formatBytes } from "@/lib/utils";
import { documentService } from "@/services/documentService";
import { useAppDispatch } from "@/store/hooks";
import { documentAdded } from "@/store/slices/documentSlice";

interface UploadTask {
  id: string;
  file: File;
  progress: number;
  status: "uploading" | "done" | "error";
  errorMessage?: string;
}

const ACCEPTED_EXTENSIONS = [".pdf", ".docx", ".txt"];
const MAX_FILE_SIZE_BYTES = 50 * 1024 * 1024; // 50 MB

export default function UploadPage() {
  const dispatch = useAppDispatch();
  const [dragOver, setDragOver] = useState(false);
  const [tasks, setTasks] = useState<UploadTask[]>([]);
  const inputRef = useRef<HTMLInputElement>(null);

  const validateFile = (file: File): string | null => {
    const ext = "." + (file.name.split(".").pop()?.toLowerCase() ?? "");
    if (!ACCEPTED_EXTENSIONS.includes(ext)) {
      return `${file.name}: unsupported file type`;
    }
    if (file.size > MAX_FILE_SIZE_BYTES) {
      return `${file.name}: exceeds 50 MB limit`;
    }
    return null;
  };

  const uploadFile = useCallback(
    async (file: File) => {
      const taskId = Math.random().toString(36).slice(2);
      setTasks((prev) => [...prev, { id: taskId, file, progress: 0, status: "uploading" }]);

      try {
        const doc = await documentService.upload(file, (percent) => {
          setTasks((prev) =>
            prev.map((t) => (t.id === taskId ? { ...t, progress: percent } : t))
          );
        });

        setTasks((prev) =>
          prev.map((t) => (t.id === taskId ? { ...t, progress: 100, status: "done" } : t))
        );
        dispatch(documentAdded(doc));
        toast.success(`${file.name} uploaded successfully`);
      } catch (err: any) {
        const message = err?.response?.data?.message || "Upload failed";
        setTasks((prev) =>
          prev.map((t) => (t.id === taskId ? { ...t, status: "error", errorMessage: message } : t))
        );
        toast.error(`${file.name}: ${message}`);
      }
    },
    [dispatch]
  );

  const handleFiles = useCallback(
    (fileList: FileList) => {
      Array.from(fileList).forEach((file) => {
        const validationError = validateFile(file);
        if (validationError) {
          toast.error(validationError);
          return;
        }
        uploadFile(file);
      });
    },
    [uploadFile]
  );

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(false);
    handleFiles(e.dataTransfer.files);
  };

  return (
    <div className="p-6 max-w-3xl mx-auto space-y-6">
      <div>
        <h1 className="text-xl font-semibold text-foreground">Upload Documents</h1>
        <p className="text-sm text-muted-foreground mt-0.5">
          Upload PDF, DOCX, or TXT files to start chatting with your content.
        </p>
      </div>

      <div
        onDragOver={(e) => {
          e.preventDefault();
          setDragOver(true);
        }}
        onDragLeave={() => setDragOver(false)}
        onDrop={handleDrop}
        className={cn(
          "relative border-2 border-dashed rounded-xl p-12 flex flex-col items-center justify-center gap-4 transition-all duration-200 cursor-pointer",
          dragOver
            ? "border-primary bg-accent/50 scale-[1.01]"
            : "border-border hover:border-primary/50 hover:bg-muted/50"
        )}
        onClick={() => inputRef.current?.click()}
      >
        <input
          ref={inputRef}
          type="file"
          multiple
          accept={ACCEPTED_EXTENSIONS.join(",")}
          className="hidden"
          onChange={(e) => {
            if (e.target.files) handleFiles(e.target.files);
            e.target.value = "";
          }}
        />
        <div
          className={cn(
            "w-16 h-16 rounded-2xl flex items-center justify-center transition-all",
            dragOver ? "bg-primary text-white scale-110" : "bg-muted text-muted-foreground"
          )}
        >
          <FileUp className="w-8 h-8" />
        </div>
        <div className="text-center">
          <p className="font-semibold text-foreground">
            {dragOver ? "Drop files here" : "Drag & drop your files"}
          </p>
          <p className="text-sm text-muted-foreground mt-1">or click to browse from your computer</p>
        </div>
        <div className="flex items-center gap-2">
          {["PDF", "DOCX", "TXT"].map((type) => (
            <span key={type} className="px-2.5 py-1 bg-muted rounded-full text-xs font-medium text-muted-foreground">
              {type}
            </span>
          ))}
          <span className="text-xs text-muted-foreground">· Max 50 MB per file</span>
        </div>
        {!dragOver && (
          <Button
            variant="outline"
            size="md"
            icon={<Upload className="w-3.5 h-3.5" />}
            onClick={(e) => {
              e.stopPropagation();
              inputRef.current?.click();
            }}
          >
            Browse Files
          </Button>
        )}
      </div>

      {tasks.length > 0 && (
        <CardShell>
          <div className="px-5 py-3 border-b border-border">
            <h2 className="text-sm font-semibold text-foreground">
              Uploading <span className="text-muted-foreground font-normal ml-1">({tasks.length})</span>
            </h2>
          </div>
          <div className="divide-y divide-border">
            {tasks.map((task) => (
              <div key={task.id} className="flex items-center gap-4 px-5 py-3.5 group">
                <div className="w-9 h-9 rounded-lg bg-muted flex items-center justify-center flex-shrink-0">
                  <FileText className="w-4 h-4 text-muted-foreground" />
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-medium text-foreground truncate">{task.file.name}</span>
                    <span className="text-xs text-muted-foreground ml-4 flex-shrink-0">
                      {formatBytes(task.file.size)}
                    </span>
                  </div>

                  {task.status === "uploading" && (
                    <div className="mt-1.5">
                      <div className="h-1.5 bg-muted rounded-full overflow-hidden">
                        <div
                          className="h-full bg-primary rounded-full transition-all duration-300"
                          style={{ width: `${task.progress}%` }}
                        />
                      </div>
                      <p className="text-xs text-muted-foreground mt-0.5">{task.progress}% uploading...</p>
                    </div>
                  )}

                  {task.status === "done" && (
                    <div className="flex items-center gap-1.5 mt-0.5">
                      <CheckCircle2 className="w-3.5 h-3.5 text-emerald-500" />
                      <span className="text-xs text-emerald-600">Upload complete</span>
                    </div>
                  )}

                  {task.status === "error" && (
                    <div className="flex items-center gap-1.5 mt-0.5">
                      <XCircle className="w-3.5 h-3.5 text-destructive" />
                      <span className="text-xs text-destructive">{task.errorMessage ?? "Upload failed"}</span>
                    </div>
                  )}
                </div>
                <Button
                  variant="ghost"
                  size="icon"
                  className="opacity-0 group-hover:opacity-100 h-7 w-7 text-muted-foreground hover:text-destructive"
                  onClick={() => setTasks((prev) => prev.filter((t) => t.id !== task.id))}
                >
                  <Trash2 className="w-3.5 h-3.5" />
                </Button>
              </div>
            ))}
          </div>
        </CardShell>
      )}
    </div>
  );
}