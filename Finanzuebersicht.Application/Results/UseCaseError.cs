namespace Finanzuebersicht.Application.Results;

/// <summary>
/// Expected failure from a use case. FormatArgs feed localized resource strings (e.g. license limit current/max).
/// </summary>
public sealed class UseCaseError
{
    public UseCaseError(UseCaseErrorCode code, params object[] formatArgs)
    {
        Code = code;
        FormatArgs = formatArgs ?? [];
    }

    public UseCaseErrorCode Code { get; }
    public IReadOnlyList<object> FormatArgs { get; }
}
