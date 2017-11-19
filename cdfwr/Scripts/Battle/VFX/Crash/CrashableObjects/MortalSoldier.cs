using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class MortalSoldier : CrashableVehicleBase
{
    private SoldierController soldier;
    private Animator animator;
    private float beforeFadeTime = 3f;

    private Vector3 initLocalPosition;
    private Quaternion initLocalRotation;
    private Vector3 crashModelInitLocalPosition;
    private Quaternion crashModelInitLocalRotation;
    private Transform initialParent;
    private Vector3 destination;

    protected override void Awake() 
    {
        base.Awake();
        soldier = GetComponent<SoldierController>();
        Dispatcher.Subscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    protected override void OnTankKilled(EventId id, EventInfo ei)
    {
        int victimId = ((EventInfo_II)ei).int1;

        if (victimId == soldier.data.playerId)
        {
            if (fadeRoutine == null)
            {
                fadeRoutine = StartCoroutine(Fade());
            }
        }

        base.OnTankKilled(id, ei);
    }

    private void OnTankRespawned(EventId id, EventInfo ei)
    {
        if (((EventInfo_I)ei).int1 == soldier.data.playerId)
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }
            Restore();
        }
    }

    private void OnMainTankAppeared(EventId id, EventInfo ei)
    {
        Restore();
    }


    private Coroutine fadeRoutine = null;

    private IEnumerator Fade()
    {
        soldier.SetDeathAnim(true);
        yield return new WaitForSeconds(beforeFadeTime);
        soldier.IsAvailable = false;
        soldier.SetDeathAnim(false);
        fadeRoutine = null;
    }

    private void Restore()
    {
        //m_doAnimation = false;
        soldier.SetDeathAnim(false);
    }
}
