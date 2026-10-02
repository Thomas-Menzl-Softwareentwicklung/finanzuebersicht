using Finanzuebersicht.Core.Services;

namespace Finanzuebersicht.Application.UseCases;

/// <summary>
/// Ensures default account and seed categories on first launch.
/// </summary>
public sealed class InitializeAppDataUseCase(InitializationService initializationService)
{
    private readonly InitializationService _initializationService = initializationService;

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _initializationService.InitializeAsync();
    }
}
