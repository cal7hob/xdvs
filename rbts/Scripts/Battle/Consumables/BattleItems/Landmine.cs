using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Pool;
using Hashtable = ExitGames.Client.Photon.Hashtable;


// Условие нормальной работы: один меш внутри с нулевой локальной позицией относительно корня мины.

public class Landmine : BattleItem, IDamageInflicter
{
    private static int vehCheckMask = 0;

    [SerializeField] private FXInfo explosionFX;
    [SerializeField] private AudioClip explosionSound;

    private bool activated;
    private bool exploded;
    private SphereCollider sphereCollider;
    private Vector3 topPoint; // Верхняя точка мины

    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
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
            PoolManager.GetObject<PoolEffect>(explosionFX.GetResourcePath(true), transform.position, Quaternion.identity);
    }

    private void ApplyParameters()
    {
        sphereCollider.radius = consumableInfo.activationRadius;
        if (photonView.isMine)
            Invoke("Activation", consumableInfo.activationTime);
    }

    private void CheckForExplode(Collider other)
    {
        VehicleController activator;
        if (
            !photonView.isMine
            || exploded
            || !activated
            || (activator = other.GetComponentInParent<VehicleController>()) == null //Активируется только врагом только у клиента-хозяина мины
            || activator == owner
            || VehicleController.AreFriends(owner, activator)
            )
            return;

        Explode();
    }

    public void Explode()
    {
        PoolManager.GetObject<PoolEffect>(explosionFX.GetResourcePath(true), transform.position, Quaternion.identity);
        AudioDispatcher.PlayClipAtPosition(explosionSound, transform.position, Settings.SoundVolume * SoundSettings.EXPLOSION_VOLUME);

        int checkMask = LayerMask.GetMask("TankBumper", "Default");
        Collider[] vehColliders = Physics.OverlapSphere(transform.position, consumableInfo.radius,
            checkMask, QueryTriggerInteraction.Collide);

        HashSet<IDamageable> victims = new HashSet<IDamageable>();
        foreach (Collider vehCollider in vehColliders)
        {
            IDamageable victim = vehCollider.GetComponentInParent<IDamageable>();
            if (victim == null || !victims.Add(victim) || !CheckVictimAvailability(victim))
                continue;

            int power = Mathf.RoundToInt(victim.Health * consumableInfo.powerValue) + 1;
            victim.TakeDamage(power, this, Vector3.zero);
        }

        exploded = true;
        PhotonNetwork.Destroy(gameObject);
    }
    
    private void PutCorrectly()
    {
        RaycastHit groundHit;
        if (!Physics.Raycast(transform.position + transform.up * 10f, -transform.up, out groundHit, 20f, BattleController.TerrainLayerMask))
            return;

        Bounds meshBounds = GetComponent<MeshFilter>().sharedMesh.bounds;
        float minOffsetY = meshBounds.min.y;
        transform.position = groundHit.point - groundHit.normal * minOffsetY;
        transform.up = groundHit.normal;
        Vector3 normLocalPos = meshBounds.center;
        normLocalPos.y = meshBounds.max.y;
        topPoint = transform.TransformPoint(normLocalPos);
    }

    private void Activation()
    {
        activated = true;
    }

    private bool CheckVictimAvailability(IDamageable victim)
    {
        Vector3 closestPoint = victim.Bounds.ClosestPoint(topPoint);
        RaycastHit hit;
        return
            !Physics.Linecast(topPoint, closestPoint, out hit, vehCheckMask, QueryTriggerInteraction.Ignore)
            || hit.collider.GetComponentInParent<IDamageable>() == victim;
    }

    public bool IsLocal
    {
        get { return owner.PhotonView.isMine; }
    }

    public int OwnerId
    {
        get { return owner.data.playerId; }
    }

    public DamageSource DamageSource { get { return DamageSource.HenchmanDoes;} }
}
