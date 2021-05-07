using System;
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
            CardFormatCheck(cardNumber);

            //https://www.regular-expressions.info/creditcard.html
            if (Regex.Match(cardNumber, @"^2\d{15}$").Success)
            {
                return CardType.Mir;
            }

            if (Regex.Match(cardNumber, @"^4[0-9]{12}(?:[0-9]{3})?$").Success)
            {
                return CardType.Visa;
            }

            if (Regex.Match(cardNumber, @"^(?:5[1-5][0-9]{2}|222[1-9]|22[3-9][0-9]|2[3-6][0-9]{2}|27[01][0-9]|2720)[0-9]{12}$").Success)
            {
                return CardType.MasterCard;
            }

            if (Regex.Match(cardNumber, @"^3[47][0-9]{13}$").Success)
            {
                return CardType.AmericanExpress;
            }

            if (Regex.Match(cardNumber, @"^6(?:011|5[0-9]{2})[0-9]{12}$").Success)
            {
                return CardType.Discover;
            }

            if (Regex.Match(cardNumber, @"^(31|35)\d{14}$").Success)
            {
                return CardType.JCB;
            }

            if (Regex.Match(cardNumber, @"^(30|36|38)\d{12}$").Success)
            {
                return CardType.DinersClub;
            }

            if (Regex.Match(cardNumber, @"^(50|56|57|58|63|67)(\d{14}|\d{17})$").Success)
            {
                return CardType.Maestro;
            }

            if (Regex.Match(cardNumber, @"^(62)\d{14}$").Success)
            {
                return CardType.ChinaUnionPay;
            }

            throw new InvalidDataException("Unknown card")
            {
                Data = {{"number", cardNumber}}
            };
        }

        internal static void CardFormatCheck(string cardNumber)
        {
            if (cardNumber.Length < 7 || cardNumber.Length > 19)
                throw new ArgumentException("Card number must be 7-19 digits");

            foreach(var c in cardNumber)
            {
                if(!char.IsDigit(c))
                    throw new ArgumentException("The card number must contain only digits");
            }
        }
    }
}