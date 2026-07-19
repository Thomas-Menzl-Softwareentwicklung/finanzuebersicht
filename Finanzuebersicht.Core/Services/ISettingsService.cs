namespace Finanzuebersicht.Core.Services;

/// <summary>
/// Abstraction over settings persistence. Decouples ViewModels and services
/// from the concrete file-based implementation, enabling testability.
/// </summary>
public interface ISettingsService
{
    /// <summary>Gets a setting value. Returns <paramref name="defaultValue"/> if not found.</summary>
    string Get(string key, string defaultValue = "");

    /// <summary>Sets a setting value and persists asynchronously in the background.</summary>
    void Set(string key, string value);

    /// <summary>Returns whether the key exists in the settings store.</summary>
    bool Contains(string key);

    /// <summary>Removes a setting key if present and persists the change.</summary>
    void Remove(string key);

    string GetBackupPath();
    void SetBackupPath(string path);

    DateTime? GetLastBackupTime();
    void SetLastBackupTime(DateTime time);

    bool IsAutoBackupEnabled();
    void SetAutoBackupEnabled(bool enabled);

    string GetBackupFrequency();
    void SetBackupFrequency(string frequency);

    int GetMaxBackupsToKeep();
    void SetMaxBackupsToKeep(int max);
}
