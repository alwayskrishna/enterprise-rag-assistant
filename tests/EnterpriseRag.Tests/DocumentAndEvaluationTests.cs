using EnterpriseRag;
using Xunit;

namespace EnterpriseRag.Tests;

public class DocumentAndEvaluationTests
{
    [Fact]
    public async Task ReadsTextDocuments()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        try
        {
            await File.WriteAllTextAsync(path, "Incident recovery runbook");
            Assert.Equal("Incident recovery runbook", await new DocumentReader().ReadAsync(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task RejectsUnsupportedFileFormats()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".docx");
        try
        {
            await File.WriteAllTextAsync(path, "sample");
            await Assert.ThrowsAsync<NotSupportedException>(() => new DocumentReader().ReadAsync(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ReportsAccuracyAtOne()
    {
        var chunks = new[] { new Chunk("queue.txt", 0, "queue worker latency"),
            new Chunk("security.txt", 0, "security access encryption") };
        var cases = new[] { new RetrievalCase("queue", "worker latency", "queue.txt"),
            new RetrievalCase("miss", "worker latency", "security.txt") };
        var report = RetrievalEvaluation.Run(chunks, cases);
        Assert.Equal(2, report.Total);
        Assert.Equal(1, report.Passed);
        Assert.Equal(0.5, report.AccuracyAtOne);
    }
}
