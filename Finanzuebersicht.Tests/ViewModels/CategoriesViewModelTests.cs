using System.Collections.ObjectModel;
using Finanzuebersicht.Application.UseCases.Categories;
using Finanzuebersicht.Application.UseCases.Accounts;
using Finanzuebersicht.Models;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Tests.ViewModels;

public class CategoriesViewModelTests
{
    [Fact]
    public async Task LoadKategorien_SetsGesamtSaldoAktivWithoutArchivedAccounts()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.GetCategoriesAsync().Returns(Task.FromResult(new List<Category>()));

        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(Task.FromResult(new List<Account>
        {
            new() { Id = "acc-1", Name = "Giro" },
            new() { Id = "acc-2", Name = "Alt", IsArchived = true }
        }));

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction>
            {
                new() { Typ = TransactionType.Einnahme, Betrag = 800m, AccountId = "acc-1" },
                new() { Typ = TransactionType.Einnahme, Betrag = 400m, AccountId = "acc-2" }
            }));

        var sut = CreateSut(
            categoryRepository,
            transactionRepository,
            Substitute.For<IRecurringTransactionRepository>(),
            accountRepository,
            Substitute.For<ITransactionTemplateRepository>(),
            out _);

        await sut.LoadKategorienCommand.ExecuteAsync(null);

        Assert.Equal(800m, sut.GesamtSaldoAktiv);
        Assert.True(sut.ShowGesamtSaldoHeader);
    }

    [Fact]
    public async Task LoadKategorien_PopulatesKategorien()
    {
        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.GetCategoriesAsync().Returns(Task.FromResult(new List<Category>
        {
            new() { Id = "cat-1", Name = "Miete" },
            new() { Id = "cat-2", Name = "Lebensmittel" }
        }));

        var sut = CreateSut(
            categoryRepository,
            Substitute.For<ITransactionRepository>(),
            Substitute.For<IRecurringTransactionRepository>(),
            Substitute.For<IAccountRepository>(),
            Substitute.For<ITransactionTemplateRepository>(),
            out _);

        await sut.LoadKategorienCommand.ExecuteAsync(null);

        Assert.Collection(
            sut.Kategorien,
            item => Assert.Equal("cat-1", item.Id),
            item => Assert.Equal("cat-2", item.Id));
    }

    [Fact]
    public async Task DeleteKategorie_WhenConfirmed_CallsDeleteUseCase()
    {
        var categoryToDelete = new Category { Id = "cat-1", Name = "Miete" };
        var fallbackCategory = new Category { Id = "cat-2", Name = "Sonstiges" };
        var categoryRepository = Substitute.For<ICategoryRepository>();
        categoryRepository.GetCategoriesAsync().Returns(Task.FromResult(new List<Category> { categoryToDelete, fallbackCategory }));
        categoryRepository.DeleteCategoryAsync("cat-1").Returns(Task.CompletedTask);

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetTransactionsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(Task.FromResult(new List<Transaction>()));

        var recurringTransactionRepository = Substitute.For<IRecurringTransactionRepository>();
        recurringTransactionRepository.GetRecurringTransactionsAsync()
            .Returns(Task.FromResult(new List<RecurringTransaction>()));
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(Task.FromResult(new List<Account>
        {
            new() { Id = "acc-1", Name = "Girokonto", Type = AccountType.Girokonto, SystemKey = Finanzuebersicht.Constants.SystemAccountKeys.Default }
        }));
        var templateRepository = Substitute.For<ITransactionTemplateRepository>();

        var sut = CreateSut(
            categoryRepository,
            transactionRepository,
            recurringTransactionRepository,
            accountRepository,
            templateRepository,
            out var dialogService);

        sut.Kategorien = new ObservableCollection<Category> { categoryToDelete };
        dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));

        await sut.DeleteKategorieCommand.ExecuteAsync(categoryToDelete);

        await categoryRepository.Received(1).DeleteCategoryAsync("cat-1");
        Assert.Empty(sut.Kategorien);
    }

    [Fact]
    public async Task DeleteKategorie_WhenNotConfirmed_DoesNotDelete()
    {
        var categoryToDelete = new Category { Id = "cat-1", Name = "Miete" };
        var categoryRepository = Substitute.For<ICategoryRepository>();
        var sut = CreateSut(
            categoryRepository,
            Substitute.For<ITransactionRepository>(),
            Substitute.For<IRecurringTransactionRepository>(),
            Substitute.For<IAccountRepository>(),
            Substitute.For<ITransactionTemplateRepository>(),
            out var dialogService);

        sut.Kategorien = new ObservableCollection<Category> { categoryToDelete };
        dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        await sut.DeleteKategorieCommand.ExecuteAsync(categoryToDelete);

        await categoryRepository.DidNotReceive().DeleteCategoryAsync(Arg.Any<string>());
        Assert.Single(sut.Kategorien);
    }

    [Fact]
    public async Task GoToDetail_WhenKontenTabAndNoItem_OpensInlineCreateForm()
    {
        var sut = CreateSut(
            Substitute.For<ICategoryRepository>(),
            Substitute.For<ITransactionRepository>(),
            Substitute.For<IRecurringTransactionRepository>(),
            Substitute.For<IAccountRepository>(),
            Substitute.For<ITransactionTemplateRepository>(),
            out _,
            out var navigationService);

        sut.SelectedSectionIndex = 1;

        await sut.GoToDetailCommand.ExecuteAsync(null);

        Assert.True(sut.ShowAddKontoForm);
        await navigationService.DidNotReceive().GoToAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>());
        await navigationService.DidNotReceive().GoToAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task SaveNewKonto_WhenValid_SavesAccountAndClosesForm()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(Task.FromResult(new List<Account>()));
        accountRepository.SaveAccountAsync(Arg.Any<Account>()).Returns(call => Task.FromResult(call.ArgNotNull<Account>()));

        var sut = CreateSut(
            Substitute.For<ICategoryRepository>(),
            Substitute.For<ITransactionRepository>(),
            Substitute.For<IRecurringTransactionRepository>(),
            accountRepository,
            Substitute.For<ITransactionTemplateRepository>(),
            out _,
            out _);

        sut.SelectedSectionIndex = 1;
        sut.ShowAddKontoForm = true;
        sut.NeuerKontoName = "Sparkonto";
        sut.SelectedKontoTypeOption = new AccountTypeOption(AccountType.Tagesgeld, "Tagesgeld");
        sut.NeuerAnfangssaldoText = "1000";

        await sut.SaveNewKontoCommand.ExecuteAsync(null);

        await accountRepository.Received(1).SaveAccountAsync(NonNullArg.Is<Account>(a =>
            a.Name == "Sparkonto" &&
            a.Type == AccountType.Tagesgeld &&
            a.OpeningBalance == 1000m));
        Assert.False(sut.ShowAddKontoForm);
    }

    [Fact]
    public async Task GoToDetail_WhenKategorienTabAndNoItem_ShowsCreateSheet()
    {
        var categoryCreateSheet = Substitute.For<ICategoryCreateSheetService>();
        categoryCreateSheet.ShowAsync(Arg.Any<CategoryDetailViewModel>()).Returns(Task.FromResult(false));

        var sut = CreateSut(
            Substitute.For<ICategoryRepository>(),
            Substitute.For<ITransactionRepository>(),
            Substitute.For<IRecurringTransactionRepository>(),
            Substitute.For<IAccountRepository>(),
            Substitute.For<ITransactionTemplateRepository>(),
            categoryCreateSheet,
            out _,
            out var navigationService);

        await sut.GoToDetailCommand.ExecuteAsync(null);

        await categoryCreateSheet.Received(1).ShowAsync(Arg.Any<CategoryDetailViewModel>());
        await navigationService.DidNotReceive().GoToAsync(Arg.Any<string>());
        await navigationService.DidNotReceive().GoToAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public async Task GoToDetail_NavigatesToCategoryDetailRoute()
    {
        var category = new Category { Id = "cat-1", Name = "Miete" };
        var sut = CreateSut(
            Substitute.For<ICategoryRepository>(),
            Substitute.For<ITransactionRepository>(),
            Substitute.For<IRecurringTransactionRepository>(),
            Substitute.For<IAccountRepository>(),
            Substitute.For<ITransactionTemplateRepository>(),
            out _,
            out var navigationService);

        await sut.GoToDetailCommand.ExecuteAsync(category);

        await navigationService.Received(1).GoToAsync(
            Routes.CategoryDetail,
            Arg.Is<IDictionary<string, object>?>(parameters =>
                parameters != null &&
                parameters.ContainsKey("Category") &&
                object.ReferenceEquals(parameters["Category"], category)));
    }

    [Fact]
    public async Task ToggleKontoArchivierung_WhenConfirmed_UpdatesAccount()
    {
        var accountRepository = Substitute.For<IAccountRepository>();
        var account = new Account { Id = "acc-1", Name = "Giro", IsArchived = false };
        accountRepository.GetAccountsAsync().Returns(new List<Account> { account });

        var sut = CreateSut(
            Substitute.For<ICategoryRepository>(),
            Substitute.For<ITransactionRepository>(),
            Substitute.For<IRecurringTransactionRepository>(),
            accountRepository,
            Substitute.For<ITransactionTemplateRepository>(),
            out var dialogService);

        dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(true));

        await sut.ToggleKontoArchivierungCommand.ExecuteAsync(new AccountListItem(account));

        await accountRepository.Received(1).SaveAccountAsync(NonNullArg.Is<Account>(a => a.Id == "acc-1" && a.IsArchived));
    }

    private static CategoriesViewModel CreateSut(
        ICategoryRepository categoryRepository,
        ITransactionRepository transactionRepository,
        IRecurringTransactionRepository recurringTransactionRepository,
        IAccountRepository accountRepository,
        ITransactionTemplateRepository templateRepository,
        out IDialogService dialogService,
        out INavigationService navigationService)
    {
        return CreateSut(
            categoryRepository,
            transactionRepository,
            recurringTransactionRepository,
            accountRepository,
            templateRepository,
            Substitute.For<ICategoryCreateSheetService>(),
            out dialogService,
            out navigationService);
    }

    private static CategoriesViewModel CreateSut(
        ICategoryRepository categoryRepository,
        ITransactionRepository transactionRepository,
        IRecurringTransactionRepository recurringTransactionRepository,
        IAccountRepository accountRepository,
        ITransactionTemplateRepository templateRepository,
        ICategoryCreateSheetService categoryCreateSheetService,
        out IDialogService dialogService,
        out INavigationService navigationService)
    {
        dialogService = Substitute.For<IDialogService>();
        dialogService.ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);
        dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetString(Arg.Any<string>()).Returns(call => call.ArgNotNull<string>());
        localizationService.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgAtNotNull<string>(0));

        navigationService = Substitute.For<INavigationService>();

        var createCategoryViewModel = new CategoryDetailViewModel(
            new SaveCategoryDetailUseCase(categoryRepository),
            navigationService,
            localizationService,
            Substitute.For<IFeedbackService>(),
            Substitute.For<IAppEvents>(),
            dialogService);

        return new CategoriesViewModel(
            new DeleteCategoryUseCase(categoryRepository, transactionRepository, recurringTransactionRepository),
            new LoadCategoriesUseCase(categoryRepository),
            new LoadAccountsUseCase(accountRepository),
            new GetAccountBalancesUseCase(accountRepository, transactionRepository),
            new SaveAccountDetailUseCase(accountRepository),
            new ToggleAccountArchiveUseCase(accountRepository),
            new DeleteAccountUseCase(accountRepository, transactionRepository, templateRepository),
            createCategoryViewModel,
            categoryCreateSheetService,
            localizationService,
            navigationService,
            dialogService,
            Substitute.For<IFeedbackService>(),
            Substitute.For<IAppEvents>());
    }

    private static CategoriesViewModel CreateSut(
        ICategoryRepository categoryRepository,
        ITransactionRepository transactionRepository,
        IRecurringTransactionRepository recurringTransactionRepository,
        IAccountRepository accountRepository,
        ITransactionTemplateRepository templateRepository,
        out IDialogService dialogService)
    {
        return CreateSut(
            categoryRepository,
            transactionRepository,
            recurringTransactionRepository,
            accountRepository,
            templateRepository,
            out dialogService,
            out _);
    }
}
