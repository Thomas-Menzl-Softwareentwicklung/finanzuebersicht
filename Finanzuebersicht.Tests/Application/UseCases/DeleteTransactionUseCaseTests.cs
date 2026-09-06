using Finanzuebersicht.Application.UseCases.Sync;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Core.Sync;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Tests.Application.UseCases;

public class DeleteTransactionUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_DelegatesToRepository()
    {
        var transactionRepository = Substitute.For<ITransactionRepository>();
        var sut = new DeleteTransactionUseCase(transactionRepository);

        await sut.ExecuteAsync("tx-1");

        await transactionRepository.Received(1).DeleteTransactionAsync("tx-1");
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccessfulDelete_NotifiesOrchestrator()
    {
        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.DeleteTransactionAsync("tx-1").Returns(Task.CompletedTask);

        var orchestrator = Substitute.For<ICloudSyncOrchestrator>();
        var sut = new DeleteTransactionUseCase(transactionRepository, orchestrator);

        await sut.ExecuteAsync("tx-1");

        await orchestrator.Received(1).NotifyLocalDeleteAsync(
            SyncEntityType.Transaction,
            "tx-1",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteTransferGroupAsync_NotifiesBothTransactionDeletes()
    {
        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetAllTransactionsAsync(Arg.Any<CancellationToken>())
            .Returns([
                new Transaction { Id = "tx-out", TransferGroupId = "grp-1" },
                new Transaction { Id = "tx-in", TransferGroupId = "grp-1" }
            ]);
        transactionRepository.DeleteTransferGroupAsync("grp-1").Returns(Task.CompletedTask);

        var orchestrator = Substitute.For<ICloudSyncOrchestrator>();
        var sut = new DeleteTransactionUseCase(transactionRepository, orchestrator);

        await sut.ExecuteTransferGroupAsync("grp-1");

        await orchestrator.Received(1).NotifyLocalDeleteAsync(
            SyncEntityType.Transaction, "tx-out", Arg.Any<CancellationToken>());
        await orchestrator.Received(1).NotifyLocalDeleteAsync(
            SyncEntityType.Transaction, "tx-in", Arg.Any<CancellationToken>());
    }
}