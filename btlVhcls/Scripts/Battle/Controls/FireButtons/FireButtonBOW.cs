using System.Collections;
using UnityEngine;

public class FireButtonBOW : FireButtonBase
{
    [Header("Настройки для BOW")]

    [Header("Цвета")]
    public Color normalColor;       // Цвет нормального ненажатого состояния.
    public Color reloadingColor;    // Цвет при перезарядке, кнопка не нажата.
    public Color inactiveColor;     // Для самолетов – когда не можешь стрелять.

    private const float BLINKING_FREQUENCY = 0.4f;

    private IEnumerator blinkingRoutine;

    protected override void Awake()
    {
        base.Awake();

        Dispatcher.Subscribe(EventId.SACLOSAimed, OnSACLOSAimed);

        SetColor(inactiveColor);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.SACLOSAimed, OnSACLOSAimed);
    }

    protected override void FireButton_Down() { }

    protected override void FireButton_Up() { }

    private void OnSACLOSAimed(EventId id, EventInfo ei)
    {
        EventInfo_IB info = (EventInfo_IB)ei;

        int playerId = info.int1;
        bool aimed = info.bool1;

        VehicleController vehicleController;

        if (!BattleController.allVehicles.TryGetValue(playerId, out vehicleController) || !vehicleController.IsMain)
            return;

        if (blinkingRoutine != null)
        {
            CoroutineHelper.Stop(blinkingRoutine);
            SetColor(inactiveColor);
        }

        if (!aimed)
            return;

        blinkingRoutine = Blinking();

        CoroutineHelper.Start(blinkingRoutine);
    }

    private IEnumerator Blinking()
    {
        while (true)
        {
            SetColor(inactiveColor);
            yield return new WaitForSeconds(BLINKING_FREQUENCY);

            SetColor(normalColor);
            yield return new WaitForSeconds(BLINKING_FREQUENCY);
        }
    }

    private void SetColor(Color color)
    {
        sprFireButton.color = color;
    }
}
