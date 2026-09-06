using System.Text.Json;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Infrastructure.Services;

public class SyncMetadataStore : JsonDataStoreBase, ISyncMetadataStore
{
    private string MetadataFile => Path.Combine(DataDir, DataFileNames.SyncMetadata);

    public SyncMetadataStore(string dataDir, ILogger<SyncMetadataStore>? logger = null)
        : base(dataDir, logger)
    {
    }

    public async Task<SyncMetadata> GetAsync()
    {
        await StoreLock.WaitAsync();
        try
        {
            return await LoadSingleAsync<SyncMetadata>(MetadataFile) ?? new SyncMetadata();
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public async Task SaveAsync(SyncMetadata meta)
    {
        await StoreLock.WaitAsync();
        try
        {
            await SaveSingleAsync(MetadataFile, meta);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    private async Task<T?> LoadSingleAsync<T>(string path)
    {
        if (!File.Exists(path))
            return default;

        var json = await File.ReadAllTextAsync(path);
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            Logger?.LogError(ex, "Data file is corrupted and cannot be loaded: {Path}", path);
            throw new DataCorruptionException(path, ex);
        }
    }

    private static async Task SaveSingleAsync<T>(string path, T item)
    {
        var json = JsonSerializer.Serialize(item, JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }
}
