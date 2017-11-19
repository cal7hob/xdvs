using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SoldierController
{
    /*
    private void OnScreenTap(EventId eid, EventInfo ei)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        EventInfo_V2 info = (EventInfo_V2)ei;

        if (!IsMine)//эта проверка производится только для своего танка
        {
            return;
        }
        Vector3 pos = Vector3.zero;
        if (!FromScreenToWorldCoord(info.vector, ref pos))
        {
            return;
        }

        RaycastHit hit;
        if (Physics.SphereCast(BattleCamera.Instance.transform.position, 0.2f, pos - BattleCamera.Instance.transform.position, out hit, 300, hitMask))
        {
            VehicleController tapTarget = hit.transform.GetComponentInParent<VehicleController>();
            if (tapTarget != null)
            {
                if (aimingController.Target == tapTarget)//если тапнули по противнику в которого целились
                {
                    turretController.SetFullAutoAiming();
                    return;
                }
            }
        }

        turretController.ResetAimingState();
#endif
    }
	
    private bool FromScreenToWorldCoord(Vector2 flatPos, ref Vector3 pos)
    {
        Ray ray = Camera.main.ScreenPointToRay(flatPos);
        Plane plane = new Plane(Vector3.up, transform.position);
        float distance = 0; // this will return the distance from the camera
        if (plane.Raycast(ray, out distance))
        {
            pos = ray.GetPoint(distance); // get the point
            return true;
        }
        return false;
    }


    private void Subscrube_()
    {
        //Dispatcher.Subscribe(EventId.OnScreenTap, OnScreenTap);
    }
    private void Unsubscribe_()
    {
       // Dispatcher.Unsubscribe(EventId.OnScreenTap, OnScreenTap);
    }*/
}
