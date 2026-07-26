namespace Finanzuebersicht.Core.Licensing;

/// <summary>Gated capabilities. Soft limits use <see cref="LimitedResource"/> instead.</summary>
public enum AppFeature
{
    CsvImport = 1,
    Cashflow = 2,
    CloudSync = 3
}
