namespace Finanzuebersicht.Services;

public class ShellNavigationService : INavigationService
{
    public async Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        if (!TryGetShell(out var shell))
            return;

        if (parameters is null)
            await shell.GoToAsync(route);
        else
            await shell.GoToAsync(route, parameters);
    }

    public async Task GoBackAsync()
    {
        if (!TryGetShell(out var shell))
            return;

        await shell.GoToAsync("..");
    }

    private static bool TryGetShell(out Shell shell)
    {
        if (Shell.Current is not null)
        {
            shell = Shell.Current;
            return true;
        }

#if DEBUG
        throw new InvalidOperationException(
            "Shell.Current is null — navigation was attempted before the Shell was ready.");
#else
        shell = null!;
        return false;
#endif
    }
}
