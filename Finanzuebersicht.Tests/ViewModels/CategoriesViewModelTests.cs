using System.Xml.Linq;
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
    public async Task GoToDetail_WhenKontenTabAndNoItem_NavigatesToAccountDetail()
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

        await navigationService.Received(1).GoToAsync(Routes.AccountDetail);
    }

    [Fact]
    public async Task GoToDetail_WhenKontenTabAndNoItem_AndLimitReached_ShowsAlert()
    {
        var licenseService = Substitute.For<Finanzuebersicht.Core.Licensing.ILicenseService>();
        licenseService.CheckCreateLimit(
                Finanzuebersicht.Core.Licensing.LimitedResource.Accounts,
                Arg.Any<int>())
            .Returns(new Finanzuebersicht.Core.Licensing.LimitCheckResult(false, 3, 3));

        var sut = CreateSut(
            Substitute.For<ICategoryRepository>(),
            Substitute.For<ITransactionRepository>(),
            Substitute.For<IRecurringTransactionRepository>(),
            Substitute.For<IAccountRepository>(),
            Substitute.For<ITransactionTemplateRepository>(),
            out var dialogService,
            out var navigationService,
            licenseService: licenseService);

        sut.SelectedSectionIndex = 1;

        await sut.GoToDetailCommand.ExecuteAsync(null);

        await dialogService.Received(1).ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await navigationService.DidNotReceive().GoToAsync(Routes.AccountDetail);
    }

    [Fact]
    public async Task GoToDetail_WhenKategorienTabAndNoItem_NavigatesToCategoryDetail()
    {
        var sut = CreateSut(
            Substitute.For<ICategoryRepository>(),
            Substitute.For<ITransactionRepository>(),
            Substitute.For<IRecurringTransactionRepository>(),
            Substitute.For<IAccountRepository>(),
            Substitute.For<ITransactionTemplateRepository>(),
            out _,
            out var navigationService);

        await sut.GoToDetailCommand.ExecuteAsync(null);

        await navigationService.Received(1).GoToAsync(Routes.CategoryDetail);
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
                parameters.ContainsKey(NavigationQueryKeys.CategoryId) &&
                Equals(parameters[NavigationQueryKeys.CategoryId], category.Id)));
    }

    [Fact]
    public async Task DeleteKonto_WhenLastAccountIsRemoved_NotifiesEmptyStateVisibility()
    {
        var account = new Account { Id = "acc-1", Name = "Giro" };
        var accountRepository = Substitute.For<IAccountRepository>();
        accountRepository.GetAccountsAsync().Returns(new List<Account> { account });

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.RemapAccountIdAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(0);

        var templateRepository = Substitute.For<ITransactionTemplateRepository>();
        templateRepository.GetTransactionTemplatesAsync().Returns(new List<TransactionTemplate>());

        var sut = CreateSut(
            Substitute.For<ICategoryRepository>(),
            transactionRepository,
            Substitute.For<IRecurringTransactionRepository>(),
            accountRepository,
            templateRepository,
            out var dialogService);

        var accountItem = new AccountListItem(account);
        sut.Konten = new ObservableCollection<AccountListItem> { accountItem };
        dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);

        var propertyChanges = new List<string?>();
        sut.PropertyChanged += (_, args) => propertyChanges.Add(args.PropertyName);

        await sut.DeleteKontoCommand.ExecuteAsync(accountItem);

        Assert.True(sut.IsKontenEmptyStateVisible);
        Assert.Contains(nameof(CategoriesViewModel.IsKontenEmptyStateVisible), propertyChanges);
    }

    [Fact]
    public void CategoriesPage_AccountRows_DoNotDisableSwipeViewForSystemAccounts()
    {
        var page = XDocument.Load(FindWorkspaceFile("Finanzuebersicht/Views/CategoriesPage.xaml"));
        XNamespace maui = "http://schemas.microsoft.com/dotnet/2021/maui";

        var accountSwipeView = Assert.Single(
            page.Descendants(maui + "SwipeView"),
            element => element
                .Descendants(maui + "SwipeItem")
                .Any(item => ((string?)item.Attribute("Command"))?.Contains("DeleteKontoCommand", StringComparison.Ordinal) == true));

        Assert.Null(accountSwipeView.Attribute("IsEnabled"));

        var deleteSwipeItem = Assert.Single(accountSwipeView.Descendants(maui + "SwipeItem"));
        Assert.Equal("{Binding CanDelete}", (string?)deleteSwipeItem.Attribute("IsVisible"));
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
        out INavigationService navigationService,
        ILocalizationService? localizationService = null,
        Finanzuebersicht.Core.Licensing.ILicenseService? licenseService = null)
    {
        dialogService = Substitute.For<IDialogService>();
        dialogService.ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);
        dialogService.ShowConfirmationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(false));

        if (localizationService is null)
        {
            localizationService = Substitute.For<ILocalizationService>();
            localizationService.GetString(Arg.Any<string>()).Returns(call => call.ArgNotNull<string>());
            localizationService.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgAtNotNull<string>(0));
        }

        navigationService = Substitute.For<INavigationService>();

        var categoriesCoordinator = new CategoriesListCoordinator(
            new LoadCategoriesUseCase(categoryRepository),
            new DeleteCategoryUseCase(categoryRepository, transactionRepository, recurringTransactionRepository),
            localizationService,
            navigationService,
            dialogService,
            Substitute.For<IFeedbackService>(),
            Substitute.For<IAppEvents>());

        var accountsCoordinator = new AccountsListCoordinator(
            new LoadAccountsUseCase(accountRepository),
            new GetAccountBalancesUseCase(accountRepository, transactionRepository),
            new ToggleAccountArchiveUseCase(accountRepository),
            new DeleteAccountUseCase(accountRepository, transactionRepository, templateRepository),
            localizationService,
            navigationService,
            dialogService,
            Substitute.For<IFeedbackService>(),
            Substitute.For<IAppEvents>(),
            licenseService: licenseService);

        return new CategoriesViewModel(
            categoriesCoordinator,
            accountsCoordinator,
            localizationService,
            dialogService);
    }

    private static string FindWorkspaceFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate {relativePath} from {AppContext.BaseDirectory}");
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
