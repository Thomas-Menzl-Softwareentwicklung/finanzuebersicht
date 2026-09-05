namespace Finanzuebersicht.Models;

public class Transaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public decimal Betrag { get; set; }
    public string Titel { get; set; } = string.Empty;
    public DateTime Datum { get; set; } = DateTime.Today;
    public string KategorieId { get; set; } = string.Empty;
    public TransactionType Typ { get; set; } = TransactionType.Ausgabe;
    public string? DauerauftragId { get; set; }

    // Optional: which account this transaction belongs to (supports multi-account scenarios)
    public string? AccountId { get; set; }
    public bool IsTransfer { get; set; }
    public string? TransferGroupId { get; set; }

    // Detaillierter Verwendungszweck / Beschreibung aus dem Kontoauszug
    public string Verwendungszweck { get; set; } = string.Empty;

    public string? SparZielId { get; set; }

    /// <summary>Optional id in an external system (CloudKit / bank). Null for local-only rows.</summary>
    public string? ExternalId { get; set; }

    /// <summary>Optional origin marker (see <c>EntitySources</c>). Null means local/unspecified.</summary>
    public string? Source { get; set; }

    /// <summary>Last content change for future sync. Null on legacy data.</summary>
    public DateTime? UpdatedAt { get; set; }
}
