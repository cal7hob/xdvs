using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Pool;
using VFX;
using Hashtable = ExitGames.Client.Photon.Hashtable;


public class Landmine : BattleItem
{
    [Header("Объекты, которые мы считаем припятствием для взрыва")]
    [SerializeField]
    private LayerMask vehCheckMask;

    [SerializeField, AssetPathGetter] private string explosionEffectPath;
    [SerializeField] private AudioClip explosionSound;

    private bool activated = false;
    private bool exploded = false;
    private readonly List<Vector3> victimPoints = new List<Vector3>();
    private SphereCollider sphereCollider;

	protected override void OnPhotonInstantiate(PhotonMessageInfo info)
	{
        base.OnPhotonInstantiate(info);
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
        {
            AudioDispatcher.PlayClipAtPosition(explosionSound, transform.position);
            GetExplosionEffect();
        }
    }

    private void ApplyParameters()
    {
        sphereCollider.radius = consumableInfo.activationRadius;
        if (photonView.isMine)
        {
            Invoke("Activation", consumableInfo.activationTime);
        }
    }

    private void GetExplosionEffect()
    {
        var effect = PoolManager.GetObject<Effect>(explosionEffectPath);
        effect.SetOrientation(transform.position, Quaternion.identity);
    }

    private void CheckForExplode(Collider other)
    {
        var victim = other.GetComponentInParent<VehicleController>();

        if (!photonView.isMine || exploded || !activated || victim == null || !victim.IsAvailable) //Активируется любым Vehicle'ом только у хозяина мины
        {
            return;
        }
        
        Debug.Log(victim == null? "victim is null": victim.name);

        exploded = true;

        AudioDispatcher.PlayClipAtPosition(explosionSound, transform.position);
        GetExplosionEffect();

        Collider[] vehColliders = Physics.OverlapSphere(transform.position, consumableInfo.radius,
            BattleController.CommonVehicleMask);
        HashSet<VehicleController> victims = new HashSet<VehicleController>();

        foreach (Collider vehCollider in vehColliders)
        {
            VehicleController vehicle = vehCollider.GetComponentInParent<VehicleController>();

            if (vehicle == null || !victims.Add(vehicle) || !CheckVictimAvailability(vehicle))
            {
                continue;
            }
           // Debug.Log("радиус "+ consumableInfo.radius + "   after check " + vehicle.name);
            int playerId = vehicle.data.playerId;
            int power = (int)(SelectPowerContextVeh(vehicle).GetParameterForCalc(consumableInfo.powerParameter) * consumableInfo.powerValue);
            Dispatcher.Send(
                EventId.TankTakesDamage,
                new EventInfo_U(
                    playerId,
                    power,
                    owner.data.playerId,
                    ShellType.Landmine,
                    Vector3.zero),

                 //vehicle.IsBot ? Dispatcher.EventTargetType.ToMaster : Dispatcher.EventTargetType.ToSpecific,
                 Dispatcher.EventTargetType.ToAll,
                playerId);
        }
        PhotonNetwork.Destroy(gameObject);
    }
    
	private void PutCorrectly()
	{
	    RaycastHit groundHit;
        if (!Physics.Raycast(transform.position + Vector3.up * 10f, Vector3.down, out groundHit, 20f, BattleController.TerrainLayerMask))
        {
            return;
        }

        float heightFromPivot = transform.position.y - GetBottomY();
        transform.position = groundHit.point + groundHit.normal * heightFromPivot;
        transform.up = groundHit.normal;
	}

    private void Activation()
    {
        activated = true;
    }

    private bool CheckVictimAvailability(VehicleController victim)
    {
        victim.BoundPointsToList(victimPoints);
       // Debug.Log(victim.name + " points: " + victimPoints.Count);
        float sqrRadius =  consumableInfo.radius * consumableInfo.radius;

        for (int i = 0; i < victimPoints.Count; i++)
        {
             if (Vector3.Distance(victimPoints[i], transform.position) > sqrRadius)
            {
               // Debug.Log(victim.name + " dist = " + Vector3.Distance(victimPoints[i], transform.position) + " rad2 =" + sqrRadius);
                continue;
            }
            
            RaycastHit hit;
            if (!Physics.Linecast(transform.position + Vector3.up*0.5f, victimPoints[i], out hit, vehCheckMask, QueryTriggerInteraction.Ignore))//пускаем луч игнорируя триггеры
            {
                return true; 
            }
           // Debug.Log(hit.collider.name);
            
        }
        
        return false;
    }

    private float GetBottomY()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float minY = transform.position.y;
        if (renderers != null && renderers.Length > 0)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer.bounds.min.y < minY)
                {
                    minY = renderer.bounds.min.y;
                }
            }
        }

        return minY;
    }
}
