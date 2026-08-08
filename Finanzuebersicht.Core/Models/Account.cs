namespace Finanzuebersicht.Models;

public class Account
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; } = AccountType.Girokonto;
    public string? SystemKey { get; set; }
    public bool IsArchived { get; set; }
    public decimal OpeningBalance { get; set; }
    public DateTime? OpeningBalanceDate { get; set; }

    /// <summary>Optional id in an external system (CloudKit / bank). Null for local-only rows.</summary>
    public string? ExternalId { get; set; }

    /// <summary>Optional origin marker (see <c>EntitySources</c>). Null means local/unspecified.</summary>
    public string? Source { get; set; }

    /// <summary>Last content change for future sync. Null on legacy data.</summary>
    public DateTime? UpdatedAt { get; set; }

    public bool IsSystemAccount => !string.IsNullOrWhiteSpace(SystemKey);
    public bool CanDelete => !IsSystemAccount;
    public bool CanArchive => !IsSystemAccount;
}
