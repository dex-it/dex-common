using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable IdentifierTypo

namespace Dex.Inflector
{
    public static class Inflector
    {
        private static readonly List<Rule> Plurals = new List<Rule>();
        private static readonly List<Rule> Singulars = new List<Rule>();
        private static readonly List<string> UnCountables = new List<string>();

        #region Default Rules

        static Inflector()
        {
            AddPlural("$", "s");
            AddPlural("s$", "s");
            AddPlural("(ax|test)is$", "$1es");
            AddPlural("(octop|vir|alumn|fung)us$", "$1i");
            AddPlural("(alias|status)$", "$1es");
            AddPlural("(bu)s$", "$1ses");
            AddPlural("(buffal|tomat|volcan)o$", "$1oes");
            AddPlural("([ti])um$", "$1a");
            AddPlural("sis$", "ses");
            AddPlural("(?:([^f])fe|([lr])f)$", "$1$2ves");
            AddPlural("(hive)$", "$1s");
            AddPlural("([^aeiouy]|qu)y$", "$1ies");
            AddPlural("(x|ch|ss|sh)$", "$1es");
            AddPlural("(matr|vert|ind)ix|ex$", "$1ices");
            AddPlural("([m|l])ouse$", "$1ice");
            AddPlural("^(ox)$", "$1en");
            AddPlural("(quiz)$", "$1zes");

            AddSingular("s$", "");
            AddSingular("(n)ews$", "$1ews");
            AddSingular("([ti])a$", "$1um");
            AddSingular("((a)naly|(b)a|(d)iagno|(p)arenthe|(p)rogno|(s)ynop|(t)he)ses$", "$1$2sis");
            AddSingular("(^analy)ses$", "$1sis");
            AddSingular("([^f])ves$", "$1fe");
            AddSingular("(hive)s$", "$1");
            AddSingular("(tive)s$", "$1");
            AddSingular("([lr])ves$", "$1f");
            AddSingular("([^aeiouy]|qu)ies$", "$1y");
            AddSingular("(s)eries$", "$1eries");
            AddSingular("(m)ovies$", "$1ovie");
            AddSingular("(x|ch|ss|sh)es$", "$1");
            AddSingular("([m|l])ice$", "$1ouse");
            AddSingular("(bus)es$", "$1");
            AddSingular("(o)es$", "$1");
            AddSingular("(shoe)s$", "$1");
            AddSingular("(cris|ax|test)es$", "$1is");
            AddSingular("(octop|vir|alumn|fung)i$", "$1us");
            AddSingular("(alias|status)es$", "$1");
            AddSingular("^(ox)en", "$1");
            AddSingular("(vert|ind)ices$", "$1ex");
            AddSingular("(matr)ices$", "$1ix");
            AddSingular("(quiz)zes$", "$1");

            AddIrregular("person", "people");
            AddIrregular("man", "men");
            AddIrregular("child", "children");
            AddIrregular("sex", "sexes");
            AddIrregular("move", "moves");
            AddIrregular("goose", "geese");
            AddIrregular("alumna", "alumnae");

            AddUncountable("equipment");
            AddUncountable("information");
            AddUncountable("rice");
            AddUncountable("money");
            AddUncountable("species");
            AddUncountable("series");
            AddUncountable("fish");
            AddUncountable("sheep");
            AddUncountable("deer");
            AddUncountable("aircraft");
        }

        #endregion

        public static void AddIrregular(string singular, string plural)
        {
            if (singular == null) throw new ArgumentNullException(nameof(singular));
            if (plural == null) throw new ArgumentNullException(nameof(plural));

            AddPlural("(" + singular[0] + ")" + singular.Substring(1) + "$", "$1" + plural.Substring(1));
            AddSingular("(" + plural[0] + ")" + plural.Substring(1) + "$", "$1" + singular.Substring(1));
        }

        public static void AddUncountable(string word)
        {
            if (word == null) throw new ArgumentNullException(nameof(word));
            UnCountables.Add(word.ToUpperInvariant());
        }

        public static void AddPlural(string rule, string replacement)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));

            Plurals.Add(new Rule(rule, replacement));
        }

        public static void AddSingular(string rule, string replacement)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            if (replacement == null) throw new ArgumentNullException(nameof(replacement));

            Singulars.Add(new Rule(rule, replacement));
        }


        public static string Pluralize(this string word)
        {
            if (word == null) throw new ArgumentNullException(nameof(word));

            return ApplyRules(Plurals, word);
        }

        public static string Singularize(this string word)
        {
            if (word == null) throw new ArgumentNullException(nameof(word));

            return ApplyRules(Singulars, word);
        }

        public static string Titleize(this string word)
        {
            if (word == null) throw new ArgumentNullException(nameof(word));

            return Regex.Replace(Humanize(Underscore(word)), @"\b([a-z])",
                match => match.Captures[0].Value.ToUpper(CultureInfo.CurrentCulture));
        }

        public static string Humanize(this string lowercaseAndUnderscoredWord)
        {
            if (lowercaseAndUnderscoredWord == null) throw new ArgumentNullException(nameof(lowercaseAndUnderscoredWord));

            return Capitalize(Regex.Replace(lowercaseAndUnderscoredWord, @"_", " "));
        }

        public static string Pascalize(this string lowercaseAndUnderscoredWord)
        {
            if (lowercaseAndUnderscoredWord == null) throw new ArgumentNullException(nameof(lowercaseAndUnderscoredWord));

            return Regex.Replace(lowercaseAndUnderscoredWord, "(?:^|_)(.)",
                match => match.Groups[1].Value.ToUpper(CultureInfo.CurrentCulture));
        }

        public static string Camelize(this string lowercaseAndUnderscoredWord)
        {
            if (lowercaseAndUnderscoredWord == null) throw new ArgumentNullException(nameof(lowercaseAndUnderscoredWord));

            return Uncapitalize(Pascalize(lowercaseAndUnderscoredWord));
        }

        public static string Underscore(this string pascalCasedWord)
        {
            if (pascalCasedWord == null) throw new ArgumentNullException(nameof(pascalCasedWord));

            return Regex.Replace(
                Regex.Replace(
                    Regex.Replace(pascalCasedWord, @"([A-Z]+)([A-Z][a-z])", "$1_$2"), @"([a-z\d])([A-Z])",
                    "$1_$2"), @"[-\s]", "_").ToLower(CultureInfo.CurrentCulture);
        }

        public static string Capitalize(this string word)
        {
            if (word == null) throw new ArgumentNullException(nameof(word));
            return word.Substring(0, 1).ToUpper(CultureInfo.CurrentCulture) + word.Substring(1).ToLower(CultureInfo.CurrentCulture);
        }

        public static string Uncapitalize(this string word)
        {
            if (word == null) throw new ArgumentNullException(nameof(word));
            return word.Substring(0, 1).ToLower(CultureInfo.CurrentCulture) + word.Substring(1);
        }

        public static string Ordinalize(this string numberString)
        {
            if (numberString == null) throw new ArgumentNullException(nameof(numberString));
            return Ordanize(int.Parse(numberString, CultureInfo.CurrentCulture), numberString);
        }

        public static string Ordinalize(this int number)
        {
            if (number <= 0) throw new ArgumentOutOfRangeException(nameof(number));
            return Ordanize(number, number.ToString(CultureInfo.CurrentCulture));
        }

        public static string Dasherize(this string underscoredWord)
        {
            if (underscoredWord == null) throw new ArgumentNullException(nameof(underscoredWord));
            return underscoredWord.Replace('_', '-');
        }

        // private

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ApplyRules(IReadOnlyList<Rule> rules, string word)
        {
            var result = word;

            if (!UnCountables.Contains(word.ToUpperInvariant()))
            {
                for (var i = rules.Count - 1; i >= 0; i--)
                {
                    if ((result = rules[i].Apply(word)) != null)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string Ordanize(int number, string numberString)
        {
            var nMod100 = number % 100;

            if (nMod100 >= 11 && nMod100 <= 13)
            {
                return numberString + "th";
            }

            switch (number % 10)
            {
                case 1:
                    return numberString + "st";
                case 2:
                    return numberString + "nd";
                case 3:
                    return numberString + "rd";
                default:
                    return numberString + "th";
            }
        }

        private class Rule
        {
            private readonly Regex _regex;
            private readonly string _replacement;

            public Rule(string pattern, string replacement)
            {
                _regex = new Regex(pattern, RegexOptions.IgnoreCase);
                _replacement = replacement;
            }

            public string Apply(string word)
            {
                return _regex.IsMatch(word) ? _regex.Replace(word, _replacement) : null;
            }
        }
    }
}