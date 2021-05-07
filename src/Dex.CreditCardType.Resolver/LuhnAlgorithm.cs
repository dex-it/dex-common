using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Dex.CreditCardType.Resolver
{
    public static class LuhnAlgorithm
    {
        private static readonly int[] Results = {0, 2, 4, 6, 8, 1, 3, 5, 7, 9};

        /// <summary>
        /// For a list of digits, compute the ending checkdigit 
        /// </summary>
        /// <param name="digits">The list of digits for which to compute the check digit</param>
        /// <returns>the check digit</returns>
        public static int ComputeCheckDigit(IList<int> digits)
        {
            var i = 0;
            var lengthMod = digits.Count % 2;
            return digits.Sum(d => i++ % 2 == lengthMod ? d : Results[d]) * 9 % 10;
        }

        /// <summary>
        /// Return a list of digits including the checkdigit
        /// </summary>
        /// <param name="digits">The original list of digits</param>
        /// <returns>the new list of digits including checkdigit</returns>
        public static IList<int> AppendCheckDigit(IList<int> digits)
        {
            var result = digits;
            result.Add(ComputeCheckDigit(digits));
            return result;
        }

        /// <summary>
        /// Returns true when a list of digits has a valid checkdigit
        /// </summary>
        /// <param name="digits">The list of digits to check</param>
        /// <returns>true/false depending on valid checkdigit</returns>
        public static bool HasValidCheckDigit(IList<int> digits)
        {
            return digits.Last() == ComputeCheckDigit(digits.Take(digits.Count - 1).ToList());
        }


        /// <summary>
        /// Internal conversion function to convert string into a list of ints
        /// </summary>
        /// <param name="digits">the original string</param>
        /// <returns>the list of ints</returns>
        private static IList<int> ToDigitList(string digits)
        {
            CheckCorrectStringPan(digits);
            return digits.Select(d => d - 48).ToList();
        }

        /// <summary>
        /// For a string of digits, compute the ending checkdigit 
        /// </summary>
        /// <param name="digits">The string of digits for which to compute the check digit</param>
        /// <returns>the check digit</returns>
        public static string ComputeCheckDigit(string digits)
        {
            return ComputeCheckDigit(ToDigitList(digits)).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Return a string of digits including the checkdigit
        /// </summary>
        /// <param name="digits">The original string of digits</param>
        /// <returns>the new string of digits including checkdigit</returns>
        public static string AppendCheckDigit(string digits)
        {
            return digits + ComputeCheckDigit(digits);
        }

        /// <summary>
        /// Returns true when a string of digits has a valid checkdigit
        /// </summary>
        /// <param name="digits">The string of digits to check</param>
        /// <returns>true/false depending on valid checkdigit</returns>
        public static bool HasValidCheckDigit(string digits)
        {
            return HasValidCheckDigit(ToDigitList(digits));
        }

        private static void CheckCorrectStringPan(string digits)
        {
            if (!Regex.IsMatch(digits, "^\\d{13,19}$"))
            {
                throw new ArgumentException("must be 13-19 digits only", nameof(digits))
                {
                    Data = {{"PAN", digits}}
                };
            }
        }

        /// <summary>
        /// Internal conversion function to convert int into a list of ints, one for each digit
        /// </summary>
        /// <param name="digits">the original int</param>
        /// <returns>the list of ints</returns>
        private static IList<int> ToDigitList(int digits)
        {
            return digits.ToString(CultureInfo.InvariantCulture).Select(d => d - 48).ToList();
        }

        /// <summary>
        /// For an integer, compute the ending checkdigit 
        /// </summary>
        /// <param name="digits">The integer for which to compute the check digit</param>
        /// <returns>the check digit</returns>
        public static int ComputeCheckDigit(int digits)
        {
            return ComputeCheckDigit(ToDigitList(digits));
        }

        /// <summary>
        /// Return an integer including the checkdigit
        /// </summary>
        /// <param name="digits">The original integer</param>
        /// <returns>the new integer including checkdigit</returns>
        public static int AppendCheckDigit(int digits)
        {
            return digits * 10 + ComputeCheckDigit(digits);
        }

        /// <summary>
        /// Returns true when an integer has a valid checkdigit
        /// </summary>
        /// <param name="digits">The integer to check</param>
        /// <returns>true/false depending on valid checkdigit</returns>
        public static bool HasValidCheckDigit(int digits)
        {
            return HasValidCheckDigit(ToDigitList(digits));
        }

        /// <summary>
        /// Internal conversion function to convert int into a list of ints, one for each digit
        /// </summary>
        /// <param name="digits">the original int</param>
        /// <returns>the list of ints</returns>
        private static IList<int> ToDigitList(long digits)
        {
            return digits.ToString(CultureInfo.InvariantCulture).Select(d => d - 48).ToList();
        }

        /// <summary>
        /// For an integer, compute the ending checkdigit 
        /// </summary>
        /// <param name="digits">The integer for which to compute the check digit</param>
        /// <returns>the check digit</returns>
        public static int ComputeCheckDigit(long digits)
        {
            return ComputeCheckDigit(ToDigitList(digits));
        }

        /// <summary>
        /// Return an integer including the checkdigit
        /// </summary>
        /// <param name="digits">The original integer</param>
        /// <returns>the new integer including checkdigit</returns>
        public static long AppendCheckDigit(long digits)
        {
            return digits * 10 + ComputeCheckDigit(digits);
        }

        /// <summary>
        /// Returns true when an integer has a valid checkdigit
        /// </summary>
        /// <param name="digits">The integer to check</param>
        /// <returns>true/false depending on valid checkdigit</returns>
        public static bool HasValidCheckDigit(long digits)
        {
            return HasValidCheckDigit(ToDigitList(digits));
        }
    }
}