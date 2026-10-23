using EnterpriseRag;
using Xunit;

namespace EnterpriseRag.Tests;

public class GroundedAnswerServiceTests
{
    private sealed class FakeGenerator : IGroundedAnswerGenerator
    {
        public int Calls { get; private set; }
        public Task<string> GenerateAsync(string question, IReadOnlyList<SearchHit> sources, CancellationToken cancellationToken = default)
        {
            Calls++;
            Assert.NotEmpty(sources);
            return Task.FromResult("Inspect queue depth [1].");
        }
    }

    [Fact]
    public async Task MissingEvidenceDoesNotCallModel()
    {
        var fake = new FakeGenerator();
        var service = new GroundedAnswerService(fake);
        var answer = await service.AnswerAsync(new[] { new Chunk("runbook", 0, "worker queue depth") }, "pineapple");
        Assert.Empty(answer.Sources);
        Assert.Equal(0, fake.Calls);
        Assert.Contains("couldn't find", answer.Answer);
    }

    [Fact]
    public async Task RelevantEvidenceIsPassedToGenerator()
    {
        var fake = new FakeGenerator();
        var service = new GroundedAnswerService(fake);
        var answer = await service.AnswerAsync(new[] { new Chunk("runbook", 0, "worker queue depth") }, "queue depth");
        Assert.Equal(1, fake.Calls);
        Assert.Equal("Inspect queue depth [1].", answer.Answer);
        Assert.Single(answer.Sources);
        Assert.Equal("runbook", answer.Sources[0].DocumentId);
    }
}
