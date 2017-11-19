using System;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;
using Http;
using System.Linq;

public class ForceBuy : MonoBehaviour
{
    Action<Http.Response, bool> finishCallback;

    [SerializeField]
    private int TankId = 1;
    [SerializeField]
    private int DecalId = 1;

    private const string RENT_PATHD = "/shop/buyDecal";
    private const string BODYKIT_FIELD_NAMED = "decalId";


    protected static void RequestBodykitRent(
       string rentPath,
       string bodykitFieldName,
       ObscuredInt userVehicleId,
       ObscuredInt bodykitId,
       Action<Http.Response, bool> finishCallback)
    {
        HangarController.Instance.isWaitingForSaving = true;
        Request request = Http.Manager.Instance().CreateRequest(rentPath);

        request.Form.AddField("tankId", (int)userVehicleId);
        request.Form.AddField(bodykitFieldName, bodykitId.ToString());

        Http.Manager.StartAsyncRequest(
            request: request,
            successCallback: delegate (Http.Response result)
            {
            },
            failCallback: delegate (Http.Response result)
            {
            });
    }
    private void Awake()
    {
        Dispatcher.Subscribe(EventId.AfterHangarInit, Handler);
    }
    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, Handler);
    }
    private void Handler(EventId _id, EventInfo _info)
    {
        if (!Shop.GetVehicle(TankId).Upgrades.OwnedDecals.Any(decal => decal.id == DecalId) || ProfileInfo.TutorialIndex == 0)
        {
            RequestBodykitRent(RENT_PATHD, BODYKIT_FIELD_NAMED, TankId, DecalId, finishCallback);
        }

    }
}
