namespace Finanzuebersicht.Application.UseCases.Sync;

public sealed record CloudSyncStatusSnapshot(bool SyncEnabled, string? LastError, DateTime? LastSyncUtc);
