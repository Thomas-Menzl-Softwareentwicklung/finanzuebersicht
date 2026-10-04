using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Presentation.Services;

public interface IImportSessionStore
{
    void SetTable(CsvTable table, string? accountId);
    void SetActiveSession(ImportPreviewResult preview, CsvImportProfile? profile = null);
    ImportPreviewResult? GetActiveSession();
    CsvTable? GetTable();
    CsvImportProfile? GetProfile();
    string? GetAccountId();
    bool CanRemap { get; }
    void Clear();
}
