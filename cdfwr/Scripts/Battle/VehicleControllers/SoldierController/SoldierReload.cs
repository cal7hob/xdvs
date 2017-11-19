using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SoldierController
{

    public void ForceReload()
    {
        PhotonView.RPC("Reload", PhotonTargets.All, data.playerId, (float)data.reloadTime);
    }

    [PunRPC]
    public void Reload(int playerId, float time)
    {
        //if (data.playerId == playerId)
        {
            turretController.Reload(time);
        }
    }

    public override void SetOnReloadParams(bool start)
    {
        SetReload(start);
        IsAiming = !start;
    }
}
