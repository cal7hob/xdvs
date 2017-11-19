using System;

public class Clock
{
    public const int MINUTE_SECONDS = 60;
    public const int HOUR_SECONDS = 3600;
    public const int DAY_SECONDS = HOUR_SECONDS * 24;
    public const int MONTH_SECONDS = DAY_SECONDS * 30;
    public const int YEAR_SECONDS = DAY_SECONDS * 365;

    private static string daysTitle;
    private static string hoursTitle;
    private static string minutesTitle;
    private static string secondsTitle;

    public static string GetTimerString(long seconds, bool trimmedResult = false)
    {
        daysTitle = Localizer.GetText("TimeDays");
        hoursTitle = Localizer.GetText("TimeHours");
        minutesTitle = Localizer.GetText("TimeMinutes");
        secondsTitle = Localizer.GetText("TimeSeconds");

        string whitespace = trimmedResult ? " " : String.Empty;

        long days = seconds / DAY_SECONDS;
        seconds = seconds % DAY_SECONDS;

        long hours = seconds / HOUR_SECONDS;
        seconds = seconds % HOUR_SECONDS;

        long minutes = seconds / MINUTE_SECONDS;
        seconds = seconds % MINUTE_SECONDS;

        long firstValue, secondValue;
        string firstUnit, secondUnit;

        if (days > 0)
        {
            firstValue = days;
            firstUnit = daysTitle;
            secondValue = hours;
            secondUnit = hoursTitle;
        }
        else if (hours > 0)
        {
            firstValue = hours;
            firstUnit = hoursTitle;
            secondValue = minutes;
            secondUnit = minutesTitle;
        }
        else
        {
            firstValue = minutes;
            firstUnit = minutesTitle;
            secondValue = seconds;
            secondUnit = secondsTitle;
        }

        string firstUnitLabel = firstValue + whitespace + firstUnit;
        string secondUnitLabel = secondValue + whitespace + secondUnit;

        return (firstUnitLabel + " " + (secondValue == 0 && trimmedResult ? String.Empty : secondUnitLabel)).Trim();
    }
}
