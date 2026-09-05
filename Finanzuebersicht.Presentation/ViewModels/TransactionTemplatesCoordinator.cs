using System.Collections.ObjectModel;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;

namespace Finanzuebersicht.ViewModels;

/// <summary>
/// Transaction template load/delete/create-from-template for the transactions list.
/// </summary>
public sealed class TransactionTemplatesCoordinator(
    INavigationService navigationService,
    IDialogService dialogService,
    ILocalizationService localizationService,
    LoadTransactionTemplatesUseCase? loadTransactionTemplatesUseCase = null,
    DeleteTransactionTemplateUseCase? deleteTransactionTemplateUseCase = null,
    UseTransactionTemplateUseCase? useTransactionTemplateUseCase = null)
{
    private readonly INavigationService _navigationService = navigationService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly ILocalizationService _loc = localizationService;
    private readonly LoadTransactionTemplatesUseCase? _loadTransactionTemplatesUseCase = loadTransactionTemplatesUseCase;
    private readonly DeleteTransactionTemplateUseCase? _deleteTransactionTemplateUseCase = deleteTransactionTemplateUseCase;
    private readonly UseTransactionTemplateUseCase? _useTransactionTemplateUseCase = useTransactionTemplateUseCase;

    public async Task<ObservableCollection<TransactionTemplate>> LoadAsync()
    {
        if (_loadTransactionTemplatesUseCase == null)
            return [];

        var list = await _loadTransactionTemplatesUseCase.ExecuteAsync();
        return new ObservableCollection<TransactionTemplate>(list);
    }

    public async Task<bool> DeleteAsync(TransactionTemplate template)
    {
        if (template == null || _deleteTransactionTemplateUseCase == null)
            return false;

        var confirm = await _dialogService.ShowConfirmationAsync(
            _loc.GetString(ResourceKeys.Dlg_VorlageLoeschen),
            _loc.GetString(ResourceKeys.Dlg_VorlageLoeschenFrage, template.Name),
            _loc.GetString(ResourceKeys.Btn_Ja),
            _loc.GetString(ResourceKeys.Btn_Nein));
        if (!confirm) return false;

        await _deleteTransactionTemplateUseCase.ExecuteAsync(template.Id);
        return true;
    }

    public async Task CreateFromTemplateAsync(TransactionTemplate template)
    {
        if (template == null) return;

        if (_useTransactionTemplateUseCase != null)
            await _useTransactionTemplateUseCase.ExecuteAsync(template);

        await _navigationService.GoToAsync(Routes.TransactionDetail, new Dictionary<string, object>
        {
            [NavigationQueryKeys.TransactionTemplate] = template
        });
    }
}
