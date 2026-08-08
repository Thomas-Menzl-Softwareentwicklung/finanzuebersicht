namespace Finanzuebersicht.Application.Results;

/// <summary>
/// Stable application-layer error codes. Presentation maps these to ResourceKeys — never show raw English exception text from Use Cases.
/// </summary>
public enum UseCaseErrorCode
{
    AccountNotFound,
    AccountArchived,
    TransferAccountsRequired,
    TransferAccountsMustDiffer,
    TransferAmountMustBePositive,
    TransferMustUseTransferFlow,
    LicenseLimitReached
}
