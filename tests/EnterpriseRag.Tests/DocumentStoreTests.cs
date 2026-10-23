using EnterpriseRag;
using Xunit;

namespace EnterpriseRag.Tests;

public sealed class DocumentStoreTests
{
    [Fact]
    public void UpsertReplacesExistingDocument()
    {
        var store = new DocumentStore();
        store.Upsert("runbook", "worker queue latency");
        store.Upsert("runbook", "database backup restore");
        Assert.Equal(new[] { "runbook" }, store.List());
        Assert.Empty(store.Ask("worker queue latency").Sources);
        Assert.Single(store.Ask("database backup restore").Sources);
    }

    [Fact]
    public void DeleteRemovesDocument()
    {
        var store = new DocumentStore();
        store.Upsert("runbook", "service incident recovery");
        Assert.True(store.Delete("runbook"));
        Assert.False(store.Delete("runbook"));
        Assert.Empty(store.Ask("service incident").Sources);
    }

    [Fact]
    public void RejectsInvalidQuestions()
    {
        var store = new DocumentStore();
        Assert.Throws<ArgumentException>(() => store.Ask(" "));
        Assert.Throws<ArgumentOutOfRangeException>(() => store.Ask("valid", 0));
    }
}
