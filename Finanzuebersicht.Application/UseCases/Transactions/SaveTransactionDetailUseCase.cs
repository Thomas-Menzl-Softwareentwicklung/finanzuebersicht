using Finanzuebersicht.Application.Results;
using Finanzuebersicht.Constants;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Transactions;

public class SaveTransactionDetailUseCase(
    ITransactionRepository transactionRepository,
    IAccountRepository accountRepository)
{
    private readonly ITransactionRepository _transactionRepository = transactionRepository;
    private readonly IAccountRepository _accountRepository = accountRepository;

    public async Task<UseCaseResult> ExecuteAsync(
        Transaction? existingTransaction,
        decimal betrag,
        string titel,
        DateTime datum,
        string kategorieId,
        string? accountId,
        TransactionType typ,
        string verwendungszweck,
        string? sparZielId = null,
        CancellationToken cancellationToken = default)
    {
        if (existingTransaction?.IsTransfer == true)
            return UseCaseResult.Fail(UseCaseErrorCode.TransferMustUseTransferFlow);

        if (!string.IsNullOrWhiteSpace(accountId))
        {
            var accounts = await _accountRepository.GetAccountsAsync();
            var account = accounts.FirstOrDefault(a => a.Id == accountId);
            if (account is null)
                return UseCaseResult.Fail(UseCaseErrorCode.AccountNotFound);
            if (account.IsArchived && (existingTransaction == null || existingTransaction.AccountId != accountId))
                return UseCaseResult.Fail(UseCaseErrorCode.AccountArchived);
        }

        var transaction = existingTransaction ?? new Transaction();
        transaction.Betrag = betrag;
        transaction.Titel = titel;
        transaction.Datum = datum;
        transaction.KategorieId = kategorieId;
        transaction.AccountId = await ResolveAccountIdAsync(accountId, transaction.AccountId, cancellationToken)
            .ConfigureAwait(false);
        transaction.Typ = typ;
        transaction.Verwendungszweck = verwendungszweck ?? string.Empty;
        transaction.SparZielId = string.IsNullOrWhiteSpace(sparZielId) ? null : sparZielId;

        await _transactionRepository.SaveTransactionAsync(transaction);
        return UseCaseResult.Ok();
    }

    private async Task<string> ResolveAccountIdAsync(
        string? requestedAccountId,
        string? existingAccountId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestedAccountId))
            return requestedAccountId;

        if (!string.IsNullOrWhiteSpace(existingAccountId))
            return existingAccountId;

        cancellationToken.ThrowIfCancellationRequested();
        var accounts = await _accountRepository.GetAccountsAsync().ConfigureAwait(false);
        var defaultAccount = accounts.FirstOrDefault(a => a.SystemKey == SystemAccountKeys.Default && !a.IsArchived)
            ?? accounts.FirstOrDefault(a => !a.IsArchived);

        return defaultAccount?.Id ?? string.Empty;
    }
}
