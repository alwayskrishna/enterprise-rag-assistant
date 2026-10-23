using UglyToad.PdfPig;

namespace EnterpriseRag;

public interface IDocumentReader
{
    Task<string> ReadAsync(string path, CancellationToken cancellationToken = default);
}

public sealed class DocumentReader : IDocumentReader
{
    public async Task<string> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Document not found.", path);
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension == ".txt")
            return await File.ReadAllTextAsync(path, cancellationToken);
        if (extension != ".pdf")
            throw new NotSupportedException("Only .txt and .pdf documents are supported.");
        return await Task.Run(() =>
        {
            using var pdf = PdfDocument.Open(path);
            var pages = pdf.GetPages().Select(page => page.Text);
            var text = string.Join(Environment.NewLine, pages);
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidDataException("PDF has no extractable text. Scanned PDFs require OCR, which is not included.");
            return text;
        }, cancellationToken);
    }
}
