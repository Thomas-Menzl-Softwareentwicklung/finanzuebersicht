namespace Finanzuebersicht.Models;

public class SparZiel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Titel { get; set; } = string.Empty;
    public string Icon { get; set; } = "🎯";
    public decimal ZielBetrag { get; set; }
    public decimal AktuellerBetrag { get; set; }
    public DateTime? Faelligkeitsdatum { get; set; }
    public decimal? MonatlicheSparrate { get; set; }

    /// <summary>Optional id in an external system (CloudKit / bank). Null for local-only rows.</summary>
    public string? ExternalId { get; set; }

    /// <summary>Optional origin marker (see <c>EntitySources</c>). Null means local/unspecified.</summary>
    public string? Source { get; set; }

    /// <summary>Last content change for future sync. Null on legacy data.</summary>
    public DateTime? UpdatedAt { get; set; }
}
