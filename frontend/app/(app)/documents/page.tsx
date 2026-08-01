"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Upload, Search, FileText, Eye, Trash2 } from "lucide-react";
import { Button } from "@/components/ui2/Button";
import { Input } from "@/components/ui2/Input";
import { CardShell } from "@/components/ui2/CardShell";
import { FileTypeBadge } from "@/components/ui2/FileTypeBadge";
import { Pagination } from "@/components/documents/pagination";
import { cn, formatBytes } from "@/lib/utils";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import { fetchDocuments, deleteDocument } from "@/store/slices/documentSlice";

const PAGE_SIZE = 10;

export default function DocumentsPage() {
  const router = useRouter();
  const dispatch = useAppDispatch();
  const { items, totalCount, pageNumber, totalPages, loading, deletingIds } = useAppSelector(
    (state) => state.documents
  );

  const [search, setSearch] = useState("");
  const [selectedType, setSelectedType] = useState<"ALL" | "PDF" | "DOCX" | "TXT">("ALL");

  useEffect(() => {
    dispatch(fetchDocuments({ pageNumber: 1, pageSize: PAGE_SIZE }));
  }, [dispatch]);

  // Client-side filtering applies only to the currently loaded page.
  // Fine for now — once document counts grow, move search/type filtering
  // into query params sent to /document and refetch server-side instead.
  const filtered = items.filter((d) => {
    const matchSearch = d.name.toLowerCase().includes(search.toLowerCase());
    const matchType = selectedType === "ALL" || d.extension === selectedType;
    return matchSearch && matchType;
  });

  const handleDelete = async (id: string, name: string) => {
    const result = await dispatch(deleteDocument(id));
    if (deleteDocument.fulfilled.match(result)) {
      toast.success(`${name} deleted`);
      // If we just deleted the last item on a page beyond page 1, step back a page
      if (items.length === 1 && pageNumber > 1) {
        dispatch(fetchDocuments({ pageNumber: pageNumber - 1, pageSize: PAGE_SIZE }));
      }
    } else {
      toast.error(result.payload || "Failed to delete document");
    }
  };

  const goToPage = (page: number) => {
    dispatch(fetchDocuments({ pageNumber: page, pageSize: PAGE_SIZE }));
  };

  return (
    <div className="p-6 max-w-6xl mx-auto space-y-5">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold text-foreground">My Documents</h1>
          <p className="text-sm text-muted-foreground mt-0.5">{totalCount} documents total</p>
        </div>
        <Button variant="primary" size="md" icon={<Upload className="w-3.5 h-3.5" />} onClick={() => router.push("/upload")}>
          Upload
        </Button>
      </div>

      <div className="flex items-center gap-3">
        <div className="flex-1 max-w-xs">
          <Input
            placeholder="Search documents..."
            icon={<Search className="w-4 h-4" />}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
        <div className="flex items-center gap-1 bg-muted rounded-lg p-1">
          {(["ALL", "PDF", "DOCX", "TXT"] as const).map((type) => (
            <button
              key={type}
              onClick={() => setSelectedType(type)}
              className={cn(
                "px-3 h-7 text-xs font-medium rounded-md transition-all",
                selectedType === type ? "bg-card text-foreground shadow-sm" : "text-muted-foreground hover:text-foreground"
              )}
            >
              {type}
            </button>
          ))}
        </div>
      </div>

      <CardShell className="overflow-hidden">
        <table className="w-full">
          <thead>
            <tr className="border-b border-border bg-muted/50">
              <th className="text-left text-xs font-medium text-muted-foreground px-5 py-3 uppercase tracking-wide">Name</th>
              <th className="text-left text-xs font-medium text-muted-foreground px-4 py-3 uppercase tracking-wide">Type</th>
              <th className="text-left text-xs font-medium text-muted-foreground px-4 py-3 uppercase tracking-wide">Upload Date</th>
              <th className="text-left text-xs font-medium text-muted-foreground px-4 py-3 uppercase tracking-wide">Size</th>
              <th className="text-right text-xs font-medium text-muted-foreground px-5 py-3 uppercase tracking-wide">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {loading ? (
              <tr>
                <td colSpan={5} className="text-center py-16 text-sm text-muted-foreground">
                  Loading documents...
                </td>
              </tr>
            ) : filtered.length === 0 ? (
              <tr>
                <td colSpan={5} className="text-center py-16">
                  <FileText className="w-10 h-10 text-muted-foreground/30 mx-auto mb-3" />
                  <p className="text-sm text-muted-foreground">No documents found</p>
                </td>
              </tr>
            ) : (
              filtered.map((doc) => (
                <tr key={doc.id} className="hover:bg-muted/30 transition-colors group">
                  <td className="px-5 py-3.5">
                    <div className="flex items-center gap-3">
                      <div className="w-8 h-8 rounded-lg bg-muted flex items-center justify-center flex-shrink-0">
                        <FileText className="w-4 h-4 text-muted-foreground" />
                      </div>
                      <p className="text-sm font-medium text-foreground truncate max-w-xs">{doc.name}</p>
                    </div>
                  </td>
                  <td className="px-4 py-3.5">
                    <FileTypeBadge type={doc.extension as "PDF" | "DOCX" | "TXT"} />
                  </td>
                  <td className="px-4 py-3.5 text-sm text-muted-foreground">
                    {new Date(doc.uploadDate).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3.5 text-sm text-muted-foreground">{formatBytes(doc.sizeBytes)}</td>
                  <td className="px-5 py-3.5">
                    <div className="flex items-center justify-end gap-1">
                      <Button
                        variant="ghost"
                        size="sm"
                        icon={<Eye className="w-3.5 h-3.5" />}
                        className="h-8"
                        onClick={() => router.push(`/documents/${doc.id}`)}
                      >
                        Open
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-muted-foreground hover:text-destructive"
                        disabled={deletingIds.includes(doc.id)}
                        onClick={() => handleDelete(doc.id, doc.name)}
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>

        {!loading && filtered.length > 0 && (
          <div className="flex items-center justify-between px-5 py-3 border-t border-border">
            <span className="text-xs text-muted-foreground">
              Showing {items.length} of {totalCount} documents · Page {pageNumber} of {totalPages}
            </span>
            <Pagination currentPage={pageNumber} totalPages={totalPages} onPageChange={goToPage} />
          </div>
        )}
      </CardShell>
    </div>
  );
}