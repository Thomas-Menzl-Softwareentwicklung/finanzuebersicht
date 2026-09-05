using Finanzuebersicht.Application.Results;
using Finanzuebersicht.Application.UseCases.Backup;
using Finanzuebersicht.Core.Services;

namespace Finanzuebersicht.Tests.Application.UseCases.Backup;

public class BackupUseCaseTests
{
    [Fact]
    public async Task CreateBackup_ReturnsMetadataOnSuccess()
    {
        var backup = Substitute.For<IBackupService>();
        backup.CreateBackupAsync(Arg.Any<string?>()).Returns(new BackupMetadata { Id = "b1" });

        var result = await new CreateBackupUseCase(backup).ExecuteAsync("/tmp");

        Assert.True(result.IsSuccess);
        Assert.Equal("b1", result.Value!.Id);
    }

    [Fact]
    public async Task RestoreBackup_MapsNotFoundToErrorCode()
    {
        var backup = Substitute.For<IBackupService>();
        backup.RestoreBackupAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new RestoreResult { Success = false, ErrorKind = BackupErrorKind.NotFound });

        var result = await new RestoreBackupUseCase(backup).ExecuteAsync("/tmp", "missing");

        Assert.False(result.IsSuccess);
        Assert.Equal(UseCaseErrorCode.BackupNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task RestoreBackup_MapsInconsistentFlag()
    {
        var backup = Substitute.For<IBackupService>();
        backup.RestoreBackupAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new RestoreResult
            {
                Success = false,
                DataMayBeInconsistent = true,
                ErrorKind = BackupErrorKind.RestoreAndRollbackFailed
            });

        var result = await new RestoreBackupUseCase(backup).ExecuteAsync("/tmp", "id");

        Assert.False(result.IsSuccess);
        Assert.Equal(UseCaseErrorCode.BackupDataInconsistent, result.Error!.Code);
    }

    [Fact]
    public async Task ListBackups_ReturnsEmptyList()
    {
        var backup = Substitute.For<IBackupService>();
        backup.ListBackupsAsync(Arg.Any<string>()).Returns(Array.Empty<BackupMetadata>());

        var result = await new ListBackupsUseCase(backup).ExecuteAsync("/tmp");

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task DeleteBackup_ReturnsOk()
    {
        var backup = Substitute.For<IBackupService>();
        backup.DeleteBackupAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(Task.CompletedTask);

        var result = await new DeleteBackupUseCase(backup).ExecuteAsync("/tmp", "id");

        Assert.True(result.IsSuccess);
        await backup.Received(1).DeleteBackupAsync("/tmp", "id");
    }

    [Fact]
    public async Task ExportCsv_ReturnsStream()
    {
        var backup = Substitute.For<IBackupService>();
        backup.ExportAsCSVAsync().Returns(new MemoryStream([1, 2, 3]));

        var result = await new ExportCsvUseCase(backup).ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Length);
    }
}
