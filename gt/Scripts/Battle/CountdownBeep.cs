using UnityEngine;
using System.Collections;

public class CountdownBeep : MonoBehaviour
{
    public AudioClip timerTickSound;

    private const float COUNTDOWN_REMAINING_TIME_MIN = 5.0f;
    private const float COUNTDOWN_REMAINING_TIME_MAX = 10.0f;
    private const float BEEP_FREQUENCY_MIN = 0.5f;
    private const float BEEP_FREQUENCY_MAX = 1.0f;

    private bool countdownStarted;
    private IEnumerator countdownSoundPlayingRoutine;

    void Update()
    {
        if (BattleController.TimeRemaining <= COUNTDOWN_REMAINING_TIME_MAX)
            PlayCountdownSound();
    }

    private void PlayCountdownSound()
    {
        if (countdownStarted)
            return;

        StopCountdownSound();

        countdownSoundPlayingRoutine = CountdownSoundPlaying();

        StartCoroutine(countdownSoundPlayingRoutine);
    }

    private void StopCountdownSound()
    {
        if (countdownSoundPlayingRoutine != null)
            StopCoroutine(countdownSoundPlayingRoutine);

        countdownStarted = false;
    }

    private IEnumerator CountdownSoundPlaying()
    {
        countdownStarted = true;

        while (BattleController.TimeRemaining > 0 && BattleController.TimeRemaining <= COUNTDOWN_REMAINING_TIME_MAX)
        {
            if (BattleController.MyVehicle == null)
            {
                countdownStarted = false;
                yield break;
            }

            AudioDispatcher.PlayClipAtPosition(timerTickSound, BattleController.MyVehicle.transform);

            yield return new WaitForSeconds(
                BattleController.TimeRemaining <= COUNTDOWN_REMAINING_TIME_MIN
                    ? BEEP_FREQUENCY_MIN
                    : BEEP_FREQUENCY_MAX);
        }

        countdownStarted = false;
    }
}
