using System;
using UnityEngine;

public class KillAlert : MonoBehaviour
{

    public GameObject alert;
    private GameObject coordinates;
    private int count = 0;
    public float startYcoord = 30f;
    public float distanceBetween = 50f;
    private bool LastDamageTakenFromLandmine = false;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.TankTakesDamage, RememberLastDamageType, 2);
        Dispatcher.Subscribe(EventId.TankKilled, SendAlert);
        Dispatcher.Subscribe(EventId.OnKillAlertDestroy, OnKillAlertDestroy);
    }

    private void RememberLastDamageType(EventId id, EventInfo info)
    {
        EventInfo_U _info = (EventInfo_U)info;
        switch ((ShellType)(int)_info[3])
        {
            case ShellType.Landmine:
                LastDamageTakenFromLandmine = true;
                return;
            default:
                return;
        }
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.OnKillAlertDestroy, OnKillAlertDestroy);
        Dispatcher.Unsubscribe(EventId.TankTakesDamage, RememberLastDamageType);
        Dispatcher.Unsubscribe(EventId.TankKilled, SendAlert);
    }

    void OnKillAlertDestroy(EventId _id, EventInfo info)
    {
        if (count <= 0) return;
        count--;
    }

    void SendAlert(EventId _id, EventInfo _info)
    {
        var _Info = (EventInfo_II)_info;
        coordinates = Instantiate(alert, transform);
        if (LastDamageTakenFromLandmine)
        {
            LastDamageTakenFromLandmine = false;
            coordinates.GetComponentInChildren<tk2dSprite>().SetSprite("bonus_mine");
        }
        coordinates.transform.localPosition = new Vector3(0, -distanceBetween * count + startYcoord, 0);
        count++;
        tk2dTextMesh[] meshs = coordinates.GetComponentsInChildren<tk2dTextMesh>();
        foreach (var tmesh in meshs)
        {
            if (tmesh.name == "Killer")
            {
                tmesh.text = BattleController.GameStat[_Info.int2].playerName;
            }
            else if (tmesh.name == "Victim")
            {
                tmesh.text = BattleController.GameStat[_Info.int1].playerName;
            }
        }
    }

}
