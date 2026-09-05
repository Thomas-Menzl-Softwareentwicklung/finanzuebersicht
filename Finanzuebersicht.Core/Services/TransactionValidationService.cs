using System.Globalization;

namespace Finanzuebersicht.Core.Services;

public class TransactionValidationService : ITransactionValidationService
{
    public bool TryValidate(
        string amountText,
        string title,
        bool hasCategory,
        CultureInfo culture,
        out decimal amount,
        out TransactionInputError? error)
    {
        error = null;

        // Accept both comma and dot — OS numeric keyboards follow device locale, not app language.
        if (!FlexibleAmountParser.TryParse(amountText, out amount))
        {
            error = TransactionInputError.InvalidAmountFormat;
            return false;
        }

        if (amount <= 0)
        {
            error = TransactionInputError.AmountMustBePositive;
            return false;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            error = TransactionInputError.TitleRequired;
            return false;
        }

        if (!hasCategory)
        {
            error = TransactionInputError.CategoryRequired;
            return false;
        }

        return true;
    }
}
