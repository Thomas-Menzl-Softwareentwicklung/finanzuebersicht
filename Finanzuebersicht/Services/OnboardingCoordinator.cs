using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Core.Services.ScreenshotDemo;
using Finanzuebersicht.Presentation.Services;

namespace Finanzuebersicht.Services;

public class OnboardingCoordinator(
    ISettingsService settingsService,
    HasAnyTransactionsUseCase hasAnyTransactionsUseCase) : IOnboardingCoordinator
{
    public async Task<bool> ShouldShowOnboardingAsync(CancellationToken cancellationToken = default)
    {
        if (ScreenshotDemoLaunchOptions.IsActive())
            return false;

        if (settingsService.Get(SettingsKeys.OnboardingCompleted, "false") == "true")
            return false;

        return !await hasAnyTransactionsUseCase.ExecuteAsync(cancellationToken);
    }

    public void MarkCompleted()
        => settingsService.Set(SettingsKeys.OnboardingCompleted, "true");
}
