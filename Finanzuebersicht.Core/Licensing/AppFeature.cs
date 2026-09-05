namespace Finanzuebersicht.Core.Licensing;

/// <summary>Gated capabilities. Soft limits use <see cref="LimitedResource"/> instead.</summary>
public enum AppFeature
{
    CsvImport = 1,
    Cashflow = 2,
    CloudSync = 3,
    /// <summary>iOS Quick-Expense Widget (Presets, Inbox, deep link). In-app Schnell is free.</summary>
    QuickExpenseCapture = 4
}
