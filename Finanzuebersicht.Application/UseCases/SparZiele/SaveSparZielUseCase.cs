using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.SparZiele;

public class SaveSparZielUseCase(
    ISparZielRepository sparZielRepository,
    ILicenseService? licenseService = null)
{
    private readonly ILicenseService _licenseService = licenseService ?? UnrestrictedLicenseService.Instance;

    public async Task ExecuteAsync(SparZiel sparZiel, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existing = await sparZielRepository.GetSparZieleAsync() ?? [];
        var isNew = existing.All(s => s.Id != sparZiel.Id);
        if (isNew)
            _licenseService.EnsureCanCreate(LimitedResource.SparZiele, existing.Count);

        await sparZielRepository.SaveSparZielAsync(sparZiel);
    }
}
