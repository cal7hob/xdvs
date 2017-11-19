using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Pool;
using VFX;
using Vkontakte;
using Hashtable = ExitGames.Client.Photon.Hashtable;


public class Landmine : BattleItem
{
    [Header("Объекты, которые мы считаем припятствием для взрыва")]
    [SerializeField]
    private LayerMask vehCheckMask;

    [SerializeField, AssetPathGetter]
    private string explosionEffectPath;
    [SerializeField]
    private AudioClip explosionSound;
    [SerializeField]
    private float explosionFromAnotherMineDelay = 0.05f;

    private bool activated = false;
    private bool exploded = false;
    private readonly List<Vector3> victimPoints = new List<Vector3>();
    private SphereCollider sphereCollider;
    private PhotonView pView;

    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);
        sphereCollider = GetComponentInChildren<SphereCollider>();
        pView = GetComponent<PhotonView>();
        ApplyParameters();
        PutCorrectly();
    }

    void OnTriggerEnter(Collider other)
    {
        CheckVictimForExplode(other);
    }

    void OnTriggerStay(Collider other)
    {
        CheckVictimForExplode(other);
    }

    void Awake()
    {
        Dispatcher.Subscribe(EventId.DestroyThisGameObject, NetworkDestroy);

    }
    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.DestroyThisGameObject, NetworkDestroy);
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


    public IEnumerator ExplodeDelay()
    {
        yield return new WaitForSeconds(explosionFromAnotherMineDelay);
        ForceExplode();
        yield break;
    }

    public void ForceExplodeWithDelay()
    {
        StartCoroutine(ExplodeDelay());
    }

    public void ForceExplode()
    {
        if (!photonView.isMine || exploded || !activated) //Активируется любым Vehicle'ом только у хозяина мины
        {
            return;
        }
        RadiusExplode();
    }

    private void CheckVictimForExplode(Collider other)
    {
        var victim = other.GetComponentInParent<VehicleController>();

        if (!photonView.isMine || exploded || !activated || victim == null || !victim.IsAvailable) //Активируется любым Vehicle'ом только у хозяина мины
        {
            return;
        }

        RadiusExplode();
    }

    private void RadiusExplode()
    {
        exploded = true;
        AudioDispatcher.PlayClipAtPosition(explosionSound, transform.position);

        GetExplosionEffect();

        Collider[] vehColliders = Physics.OverlapSphere(transform.position, consumableInfo.radius,
            BattleController.CommonVehicleMask);
        HashSet<VehicleController> victims = new HashSet<VehicleController>();

        foreach (Collider vehCollider in vehColliders)
        {
            VehicleController vehicle = vehCollider.GetComponentInParent<VehicleController>();
            if (vehCollider.tag.Equals("Landmine") && vehCollider.gameObject != gameObject)
            {
                vehCollider.gameObject.GetComponent<Landmine>().ForceExplodeWithDelay();
            }
            if (vehicle == null || !victims.Add(vehicle) || !CheckVictimAvailability(vehicle))
            {
                continue;
            }
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

                 Dispatcher.EventTargetType.ToAll,
                playerId);
        }
        PhotonNetwork.Destroy(gameObject);


    }

    private void NetworkDestroy(EventId _id, EventInfo _info)
    {
        var info = (EventInfo_I)_info;
        if (pView.instantiationId == info.int1)
        {
            ForceExplode();
        }
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
        float sqrRadius = consumableInfo.radius * consumableInfo.radius;
        if (Vector3.Distance(victim.BodyCollider.transform.position, transform.position) > sqrRadius)
        {
            return false;
        }

        RaycastHit hit;
        if (!Physics.Linecast(transform.position + Vector3.up * 0.5f, victim.BodyCollider.transform.position, out hit, vehCheckMask, QueryTriggerInteraction.Ignore))//пускаем луч игнорируя триггеры
        {
            return true;
        }
        if (!Physics.Linecast(transform.position + Vector3.up * 0.5f, victim.CritCollider.transform.position, out hit, vehCheckMask, QueryTriggerInteraction.Ignore))//пускаем луч игнорируя триггеры
        {
            return true;
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
