namespace Backend.DTOs.Documents
{
    public class DocumentDto
    {
        public Guid Id { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;

        public DateTime UploadDate { get; set; }
    }
}
