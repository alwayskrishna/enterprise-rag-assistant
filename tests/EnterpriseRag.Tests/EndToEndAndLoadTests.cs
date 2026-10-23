using System.Collections.Concurrent;
using EnterpriseRag;
using Xunit;

namespace EnterpriseRag.Tests;

public sealed class EndToEndAndLoadTests
{
    [Fact]
    public void IngestionSearchReplacementAndDeletionAreConsistent()
    {
        var store = new DocumentStore();
        Assert.True(store.Upsert("runbook", "The database recovery procedure requires a backup snapshot.") > 0);
        var answer = store.Ask("database recovery procedure", 3);
        Assert.Contains(answer.Sources, source => source.DocumentId == "runbook");

        store.Upsert("runbook", "The incident response requires an on-call engineer.");
        Assert.DoesNotContain(store.Ask("database recovery procedure", 3).Sources,
            source => source.DocumentId == "runbook");
        Assert.Contains(store.Ask("incident response", 3).Sources,
            source => source.DocumentId == "runbook");

        Assert.True(store.Delete("runbook"));
        Assert.Empty(store.Ask("incident response", 3).Sources);
    }

    [Fact]
    public async Task ConcurrentReadsAndWritesRemainSafe()
    {
        var store = new DocumentStore();
        var failures = new ConcurrentQueue<Exception>();
        await Task.WhenAll(Enumerable.Range(0, 80).Select(async i =>
        {
            try
            {
                await Task.Yield();
                store.Upsert($"doc-{i}", $"runbook recovery operation {i}");
                _ = store.Ask("runbook recovery", 5);
                _ = store.List();
            }
            catch (Exception ex)
            {
                failures.Enqueue(ex);
            }
        }));
        Assert.Empty(failures);
        Assert.Equal(80, store.List().Count);
    }
}
