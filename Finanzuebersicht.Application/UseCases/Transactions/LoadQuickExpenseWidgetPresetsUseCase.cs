using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;

namespace Finanzuebersicht.Application.UseCases.Transactions;

public class LoadQuickExpenseWidgetPresetsUseCase(
    IQuickExpenseWidgetPresetStore presetStore,
    ILicenseService? licenseService = null)
{
    private readonly IQuickExpenseWidgetPresetStore _presetStore = presetStore;
    private readonly ILicenseService _licenseService =
        licenseService ?? UnrestrictedLicenseService.Instance;

    public async Task<IReadOnlyList<QuickExpenseWidgetPreset>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        _licenseService.EnsureFeature(AppFeature.QuickExpenseCapture);
        return await _presetStore.LoadAsync(cancellationToken).ConfigureAwait(false);
    }
}
