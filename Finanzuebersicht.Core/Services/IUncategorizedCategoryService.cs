using Finanzuebersicht.Models;

namespace Finanzuebersicht.Core.Services;

/// <summary>
/// Resolves or creates the system "Unkategorisiert" category (CSV import + quick capture).
/// </summary>
public interface IUncategorizedCategoryService
{
    bool IsUncategorized(Category category);

    /// <summary>Returns the category id, creating the system category when missing.</summary>
    Task<string> EnsureAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the category id if it already exists; otherwise null.</summary>
    Task<string?> FindIdAsync(CancellationToken cancellationToken = default);
}
