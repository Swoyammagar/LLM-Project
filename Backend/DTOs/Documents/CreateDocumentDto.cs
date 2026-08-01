namespace Backend.DTOs.Documents
{
    public class CreateDocumentDto
    {
        public IFormFile File { get; set; } = null!;
    }
}
