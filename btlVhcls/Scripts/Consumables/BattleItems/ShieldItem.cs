using UnityEngine;

public class ShieldItem : BattleItem
{
    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        // Показываем щит (если он есть) из префаба щита:
        transform.SetParent(owner.Body);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        // Показываем щит из префаба самой техники:
        owner.BodykitController.ShowShield(true, consumableInfo.duration);

        this.Invoke(Disappearing, consumableInfo.duration);

        Dispatcher.Subscribe(EventId.TankKilled, OnVehicleKilled);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankKilled, OnVehicleKilled);
    }

    private void OnVehicleKilled(EventId id, EventInfo ei)
    {
        int victimId = ((EventInfo_III)ei).int1;

        if (victimId == owner.data.playerId)
            owner.BodykitController.ShowShield(false);
    }

    private void Disappearing()
    {
        owner.BodykitController.ShowShield(false);
        Destroy(gameObject);
    }
}
