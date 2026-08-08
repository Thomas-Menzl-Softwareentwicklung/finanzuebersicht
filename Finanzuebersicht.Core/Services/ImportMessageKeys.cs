namespace Finanzuebersicht.Core.Services;

/// <summary>
/// ResX key names for import preview status detail messages (resolved via ILocalizationService in UI).
/// </summary>
public static class ImportMessageKeys
{
    public const string MissingBookingDate = "Msg_ImportStatusMissingDate";
    public const string PossibleDuplicate = "Msg_ImportStatusPossibleDuplicate";
    public const string CategoryUnresolved = "Msg_ImportStatusCategoryUnresolved";
    public const string FileReadFailed = "Msg_ImportFileReadFailed";
    public const string NoParserMatched = "Msg_ImportNoParserMatched";
}
