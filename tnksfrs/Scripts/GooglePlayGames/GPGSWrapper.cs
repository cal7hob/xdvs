using System;
using UnityEngine;

public static class GPGSWrapper
{
    private static int previousSilverAmountTotal;
    private static int previousGoldAmountTotal;

    /// <summary>
    /// Запись изменения количества денег у игрока.
    /// </summary>
    /// <param name="silverTotal">Всего серебра на данный момент.</param>
    /// <param name="goldTotal">Всего золота на данный момент.</param>
    public static void LogBalanceChange(int silverTotal, int goldTotal)
    {
#if UNITY_ANDROID
        if (!Social.localUser.authenticated)
        {
            if(Achievments.Instance)
            {
                Achievments.Instance.AuthenticateIfNeeded((bool success) => 
                {
                    if(success)
                        LogBalanceChangeInternal(silverTotal, goldTotal);
                    else
                        Debug.LogErrorFormat("Cant LogBalanceChange silver: {0}, gold: {1} because social authentication failed!", silverTotal, goldTotal);
                });
            }
        }
        else
            LogBalanceChangeInternal(silverTotal, goldTotal);
#endif
    }

    private static void LogBalanceChangeInternal(int silverTotal, int goldTotal)
    {
#if UNITY_ANDROID
        int silverDelta = silverTotal - previousSilverAmountTotal;
        int goldDelta = goldTotal - previousGoldAmountTotal;

        if (silverDelta != 0 && previousSilverAmountTotal != 0)
        {
            GooglePlayGames.PlayGamesPlatform.Instance.Events.IncrementEvent(
                stepsToIncrement: (uint)Mathf.Abs(silverDelta),
                eventId: ServiceSettings.Services[ServiceSettingsKeys.Service.GooglePlayGames][silverDelta > 0 ? ServiceSettingsKeys.Field.GettingSilverEventKey : ServiceSettingsKeys.Field.SpendingSilverEventKey]);
        }

        if (goldDelta != 0 && previousGoldAmountTotal != 0)
        {
            GooglePlayGames.PlayGamesPlatform.Instance.Events.IncrementEvent(
                stepsToIncrement: (uint)Mathf.Abs(goldDelta),
                eventId: ServiceSettings.Services[ServiceSettingsKeys.Service.GooglePlayGames][goldDelta > 0 ? ServiceSettingsKeys.Field.GettingGoldEventKey : ServiceSettingsKeys.Field.SpendingGoldEventKey]);

        }

        previousSilverAmountTotal = silverTotal;
        previousGoldAmountTotal = goldTotal;
#endif
    }
}