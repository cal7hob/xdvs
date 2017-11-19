using System;
using System.Text.RegularExpressions;

public static class EnumExtensions
{
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

    public static bool IsGame(this Game source, Game requiredMode)
    {
        int currGame = (int)source;
        int reqGame = (int)requiredMode;
        return (currGame & reqGame) != 0;
    }
}
