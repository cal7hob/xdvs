using UnityEngine;
using System.Collections;

public class Module_BadInternetSign : InterfaceModuleBase
{
    [SerializeField] private TweenBase tweenScript;

    protected override void Awake ()
    {
        base.Awake();
        Dispatcher.Subscribe(EventId.HighPingAlarm, OnHighPingAlarm);
        Dispatcher.Subscribe(EventId.BattleEnd, OnBattleEnd);
        if (!tweenScript)
            tweenScript = GetComponent<TweenBase>();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.HighPingAlarm, OnHighPingAlarm);
        Dispatcher.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
    }

    private void OnBattleEnd(EventId id, EventInfo info)
    {
        SetActive(false);
    }

    private void OnHighPingAlarm(EventId id, EventInfo info)
    {
        if (BattleController.IsBattleFinished)
        {
            if(IsActive)
                SetActive(false);
            return;
        }
            
        //Debug.LogWarningFormat("Module_BadInternetSign.OnHighPingAlarm {0}", ((EventInfo_B)info).bool1);
        if (tweenScript)
            tweenScript.SetActiveAnimation(((EventInfo_B)info).bool1);
        SetActive(((EventInfo_B)info).bool1);//Чтобы выравнивалка сработала
        
    }
}
