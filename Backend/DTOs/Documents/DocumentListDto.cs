namespace Backend.DTOs.Documents
{
    public class DocumentListDto
    {
        public List<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

    }
}
