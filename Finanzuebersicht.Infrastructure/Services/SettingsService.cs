using System.IO;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Infrastructure.Services;

/// <summary>
/// Verwaltet App-Einstellungen als Key-Value-Paare (JSON-Datei).
/// Lese- und Schreibzugriffe sind thread-safe über einen internen Lock.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsFile;
    private readonly ILogger<SettingsService>? _logger;

    private Dictionary<string, string> _settings = [];
    private readonly object _lock = new();

    public SettingsService(ILogger<SettingsService>? logger = null)
        : this(Path.Combine(AppPaths.GetDefaultDataDir(), "settings.json"), logger) { }

    internal SettingsService(string settingsFilePath, ILogger<SettingsService>? logger = null)
    {
        _settingsFile = settingsFilePath;
        _logger = logger;
        Load();
    }

    public string Get(string key, string defaultValue = "")
    {
        lock (_lock)
        {
            return _settings.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }

    public void Set(string key, string value)
    {
        lock (_lock)
        {
            _settings[key] = value;
            Save();
        }
    }

    public bool Contains(string key)
    {
        lock (_lock)
        {
            return _settings.ContainsKey(key);
        }
    }

    public void Remove(string key)
    {
        lock (_lock)
        {
            if (!_settings.Remove(key))
                return;
            Save();
        }
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_settingsFile))
            {
                var json = File.ReadAllText(_settingsFile);
                _settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Fehler beim Laden der Einstellungen");
            _settings = [];
        }
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsFile)!;
            Directory.CreateDirectory(dir);
            var json = System.Text.Json.JsonSerializer.Serialize(_settings,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFile, json);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Fehler beim Speichern der Einstellungen");
        }
    }

    // ========== Backup-Einstellungen Hilfs-Methoden ==========

    /// <summary>
    /// Gibt den Backup-Pfad zurück. Falls nicht konfiguriert, nutzt {DataPath}/backups.
    /// </summary>
    public string GetBackupPath()
    {
        var backupPath = Get(SettingsKeys.BackupPath);
        if (!string.IsNullOrEmpty(backupPath))
            return backupPath;

        var dataPath = Get(SettingsKeys.DataPath);
        if (string.IsNullOrEmpty(dataPath))
            dataPath = AppPaths.GetDefaultDataDir();

        return Path.Combine(dataPath, "backups");
    }

    /// <summary>
    /// Setzt den Backup-Pfad.
    /// </summary>
    public void SetBackupPath(string path)
    {
        Set(SettingsKeys.BackupPath, path);
    }

    /// <summary>
    /// Gibt den Zeitstempel des letzten Backups zurück (ISO 8601) oder null.
    /// </summary>
    public DateTime? GetLastBackupTime()
    {
        var timeStr = Get(SettingsKeys.LastBackupTime);
        if (string.IsNullOrEmpty(timeStr))
            return null;

        if (DateTime.TryParse(timeStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
            return dt;

        return null;
    }

    /// <summary>
    /// Setzt den Zeitstempel des letzten Backups.
    /// </summary>
    public void SetLastBackupTime(DateTime time)
    {
        Set(SettingsKeys.LastBackupTime, time.ToString("O"));
    }

    /// <summary>
    /// Gibt an, ob automatische Backups aktiviert sind.
    /// </summary>
    public bool IsAutoBackupEnabled()
    {
        var enabled = Get(SettingsKeys.AutoBackupEnabled, "false");
        return bool.TryParse(enabled, out var result) && result;
    }

    /// <summary>
    /// Setzt, ob automatische Backups aktiviert sind.
    /// </summary>
    public void SetAutoBackupEnabled(bool enabled)
    {
        Set(SettingsKeys.AutoBackupEnabled, enabled.ToString().ToLowerInvariant());
    }

    /// <summary>
    /// Gibt die Häufigkeit automatischer Backups zurück ("daily", "weekly", "monthly").
    /// </summary>
    public string GetBackupFrequency()
    {
        return Get(SettingsKeys.BackupFrequency, BackupFrequencyValues.Weekly);
    }

    /// <summary>
    /// Setzt die Häufigkeit automatischer Backups.
    /// </summary>
    public void SetBackupFrequency(string frequency)
    {
        if (!BackupFrequencyValues.All.Contains(frequency))
            throw new ArgumentException($"Ungültige Backup-Häufigkeit: {frequency}");

        Set(SettingsKeys.BackupFrequency, frequency);
    }

    /// <summary>
    /// Gibt die maximale Anzahl aufzubewahrende Backups zurück.
    /// </summary>
    public int GetMaxBackupsToKeep()
    {
        var maxStr = Get(SettingsKeys.MaxBackupsToKeep, "10");
        return int.TryParse(maxStr, out var max) ? max : 10;
    }

    /// <summary>
    /// Setzt die maximale Anzahl aufzubewahrende Backups.
    /// </summary>
    public void SetMaxBackupsToKeep(int max)
    {
        if (max < 1)
            throw new ArgumentException("MaxBackupsToKeep muss >= 1 sein");

        Set(SettingsKeys.MaxBackupsToKeep, max.ToString());
    }
}
