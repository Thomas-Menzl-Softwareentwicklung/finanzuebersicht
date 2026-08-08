namespace Finanzuebersicht.Models;

public class Category
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "💰";
    public string Color { get; set; } = "#007AFF";
    public TransactionType Typ { get; set; } = TransactionType.Ausgabe;

    /// <summary>
    /// Optionaler Schlüssel für vom System angelegte Kategorien (z.B. Finanzuebersicht.Constants.SystemCategoryKeys.Lebensmittel).
    /// Wird zur Laufzeit in die übersetzte Bezeichnung aufgelöst.
    /// Null bei nutzerdefinierten Kategorien – diese verwenden immer Name direkt.
    /// </summary>
    public string? SystemKey { get; set; }

    /// <summary>Optional id in an external system (CloudKit / bank). Null for local-only rows.</summary>
    public string? ExternalId { get; set; }

    /// <summary>Optional origin marker (see <c>EntitySources</c>). Null means local/unspecified.</summary>
    public string? Source { get; set; }

    /// <summary>Last content change for future sync. Null on legacy data.</summary>
    public DateTime? UpdatedAt { get; set; }
}
