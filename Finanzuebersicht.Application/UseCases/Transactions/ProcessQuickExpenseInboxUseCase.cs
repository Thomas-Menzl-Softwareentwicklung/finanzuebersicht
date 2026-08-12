using System.Globalization;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Application.UseCases.Transactions;

public class ProcessQuickExpenseInboxUseCase(
    IQuickExpenseInboxStore inboxStore,
    CaptureQuickExpenseUseCase captureQuickExpenseUseCase,
    ILicenseService? licenseService = null,
    ILogger<ProcessQuickExpenseInboxUseCase>? logger = null)
{
    private readonly IQuickExpenseInboxStore _inboxStore = inboxStore;
    private readonly CaptureQuickExpenseUseCase _captureQuickExpenseUseCase = captureQuickExpenseUseCase;
    private readonly ILicenseService _licenseService =
        licenseService ?? UnrestrictedLicenseService.Instance;
    private readonly ILogger<ProcessQuickExpenseInboxUseCase>? _logger = logger;

    /// <summary>Drains the widget inbox and saves each item. Returns how many were saved.</summary>
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Leave the App Group file untouched until Pro is confirmed.
        if (!_licenseService.HasFeature(AppFeature.QuickExpenseCapture))
        {
            _logger?.LogDebug("Quick expense inbox skipped; Pro not available");
            return 0;
        }

        var pending = await _inboxStore.DrainPendingAsync(cancellationToken).ConfigureAwait(false);
        if (pending.Count == 0)
            return 0;

        var saved = 0;
        for (var i = 0; i < pending.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = pending[i];
            try
            {
                var amountText = FlexibleAmountParser.ToInvariantAmountText(item.AmountText)
                    ?? item.AmountText;

                var result = await _captureQuickExpenseUseCase.ExecuteAsync(
                    amountText,
                    item.Title,
                    CultureInfo.InvariantCulture,
                    cancellationToken).ConfigureAwait(false);

                if (result.Success)
                    saved++;
                else
                    _logger?.LogWarning(
                        "Quick expense inbox item {Id} failed validation: {Error}",
                        item.Id,
                        result.ValidationError);
            }
            catch (FeatureGateException ex)
            {
                var remaining = pending.Skip(i).ToList();
                _logger?.LogWarning(
                    ex,
                    "Quick expense inbox requires Pro; restoring {Count} unprocessed items",
                    remaining.Count);
                await _inboxStore.WritePendingAsync(remaining, cancellationToken).ConfigureAwait(false);
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to process quick expense inbox item {Id}", item.Id);
            }
        }

        return saved;
    }
}
