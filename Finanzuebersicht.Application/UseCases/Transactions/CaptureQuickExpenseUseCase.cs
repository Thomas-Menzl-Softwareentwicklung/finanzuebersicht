using System.Globalization;
using Finanzuebersicht.Constants;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Transactions;

public sealed record CaptureQuickExpenseResult(
    bool Success,
    Transaction? Transaction = null,
    TransactionInputError? ValidationError = null);

/// <summary>
/// Saves a quick expense as a real transaction: system Unkategorisiert + default account.
/// In-app Schnell is free; the iOS widget remains Pro-gated separately.
/// </summary>
public class CaptureQuickExpenseUseCase(
    ITransactionRepository transactionRepository,
    IAccountRepository accountRepository,
    IUncategorizedCategoryService uncategorizedCategoryService,
    ITransactionValidationService validationService,
    IClock clock)
{
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly IAccountRepository _accountRepository = accountRepository;
    private readonly IUncategorizedCategoryService _uncategorizedCategoryService = uncategorizedCategoryService;
    private readonly ITransactionValidationService _validationService = validationService;
    private readonly IClock _clock = clock;

    public async Task<CaptureQuickExpenseResult> ExecuteAsync(
        string amountText,
        string title,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default)
    {
        culture ??= CultureInfo.CurrentCulture;
        if (!_validationService.TryValidate(
                amountText,
                title,
                hasCategory: true,
                culture,
                out var amount,
                out var error))
        {
            return new CaptureQuickExpenseResult(false, ValidationError: error);
        }

        var categoryId = await _uncategorizedCategoryService.EnsureAsync(cancellationToken)
            .ConfigureAwait(false);
        var accountId = await ResolveDefaultAccountIdAsync(cancellationToken).ConfigureAwait(false);

        var transaction = new Transaction
        {
            Betrag = amount,
            Titel = title.Trim(),
            Datum = _clock.Today,
            KategorieId = categoryId,
            AccountId = accountId,
            Typ = TransactionType.Ausgabe,
            Verwendungszweck = string.Empty
        };

        cancellationToken.ThrowIfCancellationRequested();
        await _transactionRepository.SaveTransactionAsync(transaction).ConfigureAwait(false);
        return new CaptureQuickExpenseResult(true, transaction);
    }

    private async Task<string> ResolveDefaultAccountIdAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var accounts = await _accountRepository.GetAccountsAsync().ConfigureAwait(false);
        var defaultAccount = accounts.FirstOrDefault(a =>
                a.SystemKey == SystemAccountKeys.Default && !a.IsArchived)
            ?? accounts.FirstOrDefault(a => !a.IsArchived);

        return defaultAccount?.Id ?? string.Empty;
    }
}
