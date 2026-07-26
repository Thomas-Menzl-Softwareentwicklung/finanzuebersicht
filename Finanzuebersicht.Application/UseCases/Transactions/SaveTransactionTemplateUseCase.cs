using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Transactions;

public class SaveTransactionTemplateUseCase(
    ITransactionTemplateRepository repository,
    ILicenseService? licenseService = null)
{
    private readonly ITransactionTemplateRepository _repository = repository;
    private readonly ILicenseService _licenseService = licenseService ?? UnrestrictedLicenseService.Instance;

    public async Task ExecuteAsync(TransactionTemplate template, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existing = await _repository.GetTransactionTemplatesAsync() ?? [];
        var isNew = existing.All(t => t.Id != template.Id);
        if (isNew)
            _licenseService.EnsureCanCreate(LimitedResource.Templates, existing.Count);

        await _repository.SaveTransactionTemplateAsync(template);
    }
}
