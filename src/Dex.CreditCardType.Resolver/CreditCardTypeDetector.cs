using System.IO;
using System.Text.RegularExpressions;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable once ClassNeverInstantiated.Global

namespace Dex.CreditCardType.Resolver
{
    public class CreditCardTypeDetector
    {
        public static CardType FindType(string cardNumber)
        {
            LuhnAlgorithm.CheckCorrectStringPan(cardNumber);

            // https://ccardgen.com описание типов карт 
            //https://www.regular-expressions.info/creditcard.html
            //https://stackoverflow.com/questions/9315647/regex-credit-card-number-tests
            if (Regex.Match(cardNumber, @"^4[0-9]{12}(?:[0-9]{3})?$").Success)
            {
                return CardType.Visa;
            }

            if (Regex.Match(cardNumber, @"^(?:5[1-5][0-9]{2}|222[1-9]|22[3-9][0-9]|2[3-6][0-9]{2}|27[01][0-9]|2720)[0-9]{12}$").Success)
            {
                return CardType.MasterCard;
            }

            if (Regex.Match(cardNumber, @"^(?:220[0-4])\d{12}$").Success)
            {
                return CardType.Mir;
            }

            if (Regex.Match(cardNumber, @"^3[47][0-9]{13}$").Success)
            {
                return CardType.AmericanExpress;
            }

            if (Regex.Match(cardNumber, @"^65[4-9][0-9]{13}|64[4-9][0-9]{13}|6011[0-9]{12}|(622(?:12[6-9]|1[3-9][0-9]|[2-8][0-9][0-9]|9[01][0-9]|92[0-5])[0-9]{10})$").Success)
            {
                return CardType.Discover;
            }

            if (Regex.Match(cardNumber, @"^(?:2131|1800|35\d{3})\d{11}$").Success)
            {
                return CardType.JCB;
            }

            if (Regex.Match(cardNumber, @"^3(?:0[0-5]|[68][0-9])[0-9]{11}$").Success)
            {
                return CardType.DinersClub;
            }

            if (Regex.Match(cardNumber, @"^(50|56|57|58|60|61|62|63|64|65|66|67|68|69)[0-9]{10,17}$").Success)
            {
                return CardType.Maestro;
            }

            if (Regex.Match(cardNumber, @"^(62[0-9]{14,17})$").Success)
            {
                return CardType.UnionPay;
            }

            throw new InvalidDataException("Unknown card")
            {
                Data = {{"number", cardNumber}}
            };
        }
    }
}