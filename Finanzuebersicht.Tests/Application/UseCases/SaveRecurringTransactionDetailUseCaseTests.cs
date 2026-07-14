using Finanzuebersicht.Application.UseCases.RecurringTransactions;
using Finanzuebersicht.Models;
using NSubstitute;

namespace Finanzuebersicht.Tests.Application.UseCases;

public class SaveRecurringTransactionDetailUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesNewRecurring_WhenExistingIsNull()
    {
        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        var recurringGenerationService = Substitute.For<IRecurringGenerationService>();
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(new List<Account> { new() { Id = "acc-1", IsArchived = false } });
        var sut = new SaveRecurringTransactionDetailUseCase(recurringRepository, recurringGenerationService, accountRepository);

        await sut.ExecuteAsync(
            existing: null,
            betrag: 49.99m,
            titel: "Streaming",
            kategorieId: "cat-1",
            accountId: "acc-1",
            typ: TransactionType.Ausgabe,
            startdatum: new DateTime(2026, 3, 1),
            enddatum: new DateTime(2026, 12, 31),
            aktiv: true);

        await recurringRepository.Received(1).SaveRecurringTransactionAsync(
            Arg.Is<RecurringTransaction>(r =>
                r != null &&
                r.Betrag == 49.99m &&
                r.Titel == "Streaming" &&
                r.KategorieId == "cat-1" &&
                r.AccountId == "acc-1" &&
                r.Typ == TransactionType.Ausgabe &&
                r.Startdatum == new DateTime(2026, 3, 1) &&
                r.Enddatum == new DateTime(2026, 12, 31) &&
                r.Aktiv));
        await recurringGenerationService.Received(1).GeneratePendingRecurringTransactionsAsync();
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesExistingRecurring_WhenProvided()
    {
        var recurringRepository = Substitute.For<IRecurringTransactionRepository>();
        var recurringGenerationService = Substitute.For<IRecurringGenerationService>();
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(new List<Account> { new() { Id = "acc-2", IsArchived = false } });
        var existing = new RecurringTransaction
        {
            Id = "r-1",
            Titel = "Alt",
            Betrag = 10m,
            KategorieId = "cat-old",
            Typ = TransactionType.Ausgabe,
            Startdatum = new DateTime(2026, 1, 1),
            Enddatum = null,
            Aktiv = false
        };
        var sut = new SaveRecurringTransactionDetailUseCase(recurringRepository, recurringGenerationService, accountRepository);

        await sut.ExecuteAsync(
            existing,
            betrag: 120m,
            titel: "Neu",
            kategorieId: "cat-2",
            accountId: "acc-2",
            typ: TransactionType.Einnahme,
            startdatum: new DateTime(2026, 4, 1),
            enddatum: null,
            aktiv: true);

        await recurringRepository.Received(1).SaveRecurringTransactionAsync(existing);
        Assert.Equal("r-1", existing.Id);
        Assert.Equal(120m, existing.Betrag);
        Assert.Equal("Neu", existing.Titel);
        Assert.Equal("cat-2", existing.KategorieId);
        Assert.Equal("acc-2", existing.AccountId);
        Assert.Equal(TransactionType.Einnahme, existing.Typ);
        Assert.Equal(new DateTime(2026, 4, 1), existing.Startdatum);
        Assert.Null(existing.Enddatum);
        Assert.True(existing.Aktiv);
        await recurringGenerationService.Received(1).GeneratePendingRecurringTransactionsAsync();
    }
}