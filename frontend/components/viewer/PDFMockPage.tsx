export function PDFMockPage({ pageNum }: { pageNum: number }) {
  return (
    <div className="bg-white rounded shadow-sm p-6 mx-auto max-w-md">
      <div className="space-y-2">
        <div className="h-3 bg-gray-200 rounded w-3/4" />
        <div className="h-3 bg-gray-200 rounded w-full" />
        <div className="h-3 bg-gray-200 rounded w-5/6" />
        <div className="h-3 bg-gray-200 rounded w-full" />
        <div className="h-3 bg-gray-200 rounded w-2/3" />
      </div>
      <div className="mt-4 space-y-1.5">
        {[80, 100, 90, 100, 75, 85, 100, 70].map((w, i) => (
          <div key={i} className="h-2 bg-gray-100 rounded" style={{ width: `${w}%` }} />
        ))}
      </div>
      <div className="mt-4 p-3 bg-blue-50 rounded-lg border border-blue-100">
        <div className="h-2 bg-blue-200 rounded w-3/4 mb-1.5" />
        <div className="h-2 bg-blue-200 rounded w-full mb-1.5" />
        <div className="h-2 bg-blue-200 rounded w-2/3" />
      </div>
      <div className="mt-4 space-y-1.5">
        {[100, 85, 95, 60].map((w, i) => (
          <div key={i} className="h-2 bg-gray-100 rounded" style={{ width: `${w}%` }} />
        ))}
      </div>
      <div className="mt-6 text-center text-xs text-gray-300 font-medium">{pageNum}</div>
    </div>
  );
}
