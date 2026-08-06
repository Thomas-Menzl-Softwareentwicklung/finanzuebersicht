using System.Globalization;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;

namespace Finanzuebersicht.Application.UseCases.Transactions;

public sealed record SaveQuickExpenseWidgetPresetsResult(
    bool Success,
    int? InvalidSlot = null,
    TransactionInputError? ValidationError = null);

public class SaveQuickExpenseWidgetPresetsUseCase(
    IQuickExpenseWidgetPresetStore presetStore,
    ILicenseService? licenseService = null)
{
    private readonly IQuickExpenseWidgetPresetStore _presetStore = presetStore;
    private readonly ILicenseService _licenseService =
        licenseService ?? UnrestrictedLicenseService.Instance;

    public async Task<SaveQuickExpenseWidgetPresetsResult> ExecuteAsync(
        IReadOnlyList<QuickExpenseWidgetPreset> presets,
        CancellationToken cancellationToken = default)
    {
        _licenseService.EnsureFeature(AppFeature.QuickExpenseCapture);
        ArgumentNullException.ThrowIfNull(presets);

        var normalized = QuickExpenseWidgetPresetDefaults.Normalize(presets);
        var toSave = new List<QuickExpenseWidgetPreset>(QuickExpenseWidgetPresetDefaults.SlotCount);

        foreach (var preset in normalized)
        {
            var title = preset.Title.Trim();
            var amountRaw = preset.AmountText.Trim();

            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(amountRaw))
            {
                toSave.Add(new QuickExpenseWidgetPreset(preset.Slot, string.Empty, string.Empty));
                continue;
            }

            if (string.IsNullOrEmpty(title))
            {
                return new SaveQuickExpenseWidgetPresetsResult(
                    false,
                    preset.Slot,
                    TransactionInputError.TitleRequired);
            }

            if (!FlexibleAmountParser.TryParse(amountRaw, out var amount))
            {
                return new SaveQuickExpenseWidgetPresetsResult(
                    false,
                    preset.Slot,
                    TransactionInputError.InvalidAmountFormat);
            }

            if (amount <= 0)
            {
                return new SaveQuickExpenseWidgetPresetsResult(
                    false,
                    preset.Slot,
                    TransactionInputError.AmountMustBePositive);
            }

            toSave.Add(new QuickExpenseWidgetPreset(
                preset.Slot,
                title,
                amount.ToString("0.##", CultureInfo.InvariantCulture)));
        }

        await _presetStore.SaveAsync(toSave, cancellationToken).ConfigureAwait(false);
        return new SaveQuickExpenseWidgetPresetsResult(true);
    }
}
