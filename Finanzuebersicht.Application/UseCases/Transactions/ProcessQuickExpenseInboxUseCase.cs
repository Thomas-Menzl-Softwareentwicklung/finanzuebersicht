using System.Globalization;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Application.UseCases.Transactions;

public class ProcessQuickExpenseInboxUseCase(
    IQuickExpenseInboxStore inboxStore,
    CaptureQuickExpenseUseCase captureQuickExpenseUseCase,
    ILogger<ProcessQuickExpenseInboxUseCase>? logger = null)
{
    private readonly IQuickExpenseInboxStore _inboxStore = inboxStore;
    private readonly CaptureQuickExpenseUseCase _captureQuickExpenseUseCase = captureQuickExpenseUseCase;
    private readonly ILogger<ProcessQuickExpenseInboxUseCase>? _logger = logger;

    /// <summary>Drains the widget inbox and saves each item. Returns how many were saved.</summary>
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _inboxStore.DrainPendingAsync(cancellationToken).ConfigureAwait(false);
        if (pending.Count == 0)
            return 0;

        var saved = 0;
        foreach (var item in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var result = await _captureQuickExpenseUseCase.ExecuteAsync(
                    item.AmountText,
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
                _logger?.LogWarning(ex, "Quick expense inbox requires Pro; dropping {Count} remaining items", pending.Count - saved);
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
