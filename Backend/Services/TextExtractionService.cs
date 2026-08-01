using Backend.Services.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text;


namespace Backend.Services
{
    public class TextExtractionService : ITextExtractionService
    {
        private readonly ILogger<TextExtractionService> _logger;

        public TextExtractionService(ILogger<TextExtractionService> logger)
        {
            _logger = logger;
        }
        public async Task<string> ExtractTextAsync(string filePath, string fileExtension)
        {
            try 
            { 
                var normalizedExtension = fileExtension.ToLowerInvariant();

                _logger.LogInformation("Starting text extraction for file: {FileName} (type: {Extension})", 
                    Path.GetFileName(filePath), normalizedExtension);

                var extractedText = normalizedExtension switch
                {
                    ".pdf" => await ExtractTextFromPdfAsync(filePath),
                    ".docx" => await ExtractTextFromDocxAsync(filePath),
                    ".txt" => await ExtractTextFromTxtAsync(filePath),
                    _ => throw new NotSupportedException($"File type '{normalizedExtension}' is not supported")
                };

                _logger.LogInformation("Successfully extracted {CharCount} characters from {FileName}", 
                    extractedText.Length, Path.GetFileName(filePath));

                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from file {FileName}. Upload will continue with empty ExtractedText.", 
                    Path.GetFileName(filePath));
                return string.Empty;
            }
        }
        private async Task<string> ExtractTextFromPdfAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var text = new StringBuilder();

                    using (var pdfReader = new PdfReader(filePath))
                    using (var pdfDocument = new PdfDocument(pdfReader))
                    {
                        _logger.LogInformation("PDF has {PageCount} pages", pdfDocument.GetNumberOfPages());

                        for (int pageNumber = 1; pageNumber <= pdfDocument.GetNumberOfPages(); pageNumber++)
                        {
                            var pageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(pageNumber));
                            text.AppendLine(pageText);
                        }
                    }

                    return text.ToString().Trim();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting text from PDF at {FilePath}", filePath);
                    throw;
                }
            });
        }
        private async Task<string> ExtractTextFromDocxAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var text = new StringBuilder();

                    using (var wordDocument = WordprocessingDocument.Open(filePath, false))
                    {
                        var body = wordDocument.MainDocumentPart?.Document?.Body;

                        if (body == null)
                        {
                            _logger.LogWarning("Document body is null for DOCX at {FilePath}", filePath);
                            return string.Empty;
                        }

                        foreach (var paragraph in body.Descendants<Paragraph>())
                        {                           
                            var paragraphText = new StringBuilder();
                            foreach (var run in paragraph.Descendants<Run>())
                            {
                                foreach (var textElement in run.Descendants<Text>())
                                {
                                    paragraphText.Append(textElement.Text);
                                }
                            }
                           
                            if (paragraphText.Length > 0)
                            {
                                text.AppendLine(paragraphText.ToString());
                            }
                        }
                    }

                    var result = text.ToString().Trim();
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting text from DOCX at {FilePath}", filePath);
                    throw;
                }
            });
        }
        private async Task<string> ExtractTextFromTxtAsync(string filePath)
        {
            try
            {
                var text = await File.ReadAllTextAsync(filePath);
                return text.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading text from TXT file at {FilePath}", filePath);
                throw;
            }
        }
    }
}
