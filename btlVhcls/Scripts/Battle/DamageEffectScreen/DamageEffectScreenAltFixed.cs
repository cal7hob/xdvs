using UnityEngine;

public class DamageEffectScreenAltFixed : MonoBehaviour
{
    public tk2dSlicedSprite sprDamageEffectScreen;
    public Color startColor;
    public Color endColor;
    public float duration = 2.0f;

    private bool isFading;
    private float maxAccumulatedDamage;
    private float accumulatedDamageRatio;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.TankDamageApplied, OnVehicleTakesDamage);
        sprDamageEffectScreen.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankDamageApplied, OnVehicleTakesDamage);
    }

    void Update()
    {
        if (!isFading)
            return;

        if (accumulatedDamageRatio > 0)
        {
            accumulatedDamageRatio -= Time.deltaTime / duration;
        }
        else
        {
            if (isFading)
            {
                isFading = false;
                accumulatedDamageRatio = 0;

                sprDamageEffectScreen.gameObject.SetActive(false);

                return;
            }
        }

        sprDamageEffectScreen.color
                = Color.Lerp(
                    a:  startColor,
                    b:  endColor,
                    t:  accumulatedDamageRatio);
    }

    private void OnVehicleTakesDamage(EventId id, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;

        if ((int)info[0] != BattleController.MyPlayerId)
            return;

        if (!sprDamageEffectScreen.gameObject.activeSelf)
            sprDamageEffectScreen.gameObject.SetActive(true);

        isFading = true;

        float damage = (int)info[1];

        maxAccumulatedDamage = damage;

        accumulatedDamageRatio = damage / maxAccumulatedDamage;
    }
}
