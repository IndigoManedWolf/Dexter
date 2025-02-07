﻿using Dexter.Configurations;
using Dexter.Helpers;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Dexter.Databases.UserProfiles
{

    /// <summary>
    /// The Borkday class contains information on a user's last borkday.
    /// </summary>

    public class UserProfile
    {

        /// <summary>
        /// The UserID is the KEY of the table.
        /// It is the snowflake ID of the user that has had the borkday.
        /// </summary>

        [Key]

        public ulong UserID { get; set; }

        /// <summary>
        /// The UNIX time of when the borkday role was added last.
        /// </summary>

        public long BorkdayTime { get; set; }

        /// <summary>
        /// The time the user joined for the first time, expressed in seconds since UNIX time.
        /// </summary>

        public long DateJoined { get; set; }

        /// <summary>
        /// The user's gender and pronouns.
        /// </summary>

        public string? Gender { get; set; }

        /// <summary>
        /// The user's sexual and romantic orientation.
        /// </summary>

        public string? Orientation { get; set; }

        /// <summary>
        /// The user's birthday date for each year.
        /// </summary>

        [NotMapped]
        public DayInYear Borkday
        {
            get
            {
                try
                {
                    return JsonConvert.DeserializeObject<DayInYear>(BorkdayStr);
                }
                catch
                {
                    return null;
                }
            }
            set
            {
                BorkdayStr = JsonConvert.SerializeObject(value);
            }
        }

        /// <summary>
        /// The JSON expression for Borkday.
        /// </summary>

        public string? BorkdayStr { get; set; }

        /// <summary>
        /// The user's birth year.
        /// </summary>

        public int? BirthYear { get; set; }

        /// <summary>
        /// The token for the timer that control the borkday role event for the user attached to this profile.
        /// </summary>

        public string? BorkdayTimerToken { get; set; }

        /// <summary>
        /// The user's time zone abbreviation for non-daylight saving time.
        /// </summary>

        public string TimeZone { get; set; }

        /// <summary>
        /// The user's time zone abbreviation for daylight saving time.
        /// </summary>

        public string? TimeZoneDST { get; set; }

        /// <summary>
        /// Describes the rules of DST functionality for the user's local area.
        /// </summary>

        [NotMapped]
        public DaylightShiftRules DSTRules
        {
            get
            {
                try
                {
                    return JsonConvert.DeserializeObject<DaylightShiftRules>(DSTRulesStr);
                }
                catch
                {
                    return null;
                }
            }
            set
            {
                DSTRulesStr = JsonConvert.SerializeObject(value);
            }
        }

        /// <summary>
        /// String JSON representation of DSTRules. 
        /// </summary>

        public string? DSTRulesStr { get; set; }

        /// <summary>
        /// The user's sona information provided by the user.
        /// </summary>

        public string? SonaInfo { get; set; }

        /// <summary>
        /// The user's location, up to the specificity that the user wishes to set.
        /// </summary>

        public string? Nationality { get; set; }

        /// <summary>
        /// The user's known languages. 
        /// </summary>

        public string? Languages { get; set; }

        /// <summary>
        /// Miscellaneous user information.
        /// </summary>

        public string? Info { get; set; }

        /// <summary>
        /// Represents the per-user specific preferences to do with the profile system.
        /// </summary>

        [NotMapped]
        public ProfilePreferences Settings
        {
            get
            {
                try
                {
                    return JsonConvert.DeserializeObject<ProfilePreferences>(SettingsStr);
                }
                catch
                {
                    return null;
                }
            }
            set
            {
                SettingsStr = JsonConvert.SerializeObject(value);
            }
        }

        /// <summary>
        /// JSON representation of the profile preferences object.
        /// </summary>

        public string SettingsStr { get; set; }

        /// <summary>
        /// Obtains the current time zone for a user with correctly set up time zone data.
        /// </summary>
        /// <param name="languageConfiguration">The Language Configuration item necessary for time zone parsing.</param>
        /// <returns>A <see cref="TimeZoneData"/> object detailing the information of the user's time zone.</returns>

        public TimeZoneData GetRelevantTimeZone(LanguageConfiguration languageConfiguration)
        {
            return GetRelevantTimeZone(DateTimeOffset.Now, languageConfiguration);
        }

        /// <summary>
        /// Obtains the current time zone for a user with correctly set up time zone data.
        /// </summary>
        /// <param name="languageConfiguration">The Language Configuration item necessary for time zone parsing.</param>
        /// <param name="day">The day to calculate the time zone data for.</param>
        /// <returns>A <see cref="TimeZoneData"/> object detailing the information of the user's time zone.</returns>

        public TimeZoneData GetRelevantTimeZone(DateTimeOffset day, LanguageConfiguration languageConfiguration)
        {
            if (!TimeZoneData.TryParse(TimeZone, languageConfiguration, out TimeZoneData TZ))
                return null;

            if (DSTRules is null || !DSTRules.IsDST(day))
            {
                return TZ;
            }
            else
            {
                if (!TimeZoneData.TryParse(TimeZoneDST, languageConfiguration, out TimeZoneData TZDST))
                {
                    return TZ;
                }
                return TZDST;
            }
        }

        /// <summary>
        /// Gets the current time shifted to the user's configured timezone.
        /// </summary>
        /// <param name="languageConfiguration">The configuration data necessary for time zone parsing.</param>
        /// <returns>A <see cref="DateTimeOffset"/> object containing the current time shifted to the appropriate time zone.</returns>

        public DateTimeOffset GetNow(LanguageConfiguration languageConfiguration)
        {
            return DateTimeOffset.Now.ToOffset(GetRelevantTimeZone(languageConfiguration).TimeOffset);
        }

    }

    /// <summary>
    /// Represents a periodic day in any given year.
    /// </summary>

    [Serializable]
    public class DayInYear
    {

        /// <summary>
        /// The day of the month that this date refers to.
        /// </summary>

        public sbyte Day { get; set; }

        /// <summary>
        /// The month of the year that this date refers to.
        /// </summary>

        public LanguageHelper.Month Month { get; set; }

        /// <summary>
        /// Returns the weekday this object refers to if it is interpreted as relative.
        /// </summary>

        public LanguageHelper.Weekday RelativeWeekday { get { return (LanguageHelper.Weekday)(Day % 7 < 0 ? (Day % 7) + 7 : Day % 7); } }

        /// <summary>
        /// Returns which weekday of the same type in the month this object refers to if it's relative.
        /// </summary>

        public int WeekdayCount { get { return Day < 0 ? -1 : Day / 7; } }

        /// <summary>
        /// Obtains a text expression of the absolute day this object represents.
        /// </summary>
        /// <returns>A human-readable string expression the day and the month.</returns>

        public override string ToString()
        {
            return ToString(true);
        }

        /// <summary>
        /// Expresses the value that this object represents in a human-readable way.
        /// </summary>
        /// <param name="isAbsolute">Interprets the day as absolute if <see langword="true"/>, otherwise interprets it in the weekday count relative system.</param>
        /// <returns>A string briefly describing the values this object holds.</returns>

        public string ToString(bool isAbsolute = true)
        {
            if (isAbsolute) return $"{((int)Day).Ordinal()} of {Month}";
            else return $"{(WeekdayCount < 0 ? "last" : (WeekdayCount + 1).Ordinal())} {RelativeWeekday} of {Month}";
        }

        static readonly string[] ordinals = { "last", "first", "second", "third", "fourth", "fifth", "sixth" };

        /// <summary>
        /// Attempts to parse an absolute or relative day of the year from <paramref name="input"/> based on a set of parameters.
        /// </summary>
        /// <param name="input">The string to attempt to parse.</param>
        /// <param name="isAbsolute">If <see langword="true"/>, the date will be treated as a specific day in a month, otherwise it will de interpreted as a specific weekday.</param>
        /// <param name="languageConfig">The configuration required to parse specific data from natural language.</param>
        /// <param name="dayInYear">The output of the parsing.</param>
        /// <param name="feedback">Any errors or specific feedback given on completion or failure.</param>
        /// <param name="year">The year obtained from the absolute parsing of the date, or -1 if none are found.</param>
        /// <param name="cultureInfo">Optional information about what specific culture to use for the calendar.</param>
        /// <returns><see langword="true"/> if the parsing was successful, otherwise <see langword="false"/>.</returns>

        public static bool TryParse(string input, bool isAbsolute, LanguageConfiguration languageConfig, out DayInYear dayInYear, out string feedback, out int year, CultureInfo cultureInfo = null)
        {
            if (cultureInfo is null) cultureInfo = CultureInfo.InvariantCulture;
            year = -1;
            dayInYear = new();

            if (isAbsolute)
            {
                if (LanguageHelper.TryParseTime(input, cultureInfo, languageConfig, out DateTimeOffset parsedTimeStart, out feedback))
                {
                    dayInYear.Day = (sbyte)parsedTimeStart.Day;
                    dayInYear.Month = (LanguageHelper.Month)(parsedTimeStart.Month);
                    year = parsedTimeStart.Year;
                    feedback = $"Successfully parsed: {dayInYear.ToString(true)}";
                    return true;
                }
                return false;
            }
            else
            {
                input = Regex.Match(input, @"\s*([a-z]+|0-9)(st|nd|rd|th)\s+[a-z]{2,}\s+(of|in)?\s+[a-z]{3,}\s*", RegexOptions.IgnoreCase).Value;
                if (string.IsNullOrEmpty(input))
                {
                    feedback = $"Couldn't parse relative time; input {input} doesn't follow the pattern: `[Ordinal] [Weekday] (<of|in>) [Month]`";
                    return false;
                }

                string[] segments = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length < 3)
                {
                    feedback = $"Invalid number of arguments for relative time.";
                    return false;
                }

                if (segments[0][0] >= '0' && segments[0][0] <= '9')
                {
                    dayInYear.Day = (sbyte)(7 * (segments[0][0] - '1'));
                }
                else
                {
                    bool success = false;
                    for (int i = 0; i < ordinals.Length; i++)
                    {
                        if (segments[0].ToLower() == ordinals[i])
                        {
                            dayInYear.Day = (sbyte)(7 * i);
                            success = true;
                            break;
                        }
                    }

                    if (!success)
                    {
                        feedback = $"Unable to process {segments[0]} into a valid ordinal number.";
                        return false;
                    }
                }

                if (!LanguageHelper.TryParseWeekday(segments[1], out LanguageHelper.Weekday weekday, out feedback))
                {
                    feedback = "Unable to parse day of the week; " + feedback;
                    return false;
                }
                dayInYear.Day += (sbyte)weekday;

                dayInYear.Month = LanguageHelper.ParseMonthEnum(segments[^1]);
                if (dayInYear.Month == LanguageHelper.Month.None)
                {
                    feedback = $"Unable to parse {segments[^1]} into a valid month.";
                    return false;
                }

                feedback = $"Successfully parsed day to {dayInYear}";
                return true;
            }
        }

        /// <summary>
        /// Attempts to parse an absolute or relative day of the year from <paramref name="input"/> based on a set of parameters.
        /// </summary>
        /// <param name="input">The string to attempt to parse.</param>
        /// <param name="isAbsolute">If <see langword="true"/>, the date will be treated as a specific day in a month, otherwise it will de interpreted as a specific weekday.</param>
        /// <param name="languageConfig">The configuration required to parse specific data from natural language.</param>
        /// <param name="dayInYear">The output of the parsing.</param>
        /// <param name="feedback">Any errors or specific feedback given on completion or failure.</param>
        /// <param name="cultureInfo">Optional information about what specific culture to use for the calendar.</param>
        /// <returns><see langword="true"/> if the parsing was successful, otherwise <see langword="false"/>.</returns>

        public static bool TryParse(string input, bool isAbsolute, LanguageConfiguration languageConfig, out DayInYear dayInYear, out string feedback, CultureInfo cultureInfo = null)
        {
            return TryParse(input, isAbsolute, languageConfig, out dayInYear, out feedback, out _, cultureInfo);
        }

        /// <summary>
        /// Gets the day of the month that this day of year represents in relative mode for a given year.
        /// </summary>
        /// <param name="year">The relevant year for the calculation.</param>
        /// <returns>A number representing the day of the month the nth weekday will fall on.</returns>

        public int GetThresholdDay(int year)
        {
            int monthStartWeekday = (int)(CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(new DateTime(year, (int)Month, 1)) - 1) % 7;
            int weekdayDiff = (monthStartWeekday - (int)RelativeWeekday) % 7;
            if (weekdayDiff < 0) weekdayDiff += 7;

            int thresholdDay;

            if (Day < 0)
            {
                //Last weekday of the month
                int maxDay = CultureInfo.InvariantCulture.Calendar.GetDaysInMonth(year, (int)Month);
                for (thresholdDay = weekdayDiff + 1; thresholdDay <= maxDay - 7; thresholdDay += 7) { }
                return thresholdDay;
            }
            else
            {
                return weekdayDiff + 7 * WeekdayCount + 1;
            }
        }

        /// <summary>
        /// Converts a given date into a dayInYear format.
        /// </summary>
        /// <param name="day">The Date object to obtain data from.</param>
        /// <returns>A DayInYear object that represents the day in the year held in <paramref name="day"/>.</returns>

        public static DayInYear FromDateTime(DateTimeOffset day)
        {
            return new DayInYear() { Day = (sbyte)day.Day, Month = (LanguageHelper.Month)(day.Month) };
        }
    }

    /// <summary>
    /// Codifies the rules for daylight saving shifts in a given area.
    /// </summary>

    [Serializable]
    public class DaylightShiftRules
    {

        /// <summary>
        /// Refers to a shift that happens in specific days if <see langword="true"/>. Otherwise uses the rules for nth instance of a weekday in a month.
        /// </summary>

        public bool IsAbsolute { get; set; }

        /// <summary>
        /// The absolute day in a year where DST starts.
        /// Otherwise, the "day" represents the weekday in modulo 7 and the ordinal position of the weekday by the quotient of the division by 7.
        /// </summary>

        public DayInYear Starts { get; set; }

        /// <summary>
        /// The absolute day in a year where DST ends. 
        /// Otherwise, the "day" represents the weekday in modulo 7 and the ordinal position of the weekday by the quotient of the division by 7.
        /// </summary>

        public DayInYear Ends { get; set; }

        /// <summary>
        /// Attempts to parse the rules for daylight saving time switching; follows the format: from [] to [] 
        /// </summary>
        /// <param name="input">A stringified expression of time zone rules</param>
        /// <param name="languageConfig">The language configuration required to parse specific time data</param>
        /// <param name="feedback">The result of the operation expressed in a human-readable way</param>
        /// <param name="rules">The result of the parsing operation</param>
        /// <returns><see langword="true"/> if the parsing operation is successful, otherwise <see langword="false"/>.</returns>

        public static bool TryParse(string input, LanguageConfiguration languageConfig, out string feedback, out DaylightShiftRules rules)
        {
            input = input.ToLower();

            foreach (KeyValuePair<string, DaylightShiftRules> kvp in PresetRules)
            {
                if (kvp.Key.ToLower() == input)
                {
                    feedback = $"Found preset \"{kvp.Key}\": {kvp.Value}";
                    rules = kvp.Value;
                    return true;
                }
            }

            string[] segments = Regex.Replace(input, @"(from[\s\p{P}]+)|(the[\s\p{P}]+)", "", RegexOptions.IgnoreCase)
                .Split(" to ", StringSplitOptions.TrimEntries);
            rules = new DaylightShiftRules();

            if (segments.Length != 2)
            {
                feedback = $"Invalid number of parameters. Please specify two terms separated by \"to\". Alternatively, use any of these preset values: {string.Join(", ", PresetRules.Keys)}.";
                return false;
            }

            DayInYear ends;
            string error1R;
            if (DayInYear.TryParse(segments[0], true, languageConfig, out DayInYear starts, out string error1A))
            {
                if (DayInYear.TryParse(segments[1], true, languageConfig, out ends, out string error2A))
                {
                    rules.IsAbsolute = true;
                    rules.Starts = starts;
                    rules.Ends = ends;

                    feedback = $"Daylight saving time will be in effect {rules}.";
                    return true;
                }
                else
                {
                    feedback = $"Failed to parse ending day in absolute mode; received \"{segments[1]}\". {error2A}.";
                    return false;
                }
            }
            else if (DayInYear.TryParse(segments[0], false, languageConfig, out starts, out error1R))
            {
                if (DayInYear.TryParse(segments[1], false, languageConfig, out ends, out string error2R))
                {
                    rules.IsAbsolute = false;
                    rules.Starts = starts;
                    rules.Ends = ends;

                    feedback = $"Daylight saving time will be in effect {rules}.";
                    return true;
                }
                else
                {
                    feedback = $"Failed to parse ending day in relative mode; received \"{segments[1]}\". {error2R}.";
                    return false;
                }
            }
            feedback = $"Unable to interpret \"{segments[0]}\" as a valid day; relative parsing error: {error1R}. Absolute parsing error: {error1A}.";
            return false;
        }

        /// <summary>
        /// Expresses the value of this object in a human-readable manner.
        /// </summary>
        /// <returns>A string detailing the meaning of the values of this object.</returns>

        public override string ToString()
        {
            if (Starts is null || Ends is null) return $"Undefined";
            return $"From the {Starts.ToString(IsAbsolute)} to the {Ends.ToString(IsAbsolute)}.";
        }

        /// <summary>
        /// Checks whether a certain day is supposed to be interpreted using Daylight Saving time for a given user.
        /// </summary>
        /// <param name="day">The Date Time containing the day to check for.</param>
        /// <returns><see langword="true"/> if the given <paramref name="day"/> is comprised in the DST range for this user, otherwise <see langword="false"/>.</returns>

        public bool IsDST(DateTimeOffset day)
        {
            return IsDST(DayInYear.FromDateTime(day), day.Year);
        }

        /// <summary>
        /// Checks whether a certain day is supposed to be interpreted using Daylight Saving time for a given user.
        /// </summary>
        /// <param name="day">The day of the year to check for.</param>
        /// <param name="year">The year to consider this measure for, used for weekday calculations.</param>
        /// <returns><see langword="true"/> if the given <paramref name="day"/> is comprised in the DST range for this user, otherwise <see langword="false"/>.</returns>

        public bool IsDST(DayInYear day, int year)
        {
            int monthmin = (int)Starts.Month;
            int monthmax = (int)Ends.Month;

            int month = (int)day.Month;

            if (monthmax < monthmin)
            {
                monthmax += 12;
                month += month < monthmin ? 12 : 0;
            }

            if (month > monthmin && month < monthmax)
            {
                return true;
            }
            else if (month == monthmin)
            {
                if (IsAbsolute)
                    return day.Day > Starts.Day;

                return day.Day > Starts.GetThresholdDay(year);
            }
            else if (month == monthmax)
            {
                if (IsAbsolute)
                    return day.Day <= Ends.Day;

                return day.Day <= Ends.GetThresholdDay(year);
            }

            return false;

        }

        private static readonly Dictionary<string, DaylightShiftRules> PresetRules = new()
        {
            { "American", new() { IsAbsolute = false, Starts = new() { Day = (byte)LanguageHelper.Weekday.Sunday + 1 * 7, Month = LanguageHelper.Month.March }, Ends = new() { Day = (sbyte)LanguageHelper.Weekday.Sunday, Month = LanguageHelper.Month.November } } },
            { "European", new() { IsAbsolute = false, Starts = new() { Day = (byte)LanguageHelper.Weekday.Sunday - 7, Month = LanguageHelper.Month.March }, Ends = new() { Day = (sbyte)LanguageHelper.Weekday.Sunday - 7, Month = LanguageHelper.Month.October } } },
            { "Australian", new() { IsAbsolute = false, Starts = new() { Day = (sbyte)LanguageHelper.Weekday.Sunday, Month = LanguageHelper.Month.October }, Ends = new() { Day = (sbyte)LanguageHelper.Weekday.Sunday, Month = LanguageHelper.Month.April } } },
        };
    }

    /// <summary>
    /// Codified the per-user preferences for a given user profile.
    /// </summary>

    [Serializable]
    public class ProfilePreferences
    {

        /// <summary>
        /// Codifies the access level for general users to this user's profile.
        /// </summary>

        public enum PrivacyMode : byte
        {
            /// <summary>
            /// Anyone can view this profile
            /// </summary>
            Public,
            /// <summary>
            /// Only friends of this user can view this profile
            /// </summary>
            Friends,
            /// <summary>
            /// Only the user can view this profile
            /// </summary>
            Private
        }

        /// <summary>
        /// Indicates which users have permission to view this profile.
        /// </summary>

        public PrivacyMode Privacy { get; set; } = PrivacyMode.Friends;

        /// <summary>
        /// Indicates whether the user should get the borkday role added when it's their birthday
        /// </summary>

        public bool GiveBorkdayRole { get; set; } = true;

        /// <summary>
        /// Indicates whether to block friend requests directed to this user in general.
        /// </summary>

        public bool BlockRequests { get; set; } = false;

    }
}
