using UnityEngine;
using System.Collections;

public class Stabilizer : MonoBehaviour
{

    [SerializeField] private tk2dSprite sprStabilize;
    [SerializeField] private GameObject btnStabilize;
    private float initialSprStabilizeAlpha;
    private SpaceshipController myShip;

    void Awake()
    {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_WEBGL
        btnStabilize.SetActive(false);
#endif
        Dispatcher.Subscribe(EventId.MainTankAppeared, Init);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, Init);
    }


    void Init(EventId id, EventInfo info)
    {
        initialSprStabilizeAlpha = sprStabilize.color.a;
        myShip = (SpaceshipController) BattleController.MyVehicle;
    }

    private void OnStabilizerBtnDown()
    {
        myShip.StartCoroutine(myShip.Stabilization());
        var color = sprStabilize.color;
        color.a = 1;
        sprStabilize.color = color;
    }

    private void OnStabilizerBtnUp()
    {
        var color = sprStabilize.color;
        color.a = initialSprStabilizeAlpha;
        sprStabilize.color = color;
    }

    void FixedUpdate()
    {
        if (XDevs.Input.GetAxis("Stabilization") > 0)
        {
            OnStabilizerBtnDown();
        }
    }
}
