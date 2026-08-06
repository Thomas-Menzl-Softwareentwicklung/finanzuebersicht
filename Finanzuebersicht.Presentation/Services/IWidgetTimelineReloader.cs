namespace Finanzuebersicht.Presentation.Services;

/// <summary>Asks WidgetKit to rebuild timelines after App Group preset changes.</summary>
public interface IWidgetTimelineReloader
{
    void ReloadAll();
}

/// <summary>No-op for tests and platforms without WidgetKit.</summary>
public sealed class NullWidgetTimelineReloader : IWidgetTimelineReloader
{
    public static NullWidgetTimelineReloader Instance { get; } = new();

    public void ReloadAll() { }
}
