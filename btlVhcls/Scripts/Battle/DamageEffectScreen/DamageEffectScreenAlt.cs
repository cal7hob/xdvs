using UnityEngine;

public class DamageEffectScreenAlt : MonoBehaviour
{
    public tk2dSlicedSprite sprDamageEffectScreen;
    public Color startColor;
    public Color endColor;
    public float maxDamageToArmorRatio;
    public float damageDecreasingSpeed;

    private bool isFading;
    private float maxAccumulatedDamage;
    private float accumulatedDamage;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.TankDamageApplied, OnVehicleTakesDamage);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankDamageApplied, OnVehicleTakesDamage);
    }

    void Update()
    {
        if (!isFading)
            return;

        if (accumulatedDamage > 0)
        {
            accumulatedDamage -= damageDecreasingSpeed * Time.deltaTime;
        }
        else
        {
            if (isFading)
            {
                isFading = false;
                accumulatedDamage = 0;

                sprDamageEffectScreen.gameObject.SetActive(false);

                return;
            }
        }

        sprDamageEffectScreen.color
                = Color.Lerp(
                    a:  startColor,
                    b:  endColor,
                    t:  accumulatedDamage / maxAccumulatedDamage);
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

        maxAccumulatedDamage = BattleController.MyVehicle.data.maxArmor * maxDamageToArmorRatio;

        accumulatedDamage += damage;
    }
}
