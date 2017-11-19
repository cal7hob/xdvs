using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class EnumExtensions
{
    public class TutorialsComparer : IEqualityComparer<Tutorials>
    {
        public bool Equals(Tutorials a, Tutorials b)
        {
            return a == b;
        }

        public int GetHashCode(Tutorials a)
        {
            return (int)a;
        }
    }

    /// <summary>
    /// Конвертация camelCase ключа Enum в строку "user friendly" вида.
    /// </summary>
    public static string ToFriendlyString(this Enum source)
    {
        return Regex.Replace(
            source.ToString(),
            @"(?<=[A-Z])(?=[A-Z][a-z]) | (?<=[^A-Z])(?=[A-Z]) | (?<=[A-Za-z])(?=[^A-Za-z])",
            " ",
            RegexOptions.IgnorePatternWhitespace);
    }
}
