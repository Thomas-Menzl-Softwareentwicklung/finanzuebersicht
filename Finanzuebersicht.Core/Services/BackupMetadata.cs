using System;
using System.Collections.Generic;

namespace Finanzuebersicht.Core.Services
{
    /// <summary>
    /// Metadaten eines Backups für Versionierung und Validierung.
    /// </summary>
    public class BackupMetadata
    {
        /// <summary>
        /// Eindeutige ID des Backups (typischerweise ISO-Timestamp: 2026-03-11T21-46-19).
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Zeitstempel der Backup-Erstellung (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Schema-Version für Kompatibilität und Migration.
        /// Aktuell: 1
        /// </summary>
        public int SchemaVersion { get; set; } = 1;

        /// <summary>
        /// Anzahl der Entitäten pro Typ als Integrität-Check.
        /// Beispiel: { "categories": 12, "transactions": 342, "recurring": 5 }
        /// </summary>
        public Dictionary<string, int> EntityCounts { get; set; } = new();

        /// <summary>
        /// Optionale Beschreibung des Backups (z.B. "End of Q1 2026").
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Dateiname des Backup-ZIP (z.B. "backup_2026-03-11T21-46-19.zip").
        /// </summary>
        public string FileName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Structured restore failure kind (UI maps via Application UseCaseErrorCode — no DE strings).
    /// </summary>
    public enum BackupErrorKind
    {
        None = 0,
        NotFound,
        CorruptOrIncomplete,
        SchemaIncompatible,
        SaveFailed,
        RestoreFailed,
        RestoreAndRollbackFailed
    }

    /// <summary>
    /// Ergebnis einer Restore-Operation.
    /// </summary>
    public class RestoreResult
    {
        /// <summary>
        /// true wenn Restore erfolgreich.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Structured failure kind when <see cref="Success"/> is false.
        /// </summary>
        public BackupErrorKind ErrorKind { get; set; }

        /// <summary>
        /// Optional technical detail for logs (not for UI). Prefer <see cref="ErrorKind"/>.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// true wenn der Rollback nach einem fehlgeschlagenen Restore ebenfalls fehlgeschlagen ist.
        /// In diesem Fall kann der Datenzustand inkonsistent sein.
        /// </summary>
        public bool DataMayBeInconsistent { get; set; }

        /// <summary>
        /// Optionale Detailinformation (z.B. Anzahl wiederhergestellter Transaktionen).
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Die wiederhergestellten Metadaten.
        /// </summary>
        public BackupMetadata? RestoredMetadata { get; set; }
    }
}
