using System.Collections;
using UnityEngine;

public class DamageEffectScreen : MonoBehaviour
{
    public tk2dSlicedSprite sprDamageEffectScreen;
    public Color startColor;
    public Color minEndColor;
    public Color maxEndColor;
    public float fadingInSpeedRatio = 40.0f;
    public float minFadingOutSpeedRatio = 0.6f;
    public float maxFadingOutSpeedRatio = 10.0f;
    public float damageProgressDecreasingSpeed = 0.1f;

    private const float DISACTIVATION_TIME = 1.5f;

    private bool isFading;
    private float damageProgress;
    private float lastFadingTime;
    private Color endColor;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.TankTakesDamage, OnVehicleTakesDamage);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankTakesDamage, OnVehicleTakesDamage);
    }

    void Update()
    {
        if (damageProgress > 0)
            damageProgress -= damageProgressDecreasingSpeed * Time.deltaTime;

        if ((Time.time - lastFadingTime) > DISACTIVATION_TIME && sprDamageEffectScreen.gameObject.activeSelf)
            sprDamageEffectScreen.gameObject.SetActive(false);
    }

    private void OnVehicleTakesDamage(EventId id, EventInfo ei)
    {
        if (!sprDamageEffectScreen.gameObject.activeSelf)
            sprDamageEffectScreen.gameObject.SetActive(true);

        EventInfo_U info = (EventInfo_U)ei;

        if ((int)info[0] != BattleController.MyPlayerId)
            return;

        VehicleController attacker = BattleController.allVehicles[(int)info[2]];

        float maxDamage = attacker.data.attack;

        if (BattleController.MyVehicle.data.newbie)
        {
            maxDamage *= GameManager.NEWBIE_DAMAGE_RATIO;
        }

        float damageRatio = (int)info[1] / maxDamage;

        damageProgress += damageRatio;

        endColor
            = Color.Lerp(
                    a:  minEndColor,
                    b:  maxEndColor,
                    t:  damageRatio);

        if (!isFading)
            StartCoroutine(Fading());
    }

    private IEnumerator Fading()
    {
        isFading = true;

        float fadingProgress = 0;

        while (fadingProgress < 1)
        {
            fadingProgress += fadingInSpeedRatio * Time.deltaTime;

            sprDamageEffectScreen.color
                = Color.Lerp(
                    a:  startColor,
                    b:  endColor,
                    t:  fadingProgress);

            yield return null;
        }

        while (fadingProgress > 0)
        {
            float fadingOutSpeedRatio
                = Mathf.Lerp(
                    a:  maxFadingOutSpeedRatio,
                    b:  minFadingOutSpeedRatio,
                    t:  damageProgress);

            if (HelpTools.Approximately(fadingOutSpeedRatio, minFadingOutSpeedRatio))
                damageProgress = 0;

            fadingProgress -= fadingOutSpeedRatio * Time.deltaTime;

            sprDamageEffectScreen.color
                = Color.Lerp(
                    a:  startColor,
                    b:  endColor,
                    t:  fadingProgress);

            yield return null;
        }

        isFading = false;

        lastFadingTime = Time.time;
    }
}
