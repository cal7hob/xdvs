using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SoldierController
{
    private Vector3 correctCamSinghtPoint;
    private Vector3 correctGunSinghtPoint;

    private Vector2 correctControls;
    public Vector2 correctAimingAngles;

    private int correctRotRate;

    private Quaternion zeroRot = Quaternion.Euler(Vector3.zero);

    //передаем данные для клонов
    public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.isWriting)
        {
            if (!IsAvailable)
            {
                return;
            }

            stream.SendNext(transform.position);
            stream.SendNext(Body.rotation);

			stream.SendNext(new Vector2(XAxisControl, YAxisControl));

            stream.SendNext(aimingAngles);

            stream.SendNext(camSightPoint);
            stream.SendNext(gunSightPoint);
            
        }
        else
        {
            MarkActivity();
           // int itemCount = stream.Count - stream.currentItem;

            correctPosition = (Vector3)stream.ReceiveNext();
            correctRotation = (Quaternion)stream.ReceiveNext();

            correctControls = (Vector2)stream.ReceiveNext();   

			correctAimingAngles = (Vector2)stream.ReceiveNext();


            correctCamSinghtPoint = (Vector3)stream.ReceiveNext();
            correctGunSinghtPoint = (Vector3)stream.ReceiveNext();

           
            if (settingSpawnPosition)
            {
                settingSpawnPosition = false;
            }

            syncTime = Mathf.Min(PhotonNetwork.GetPing() * 0.001f, MAX_SYNC_TIME);
            rotSyncSpeed = Quaternion.Angle(transform.rotation, correctRotation) / syncTime;
        }
    }

    public override void StoreCloneRotation()
    {
        Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(lastRotation);
        deltaRotation.ToAngleAxis(out deltaRotationMagnitude, out deltaRotationAxis);
        lastRotation = transform.rotation;
    }
}
