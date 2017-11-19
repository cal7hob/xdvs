using UnityEngine;
using System.Collections.Generic;

public class Landmine : BattleItem
{
    private static int vehCheckMask = 0;

    [SerializeField]
    private GameObject explosionEffect;

    [SerializeField]
    private AudioClip explosionSound;

    private bool activated;
    private bool exploded;
    //private readonly List<Vector3> victimPoints = new List<Vector3>();
    private SphereCollider sphereCollider;

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        if (vehCheckMask == 0)
            vehCheckMask = LayerMask.GetMask("Terrain", "Default");

        sphereCollider = GetComponentInChildren<SphereCollider>();
        ApplyParameters();
        
        PutCorrectly();
    }

    void OnTriggerEnter(Collider other)
    {
        CheckForExplode(other);
    }

    void OnTriggerStay(Collider other)
    {
        CheckForExplode(other);
    }

    void OnDestroy()
    {
        if (!photonView.isMine)
            PlayEffects();
    }

    private void ApplyParameters()
    {
        sphereCollider.radius = consumableInfo.activationRadius;
        if (photonView.isMine)
            this.Invoke(Activation, consumableInfo.activationTime);
    }

    private void CheckForExplode(Collider other)
    {
        if (!photonView.isMine || exploded || !activated || other.GetComponentInParent<VehicleController>() == null) //Активируется любым Vehicle'ом только у хозяина мины
            return;

        PlayEffects();

        Collider[] vehColliders = Physics.OverlapSphere(transform.position, consumableInfo.radius, BattleController.CommonVehicleMask);

        HashSet<VehicleController> victims = new HashSet<VehicleController>();

        foreach (Collider vehCollider in vehColliders)
        {
            VehicleController vehicle = vehCollider.GetComponentInParent<VehicleController>();

            if (vehicle == null || !victims.Add(vehicle) || !CheckVictimAvailability(vehicle))
                continue;

            int playerId = vehicle.data.playerId;
            int power = CalcPower(vehicle);

            Dispatcher.Send(
                id:         EventId.TankTakesDamage,
                info:       new EventInfo_U(
                                /* victimId */      playerId,
                                /* damage */        power,
                                /* attackerId */    owner.data.playerId,
                                /* shellType */     GunShellInfo.ShellType.Landmine,
                                /* hits */          1,
                                /* hitPosition */   Vector3.zero),
                target:     vehicle.IsBot ? Dispatcher.EventTargetType.ToMaster : Dispatcher.EventTargetType.ToSpecific,
                specificId: playerId);
        }

        exploded = true;

        PhotonNetwork.Destroy(gameObject);
    }
    
    private void PutCorrectly()
    {
        if (GameData.IsGame(Game.SpaceJet | Game.BattleOfWarplanes | Game.WingsOfWar | Game.BattleOfHelicopters))
            return;

        RaycastHit groundHit;

        if (!Physics.Raycast(
            /* origin:      */  transform.position + transform.up * 2.0f,
            /* direction:   */  Vector3.down,
            /* hitInfo:     */  out groundHit,
            /* maxDistance: */  20.0f,
            /* layerMask:   */  BattleController.TerrainLayerMask))
        {
            return;
        }

        transform.position = groundHit.point;
        transform.up = groundHit.normal;
    }

    private void Activation()
    {
        activated = true;
    }

    private bool CheckVictimAvailability(VehicleController victim)
    {
        //TODO Сделать нормальную проверку препятствий для взрыва
        /*victim.BoundPointsToList(victimPoints);
        for (int i = 0; i < victimPoints.Count; i++)
        {
            RaycastHit hit;
            if (!Physics.Linecast(transform.position, victimPoints[i], out hit, vehCheckMask, QueryTriggerInteraction.Ignore))
                return true;
        }

        return false;*/
        return true;
    }

    private void PlayEffects()
    {
        EffectPoolDispatcher.GetFromPool(explosionEffect, transform.position, Quaternion.identity);
        AudioDispatcher.PlayClipAtPosition(explosionSound, transform.position);
    }
}
