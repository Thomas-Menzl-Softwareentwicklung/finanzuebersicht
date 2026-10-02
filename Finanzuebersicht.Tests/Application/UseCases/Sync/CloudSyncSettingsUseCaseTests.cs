using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Core.Sync;
using NSubstitute;
using Xunit;

namespace Finanzuebersicht.Tests.Application.UseCases.Sync;

public class CloudSyncSettingsUseCaseTests
{
    [Fact]
    public async Task GetCloudSyncStatus_ReturnsMetadataSnapshot()
    {
        var metadata = new SyncMetadata
        {
            SyncEnabled = true,
            LastError = "paused",
            LastSyncUtc = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc)
        };
        var store = Substitute.For<ISyncMetadataStore>();
        store.GetAsync().Returns(metadata);

        var snapshot = await new GetCloudSyncStatusUseCase(store).ExecuteAsync();

        Assert.True(snapshot.SyncEnabled);
        Assert.Equal("paused", snapshot.LastError);
        Assert.Equal(metadata.LastSyncUtc, snapshot.LastSyncUtc);
    }

    [Fact]
    public async Task RecordCloudSyncError_PersistsLastError()
    {
        var metadata = new SyncMetadata();
        var store = Substitute.For<ISyncMetadataStore>();
        store.GetAsync().Returns(metadata);

        await new RecordCloudSyncErrorUseCase(store).ExecuteAsync("CloudKit unavailable");

        Assert.Equal("CloudKit unavailable", metadata.LastError);
        await store.Received(1).SaveAsync(metadata);
    }

    [Fact]
    public async Task StartCloudSync_StartsOrchestratorThenSyncs()
    {
        var orchestrator = Substitute.For<ICloudSyncOrchestrator>();

        await new StartCloudSyncUseCase(orchestrator).ExecuteAsync();

        Received.InOrder(() =>
        {
            orchestrator.StartIfEnabledAsync(Arg.Any<CancellationToken>());
            orchestrator.SyncNowAsync(Arg.Any<CancellationToken>());
        });
    }
}
