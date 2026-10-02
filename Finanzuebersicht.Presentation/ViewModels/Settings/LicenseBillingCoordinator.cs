using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;

namespace Finanzuebersicht.ViewModels;

public sealed record LicenseProductPriceLabels(string ProPriceLabel, string SyncPriceLabel);

/// <summary>
/// StoreKit purchase, restore, and product price loading for the license settings screen.
/// </summary>
public sealed class LicenseBillingCoordinator(
    IStoreBillingService billingService,
    ILicenseEntitlementStore entitlementStore,
    ILicenseService licenseService,
    IDialogService dialogService,
    IFeedbackService feedbackService,
    ILocalizationService localizationService)
{
    private readonly IStoreBillingService _billingService = billingService;
    private readonly ILicenseEntitlementStore _entitlementStore = entitlementStore;
    private readonly ILicenseService _licenseService = licenseService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly IFeedbackService _feedbackService = feedbackService;
    private readonly ILocalizationService _loc = localizationService;

    public async Task<LicenseProductPriceLabels> LoadProductPricesAsync()
    {
        if (!_billingService.IsAvailable)
            return new LicenseProductPriceLabels(string.Empty, string.Empty);

        try
        {
            if (!await _billingService.InitializeAsync())
                return new LicenseProductPriceLabels(string.Empty, string.Empty);

            var products = await _billingService.GetProductsAsync();
            var proPrice = products.FirstOrDefault(p => p.Id == LicenseProductIds.Pro)?.LocalizedPrice ?? string.Empty;
            var syncPrice = products.FirstOrDefault(p => p.Id == LicenseProductIds.SyncYearly)?.LocalizedPrice ?? string.Empty;

            if (string.IsNullOrWhiteSpace(proPrice)) proPrice = string.Empty;
            if (string.IsNullOrWhiteSpace(syncPrice)) syncPrice = string.Empty;

            return new LicenseProductPriceLabels(proPrice, syncPrice);
        }
        catch
        {
            return new LicenseProductPriceLabels(string.Empty, string.Empty);
        }
    }

    public async Task PurchaseProductAsync(
        string productId,
        string successKey,
        Func<bool> getIsBusy,
        Action<bool> setIsBusy,
        Func<Task> onEntitlementsRefreshed)
    {
        if (getIsBusy() || !_billingService.IsAvailable)
            return;

        setIsBusy(true);
        try
        {
            var result = await _billingService.PurchaseAsync(productId);
            if (result.WasCancelled)
                return;

            if (!result.IsSuccess)
            {
                await _dialogService.ShowAlertAsync(
                    _loc.GetString(ResourceKeys.Err_Titel),
                    result.ErrorMessage ?? _loc.GetString(ResourceKeys.Lic_PurchaseFailed),
                    _loc.GetString(ResourceKeys.Btn_OK));
                return;
            }

            await ApplyOwnedEntitlementsAsync();
            await onEntitlementsRefreshed();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(successKey));
        }
        finally
        {
            setIsBusy(false);
        }
    }

    public async Task RestorePurchasesAsync(
        Func<bool> getIsBusy,
        Action<bool> setIsBusy,
        Func<Task> onEntitlementsRefreshed)
    {
        if (getIsBusy() || !_billingService.IsAvailable)
            return;

        setIsBusy(true);
        try
        {
            await _entitlementStore.ClearStubPreferenceAsync();
            var ok = await _billingService.RestorePurchasesAsync();
            var owned = await _billingService.GetOwnedProductIdsAsync();
            if (owned.Count > 0)
                await _entitlementStore.ApplyOwnedProductIdsAsync(owned);
            await _licenseService.RefreshAsync();
            await onEntitlementsRefreshed();
            await _feedbackService.ShowSnackbarAsync(ok || owned.Count > 0
                ? _loc.GetString(ResourceKeys.Lic_RestoreSuccess)
                : _loc.GetString(ResourceKeys.Lic_RestoreEmpty));
        }
        finally
        {
            setIsBusy(false);
        }
    }

    public async Task ApplyOwnedEntitlementsAsync()
    {
        var owned = await _billingService.GetOwnedProductIdsAsync();
        await _entitlementStore.ApplyOwnedProductIdsAsync(owned);
        await _licenseService.RefreshAsync();
    }
}
