﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dexter.Configurations;

namespace Dexter.Helpers {

    /// <summary>
    /// Holds a variety of tools to deal with organic string management/manipulation.
    /// </summary>

    public static class LanguageHelper {

        private enum ArticleType {
            None,
            Uppercase,
            Lowercase
        }

        private enum PluralType {
            None,
            Plural
        }

        private enum PossessiveType {
            None,
            Possessive
        }

        /// <summary>
        /// Randomizes special groups of characters in <paramref name="Predicate"/> and fills them in with corresponding terms in <paramref name="TermBanks"/>.
        /// </summary>
        /// <remarks><para>The way to mark an expression for randomization is to wrap it in braces ("{}"). The format of each expression must be as follows: {IDENTIFIERX}, where IDENTIFIER is a key in <paramref name="TermBanks"/> and X is a positive integer value, every expression with the same identifier and value will be swapped for the same term.</para>
        /// <para>Special Identifiers exist, which do not run through terms: 'a' will try to guess the most probable expression of the indefinite article.</para></remarks>
        /// <param name="Predicate">The message whose terms are to be randomized.</param>
        /// <param name="TermBanks">A string-string[] dictionary where the keys are the explicit identifier of each TermClass and the values are list of terms those expressions can be substituted with.</param>
        /// <param name="RNG">A Random Number Generator used to extract random terms from <paramref name="TermBanks"/>.</param>
        /// <param name="Config">The LanguageConfiguration instance to run this process with.</param>
        /// <returns>A <c>string</c> holding the new, randomized predicate.</returns>

        public static string RandomizePredicate(string Predicate, Dictionary<string, string[]> TermBanks, Random RNG, LanguageConfiguration Config) {
            HashSet<TermClass> Terms = new HashSet<TermClass>();

            foreach (KeyValuePair<string, string[]> k in TermBanks) {
                Terms.Add(new TermClass(k.Key, k.Value));
            }

            StringBuilder NewPredicate = new StringBuilder(Predicate.Length * 2);

            ArticleType ResolveArticle = ArticleType.None;
            PluralType ResolvePlural = PluralType.None;
            PossessiveType ResolvePossessive = PossessiveType.None;

            while (Predicate.Length > 0) {
                int InsertIndex = Predicate.IndexOf(Config.TermInsertionStartIndicator);
                if (InsertIndex == -1) {
                    NewPredicate.Append(Predicate);
                    break;
                }

                int EndIndex = Predicate.IndexOf(Config.TermInsertionEndIndicator);
                if (EndIndex == -1) {
                    throw new FormatException($"There was an error parsing predicate {Predicate}, unbalanced braces. Please contact the developer team.");
                }

                NewPredicate.Append(Predicate[..InsertIndex]);

                string CompareString = Predicate[(InsertIndex + 1)..EndIndex];

                //SPECIAL CASES
                if (CompareString is "a" or "A") {
                    ResolveArticle = CompareString == "a" ? ArticleType.Lowercase : ArticleType.Uppercase;
                } else if (CompareString.ToLower() == "plural") {
                    ResolvePlural = PluralType.Plural;
                } else if (CompareString.ToLower() == "\'s") {
                    ResolvePossessive = PossessiveType.Possessive;
                } else {
                    //Default case
                    foreach (TermClass Term in Terms) {
                        int Index = Term.CheckReplace(CompareString, Config);

                        if (Index == (int)Config.ErrorCodeInvalidArgument)
                            throw new IndexOutOfRangeException($"There was an error parsing the number in term call \"{CompareString}\" within \"{Predicate}\". Please contact the developer team.");

                        if (Index >= 0) {
                            string newTerm = Term.GetOrGenerateCached(Index, RNG, Config);

                            if (ResolvePlural != PluralType.None) {
                                newTerm = newTerm.GuessPlural(Config);
                            }

                            if (ResolvePossessive != PossessiveType.None) {
                                newTerm = newTerm.Possessive();
                            }

                            if (ResolveArticle != ArticleType.None) {
                                newTerm = $"{GuessIndefiniteArticle(newTerm, ResolveArticle == ArticleType.Uppercase)} {newTerm}";
                            }

                            NewPredicate.Append(newTerm);

                            ResolvePlural = PluralType.None;
                            ResolveArticle = ArticleType.None;
                            ResolvePossessive = PossessiveType.None;
                            break;
                        }
                    }
                }

                Predicate = Predicate[(EndIndex + 1)..];
            }

            return NewPredicate.ToString();
        }

        /// <summary>
        /// Checks whether an "IDENTIFIERX" expression shares an identifier with <paramref name="Term"/>, and returns the index X.
        /// </summary>
        /// <param name="Str">The raw form of the identifier-index expression, without braces.</param>
        /// <param name="Term">The TermClass to compare <paramref name="Str"/> against.</param>
        /// <param name="Config">The LanguageConfiguration instance to run this process with.</param>
        /// <returns>The number accompanying the raw expression <paramref name="Str"/> if their identifiers match, Invalid Number Error Code if the term X can't be parsed to a positive integer, -1 otherwise.</returns>

        private static int CheckReplace(this TermClass Term, string Str, LanguageConfiguration Config) {
            if (!Str.StartsWith(Term.Identifier))
                return -1;

            if (int.TryParse(Str[Term.Identifier.Length..], out int Index)) {
                return Index >= 0 ? Index : (int)Config.ErrorCodeInvalidArgument;
            }

            return (int)Config.ErrorCodeInvalidArgument;
        }

        /// <summary>
        /// Holds a list of terms related by a common Identifier and provides a way to generate random terms.
        /// </summary>

        internal class TermClass {

            public readonly string Identifier;
            private readonly string[] Bank;
            private readonly List<string> Cache;

            public TermClass(string Identifier, string[] Bank) {
                this.Identifier = Identifier;
                this.Bank = Bank;
                Cache = new List<string>();
            }

            /// <summary>
            /// Gets a cached term located at <paramref name="Index"/>, if Cache doesn't have those many elements, it generates elements up to <paramref name="Index"/>.
            /// </summary>
            /// <remarks>The TermClass will attempt to generate new terms that aren't in its cache. To disable this, set <paramref name="MaxRetries"/> to 0.</remarks>
            /// <param name="Index">The index to get from the cache of terms stored in this TermClass.</param>
            /// <param name="Generator">A random number generator.</param>
            /// <param name="Config">The LanguageConfiguration instance to run this process with.</param>
            /// <param name="MaxRetries">The maximum amount of attempts to generate a novel term from the bank.</param>
            /// <returns>The term at position <paramref name="Index"/> within the Cache.</returns>

            public string GetOrGenerateCached(int Index, Random Generator, LanguageConfiguration Config, int MaxRetries = -1) {
                if (MaxRetries < 0)
                    MaxRetries = (int)Config.TermRepetitionAversionFactor;

                while (Cache.Count <= Index) {
                    Cache.Add(Bank[Generator.Next(Bank.Length)]);

                    //If this term is already in the cache, try to replace it for a new, random one.
                    for (int i = 0; i < MaxRetries && Cache.IndexOf(Cache[^1]) < Cache.Count - 1; i++) {
                        Cache[^1] = Bank[Generator.Next(Bank.Length)];
                    }
                }

                return Cache[Index];
            }
        }

        /// <summary>
        /// Will attempt to guess whether the indefinite article should be 'a' or 'an' based on <paramref name="NextWord"/>.
        /// </summary>
        /// <param name="NextWord">A string describing what follows the article.</param>
        /// <param name="Capitalize">Whether to capitalize the first letter of the article.</param>
        /// <returns>A string, either "a", "an", or "a(n)", where the character 'a' is capitalized if <paramref name="Capitalize"/> is set to <see langword="true"/>.</returns>

        public static string GuessIndefiniteArticle(string NextWord, bool Capitalize = false) {
            string Relevant = NextWord.Trim().Split(' ')[0].ToLower();

            return (Relevant[0]) switch {
                'a' or 'e' or 'i' or 'o' => Capitalize ? "An" : "an",
                'h' or 'u' => Capitalize ? "A(n)" : "a(n)",
                _ => Capitalize ? "A" : "a",
            };
        }

        /// <summary>
        /// Guesses the most likely plural form of a noun from a set of English pluralization rules and irregular plurals.
        /// </summary>
        /// <param name="Singular">The singular noun to pluralize.</param>
        /// <param name="Config">The LanguageConfiguration instance to run this process with.</param>
        /// <returns>A string containing the pluralized form of <paramref name="Singular"/>.</returns>

        public static string GuessPlural(this string Singular, LanguageConfiguration Config) {
            string LowerSingular = Singular.ToLower();

            if (Config.IrregularPlurals.ContainsKey(LowerSingular)) {
                return Config.IrregularPlurals[LowerSingular].MatchCase(Singular);
            } else if (LowerSingular.EndsWith("on")) {
                return Singular[..^2] + "a";
            } else if (LowerSingular.EndsWith("um")) {
                return Singular[..^2] + "a";
            } else if (LowerSingular.EndsWith("us")) {
                return Singular[..^2] + "i";
            } else if (LowerSingular.EndsWith("is")) {
                return Singular[..^2] + "es";
            } else if (LowerSingular.EndsWith("ex") || LowerSingular.EndsWith("ix")) {
                return Singular[..^2] + "ices";
            } else if (LowerSingular[^1] is 's' or 'z') {
                if (LowerSingular.Length > 2 && LowerSingular[^3].IsConsonant() && LowerSingular[^2].IsVowel() && LowerSingular[^1].IsConsonant()) {
                    return Singular + Singular[^1] + "es";
                } else {
                    return Singular + "es";
                }
            } else if (LowerSingular.EndsWith("fe")) {
                return Singular[..^2] + "ves";
            } else if (LowerSingular.EndsWith("f")) {
                return Singular[..^1] + "ves";
            } else if (LowerSingular.EndsWith("y")) {
                return Singular[^2].IsVowel() ? Singular + "s" : Singular[^1] + "ies";
            } else if (LowerSingular[^1] is 'o' or 'x' || LowerSingular.EndsWith("sh") || LowerSingular.EndsWith("ch")) {
                return Singular + "es";
            }

            return Singular + "s";
        }

        /// <summary>
        /// Checks whether a character is a vowel in the Latin alphabet
        /// </summary>
        /// <param name="c">The character to check</param>
        /// <returns><see langword="true"/> if <paramref name="c"/> is a vowel, <see langword="false"/> otherwise.</returns>

        public static bool IsVowel(this char c) { return char.ToLower(c) is 'a' or 'e' or 'i' or 'o' or 'u'; }

        /// <summary>
        /// Checks whether a character is a consonant in the Latin alphabet
        /// </summary>
        /// <param name="c">The character to check</param>
        /// <returns><see langword="true"/> if <paramref name="c"/> is a consonant, <see langword="false"/> otherwise.</returns>

        public static bool IsConsonant(this char c) { return char.ToLower(c) >= 'a' && char.ToLower(c) <= 'z' && !IsVowel(c); }

        /// <summary>
        /// Matches the case of <paramref name="Input"/> to that of <paramref name="Case"/>.
        /// </summary>
        /// <param name="Input">The sequence of letters to convert to <paramref name="Case"/>.</param>
        /// <param name="Case">The uppercase-lowercase sequence to follow. If the length differs from <paramref name="Input"/>, case-matching will stop..</param>
        /// <returns>A string with the same letters as <paramref name="Input"/> and the same case as <paramref name="Case"/>.</returns>

        public static string MatchCase(this string Input, string Case) {
            int Match = Input.Length < Case.Length ? Input.Length : Case.Length;

            StringBuilder SB = new(Input.Length);

            for (int i = 0; i < Match; i++) {
                SB.Append(Input[i].MatchCase(Case[i]));
            }

            if (Match < Input.Length) {
                SB.Append(Input[Match..]);
            }

            return SB.ToString();
        }

        /// <summary>
        /// Matches the case of <paramref name="Input"/> to that of <paramref name="Case"/>.
        /// </summary>
        /// <param name="Input">The character to convert <paramref name="Case"/>.</param>
        /// <param name="Case">The uppercase or lowercase character to reference for conversion of <paramref name="Input"/>.</param>
        /// <returns>The character obtained from <paramref name="Input"/> in the same case as <paramref name="Case"/>.</returns>

        public static char MatchCase(this char Input, char Case) {
            if (char.IsUpper(Case))
                return char.ToUpper(Input);

            return char.ToLower(Input);
        }

        /// <summary>
        /// Obtains the ordinal form of an integer <paramref name="N"/>.
        /// </summary>
        /// <param name="N">The base number to obtain the ordinal from.</param>
        /// <returns>The string "<paramref name="N"/>st", "<paramref name="N"/>nd", "<paramref name="N"/>rd" if any are appropriate, otherwise "<paramref name="N"/>th".</returns>

        public static string Ordinal(this int N) {
            if (N < 0)
                N = -N;

            if (N % 100 / 10 == 1)
                return N + "th";

            return (N % 10) switch {
                1 => N + "st",
                2 => N + "nd",
                3 => N + "rd",
                _ => N + "th"
            };
        }

        /// <summary>
        /// Adds the appropriate form of the possessive "'s" to a term <paramref name="Input"/>.
        /// </summary>
        /// <param name="Input">The noun to add the possessive termination to.</param>
        /// <returns>"<paramref name="Input"/>'" if <paramref name="Input"/> ends in 's'. "<paramref name="Input"/>'s" otherwise.</returns>

        public static string Possessive(this string Input) {
            return $"{Input}'{(Input.EndsWith('s') ? "" : "s")}";
        }

        /// <summary>
        /// Attempts to parse a string expressing a date, time, and offset. All elements except for the hour and minute are optional.
        /// </summary>
        /// <param name="Input">The stringified expression of the date to be parsed into <paramref name="Time"/>.</param>
        /// <param name="CultureInfo">The Cultural Context with which to parse the date given in <paramref name="Input"/>.</param>
        /// <param name="LanguageConfiguration">The Configuration related to parsing linguistic humanized information like time zone abbreviations.</param>
        /// <param name="Time">The parsed <c>DateTimeOffset</c> extracted from <paramref name="Input"/>.</param>
        /// <returns><see langword="true"/> if the parsing was successful; otherwise <see langword="false"/>.</returns>

        public static bool TryParseTime(this string Input, CultureInfo CultureInfo, LanguageConfiguration LanguageConfiguration, out DateTimeOffset Time) {

            Input = Input.Trim();
            Time = DateTimeOffset.Now;

            switch (Input.ToLower()) {
                case "now":
                    Time = DateTimeOffset.Now;
                    return true;
            }

            int Year = DateTime.Now.Year;
            int Month = DateTime.Now.Month;
            int Day = DateTime.Now.Day;
            int Hour = DateTime.Now.Hour;
            int Minute = DateTime.Now.Minute;
            float Second = DateTime.Now.Second + DateTime.Now.Millisecond / 1000f;

            TimeSpan TimeZoneOffset = DateTimeOffset.Now.Offset;

            string DateStrSegment = Regex.Match(Input, @"([A-Za-z]+\s)([0-9]{1,2})(,\s?[0-9]{2,4})?").Value;
            string DateNumSegment = Regex.Match(Input, @"[0-9]{1,2}\/[0-9]{1,2}(\/[0-9]{2,4})?").Value;
            string TimeSegment = Regex.Match(Input, @"[0-9]{1,2}:[0-9]{1,2}(:[0-9]{1,2}(.[0-9]+)?)?(\s(a|p)m)?", RegexOptions.IgnoreCase).Value;
            string TimeZoneSegment = Regex.Match(Input, @"(((UTC|GMT|Z)?[+-][0-9]{1,2}(:[0-9]{2})?)|([A-Z][A-Za-z0-9]*))$").Value;

            if(!string.IsNullOrEmpty(DateStrSegment)) {
                string MMM = DateStrSegment.Split(" ")[0];
                Month = ParseMonth(MMM);
                if(Month < 0) {
                    return false;
                }

                string[] dy = DateStrSegment.Split(",");
                string dd = dy[0][MMM.Length..];
                if (!int.TryParse(dd, out Day)) return false;
                if (Day < 0 || Day > 31) return false;

                if(dy.Length > 1) {
                    if (!int.TryParse(dy[1], out Year)) return false;
                    if (Year < 1970) Year += 2000;
                    if (Year < 1970 || Year > 3000) return false;
                }
            } else if (!string.IsNullOrEmpty(DateNumSegment)) {
                if(DateNumSegment.Split("/").Length < 3) {
                    DateNumSegment += $"/{Year}";
                }
                DateTime Subparse = DateTime.Parse(DateNumSegment, CultureInfo);
                Day = Subparse.Day;
                Month = Subparse.Month;
                Year = Subparse.Year;
            }

            if (string.IsNullOrEmpty(TimeSegment)) return false;

            string[] hmsf = TimeSegment.Split(" ")[0].Split(":");
            Hour = int.Parse(hmsf[0]);
            Minute = int.Parse(hmsf[1]);

            if (hmsf.Length > 2) Second = float.Parse(hmsf[2]);

            if (TimeSegment.ToLower().EndsWith("am") && Hour == 12) Hour = 0;
            if (TimeSegment.ToLower().EndsWith("pm") && Hour != 12) Hour += 12;

            if(!string.IsNullOrEmpty(TimeZoneSegment)) {
                if(TimeZoneSegment.Contains("+") || TimeZoneSegment.Contains("-") || LanguageConfiguration.TimeZones.ContainsKey(TimeZoneSegment)) {
                    TimeZoneData TimeZone = TimeZoneData.Parse(TimeZoneSegment, LanguageConfiguration);

                    TimeZoneOffset = TimeZone.TimeOffset;
                }
            }

            Time = new DateTimeOffset(new DateTime(Year, Month, Day, Hour, Minute, (int)Second, (int)(Second % 1 * 1000)), TimeZoneOffset);

            return true;
        }

        /// <summary>
        /// Attempts to parse a Month given a CultureInfo for Month Names and Abbreviated Month Names.
        /// </summary>
        /// <param name="Input">An abbreviated or complete month name in accordance to <paramref name="CultureInfo"/>, case-insensitive.</param>
        /// <param name="CultureInfo">The contextual CultureInfo containing the calendar information and month names.</param>
        /// <returns></returns>

        public static int ParseMonth(this string Input, CultureInfo CultureInfo = null) {
            if (CultureInfo == null) CultureInfo = CultureInfo.InvariantCulture;

            Input = Input.ToLower();

            for (int i = 0; i < CultureInfo.DateTimeFormat.MonthNames.Length; i++) {
                if(Input == CultureInfo.DateTimeFormat.MonthNames[i].ToLower() || Input == CultureInfo.DateTimeFormat.AbbreviatedMonthNames[i].ToLower()) {
                    return i + 1;
                }
            }

            return -1;
        }

        /// <summary>
        /// Attempts to find a time zone by the abbreviated name of <paramref name="Input"/> and returns it as a TimeSpan to be used as an Offset. 
        /// </summary>
        /// <param name="Input">The abbreviation of the time zone as it appears in <paramref name="LanguageConfiguration"/>.</param>
        /// <param name="LanguageConfiguration">The Config file containing data on Time Zone names and their respective offsets.</param>
        /// <param name="TimeSpan">The output value of the parsed <paramref name="Input"/>, or <c>TimeSpan.Zero</c> if it can't be parsed.</param>
        /// <returns><see langword="true"/> if the parsing was successful; otherwise <see langword="false"/>.</returns>

        public static bool TryParseTimeZone(this string Input, LanguageConfiguration LanguageConfiguration, out TimeSpan TimeSpan) {
            if(LanguageConfiguration.TimeZones.ContainsKey(Input)) {
                TimeSpan = LanguageConfiguration.TimeZones[Input].TimeOffset;
                return true;
            }

            TimeSpan = TimeSpan.Zero;
            return false;
        }

        /// <summary>
        /// Searches the list of static time zone abbreviations in <paramref name="LanguageConfiguration"/> to find the closest expressions to <paramref name="Input"/>.
        /// </summary>
        /// <param name="Input">The search term.</param>
        /// <param name="LanguageConfiguration">The global language configuration from which to draw time zone abbreviations.</param>
        /// <returns>A <c>string[]</c> array of time zone abbreviations from <paramref name="LanguageConfiguration"/> which are most similar to <paramref name="Input"/>, sorted by relevance.</returns>

        public static string[] SearchTimeZone(this string Input, LanguageConfiguration LanguageConfiguration) {
            Dictionary<string, int> SearchWeight = new();

            foreach(KeyValuePair<string, TimeZoneData> k in LanguageConfiguration.TimeZones) {
                int Weight = 0;
                for(int i = 0; i < k.Key.Length; i++) {
                    if(i < Input.Length) {
                        if (Input[i] == k.Key[i]) Weight += 10;
                        else if (char.ToUpper(Input[i]) == char.ToUpper(k.Key[i])) Weight += 9;
                    }
                    if (Input.Contains(k.Key[i])) Weight += 3;
                }

                if(Input.Length >= 2 && Input[^2] == 'S') {
                    if (k.Key.Length >= 2 && k.Key[^2] == 'D') Weight += 8;
                    else if (k.Key.Length + 1 == Input.Length) Weight += 4;
                }

                if(k.Key.Length >= 2 && k.Key[^2] == 'S') {
                    if (Input.Length >= 2 && Input[^2] == 'D') Weight += 8;
                    else if (Input.Length + 1 == k.Key.Length) Weight += 4;
                }

                if (Input.ToUpper() == k.Key.ToUpper()) Weight += 100;

                SearchWeight.Add(k.Key, Weight);
            }
            
            List<KeyValuePair<string, int>> WeightedList = SearchWeight.ToList();
            WeightedList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            string[] SortedResults = new string[WeightedList.Count];
            for(int i = 0; i < WeightedList.Count; i++) {
                SortedResults[i] = WeightedList[i].Key;
            }
            return SortedResults;
        }

        /// <summary>
        /// Searches the list of static time zone data in <paramref name="LanguageConfiguration"/> to find timezones whose offset is closest to <paramref name="Offset"/>.
        /// </summary>
        /// <param name="Offset">A TimeSpan representing the difference between UTC and a given time zone.</param>
        /// <param name="LanguageConfiguration">The configuration required to parse time zone data.</param>
        /// <param name="ExactMatches">The number of matches in the results that have the exact <paramref name="Offset"/> provided.</param>
        /// <returns>A <c>string[]</c> array of time zone abbreviations from <paramref name="LanguageConfiguration"/> which are most similar in offset to <paramref name="Offset"/>, sorted by relevance.</returns>

        public static string[] SearchTimeZone(this TimeSpan Offset, LanguageConfiguration LanguageConfiguration, out int ExactMatches) {
            Dictionary<string, int> SearchWeight = new();
            ExactMatches = 0;

            foreach (KeyValuePair<string, TimeZoneData> k in LanguageConfiguration.TimeZones) {
                int Weight = (int) Math.Abs(k.Value.TimeOffset.Subtract(Offset).TotalMinutes);

                if (Weight == 0) ExactMatches++;

                SearchWeight.Add(k.Key, Weight);
            }

            List<KeyValuePair<string, int>> WeightedList = SearchWeight.ToList();
            WeightedList.Sort((pair1, pair2) => {
                if (pair1.Value.CompareTo(pair2.Value) != 0) return pair1.Value.CompareTo(pair2.Value);
                else return pair1.Key.CompareTo(pair2.Key);
                });

            string[] SortedResults = new string[WeightedList.Count];
            for (int i = 0; i < WeightedList.Count; i++) {
                SortedResults[i] = WeightedList[i].Key;
            }
            return SortedResults;
        }
    }

    /// <summary>
    /// Represents a time zone for comparison as an offset to UTC.
    /// </summary>

    [Serializable]
    public class TimeZoneData {

        /// <summary>
        /// The Full name of the time zone.
        /// </summary>

        public string Name { get; set; }

        /// <summary>
        /// The offset to UTC of the time zone, in hours.
        /// </summary>

        public float Offset { get; set; }

        /// <summary>
        /// The offset to UTC of the time zone, as a <c>TimeSpan</c>.
        /// </summary>

        public TimeSpan TimeOffset { get { return TimeSpan.FromHours(Offset); } }

        /// <summary>
        /// Stringifies the given timezone
        /// </summary>
        /// <returns>A string expression of the time zone, with critical information.</returns>

        public override string ToString() {
            return $"{Name} | {ToTimeZoneExpression(Offset)}";
        }

        /// <summary>
        /// Gives a human-readable form of the <paramref name="Offset"/> relative to UTC.
        /// </summary>
        /// <param name="Offset">The number of hours offset from UTC.</param>
        /// <returns>A string expressing a human-readable form of the offset relative to UTC.</returns>

        public static string ToTimeZoneExpression(float Offset) {
            return $"UTC{(Offset >= 0 ? "+" : "")}{(int)Offset}:{Math.Abs(Offset % 1 * 60):00}";
        }

        /// <summary>
        /// Gives a human-readable form of the <paramref name="Offset"/> TimeSpan relative to UTC.
        /// </summary>
        /// <param name="Offset">The TimeSpan object representing the offset of hours relative to UTC.</param>
        /// <returns>A string expressing a human-readable form of the offset relative to UTC.</returns>

        public static string ToTimeZoneExpression(TimeSpan Offset) {
            return ToTimeZoneExpression(Offset.Hours + Offset.Minutes / 60f);
        }

        /// <summary>
        /// Attempts to Parse a TimeZone data from given information.
        /// </summary>
        /// <param name="Str">The string to parse into TimeZone data.</param>
        /// <param name="LanguageConfiguration">The Configuration data containing static time zone definitions.</param>
        /// <returns>A <c>TimeZoneData</c> object whose name is <paramref name="Str"/> and Offset is obtained by parsing <paramref name="Str"/>.</returns>

        public static TimeZoneData Parse(string Str, LanguageConfiguration LanguageConfiguration) {
            TimeZoneData Result = new();
            
            Result.Offset = 0;
            Result.Name = Str;

            int Sign = 1;

            int SignPos = Str.IndexOf("+");
            if (SignPos < 0) {
                Sign = -1;
                SignPos = Str.IndexOf("-");
            }

            string TZString = SignPos < 0 ? Str : Str[..SignPos];
            if (!string.IsNullOrEmpty(TZString)) {
                if (LanguageHelper.TryParseTimeZone(TZString.Trim(), LanguageConfiguration, out TimeSpan Offset)) {
                    Result.Offset = Offset.Hours;
                    Result.Offset += Offset.Minutes / 60f;
                }
            }

            if (SignPos >= 0) {
                string[] Mods = Str[(SignPos + 1)..].Split(":");
                Result.Offset += int.Parse(Mods[0]) * Sign;
                if (Mods.Length > 1) Result.Offset += int.Parse(Mods[1]) / 60f;
            }

            return Result;
        }
        
        /// Extracts substrings that fit a given url schema from an <paramref name="Input"/> string.
        /// </summary>
        /// <remarks>All potential links in the string must be encapsulated in parentheses or spaces.</remarks>
        /// <param name="Input">The string to analyze and extracts urls from.</param>
        /// <returns>A <c>string[]</c> array containing a collection of substrings that matched the url pattern in <paramref name="Input"/>.</returns>

        public static string[] GetHyperLinks(this string Input) {
            List<string> Matches = new List<string>();

            Input = ' ' + Input + ' ';

            List<int> Openers = new List<int>();
            List<int> Closers = new List<int>();
            for (int i = 0; i < Input.Length; i++) {
                switch (Input[i]) {
                    case ' ':
                        Closers.Add(i);
                        Matches.AddRange(Input.CheckForLinks(Openers, Closers));

                        Openers.Clear();
                        Closers.Clear();
                        Openers.Add(i);
                        break;
                    case ')':
                        Closers.Add(i);
                        break;
                    case '(':
                        Openers.Add(i);
                        break;
                }
            }

            return Matches.ToArray();
        }

        private static string[] CheckForLinks(this string Input, IEnumerable<int> Openers, IEnumerable<int> Closers) {
            List<string> Result = new List<string>();

            foreach(int o in Openers) {
                foreach(int c in Closers) {
                    if(c > o) {
                        string s = Input[(o + 1)..c];
                        if (s.IsHyperLink()) {
                            Result.Add(s);
                        }
                    }
                }
            }

            return Result.ToArray();
        }

        /// <summary>
        /// Checks whether a given <paramref name="Input"/> string is a url.
        /// </summary>
        /// <param name="Input">The string to check.</param>
        /// <returns><see langword="true"/> if the given string is a url; otherwise <see langword="false"/>.</returns>

        public static bool IsHyperLink(this string Input) {
            return Regex.IsMatch(Input, @"^(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&%\$#_=]*)?$");
        }
    }
}