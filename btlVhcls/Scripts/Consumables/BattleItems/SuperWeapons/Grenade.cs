using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Grenade : MonoBehaviour
{
    [Header("Ссылки")]
    public GameObject explosionEffect;
    public AudioClip[] explosionSounds;

    [Header("Остальное")]
    public float speed = 185.0f;

    private bool isExploded;
    private int hitMask;
    private float activationTime;
    private Vector3 aimPosition;
    private SphereCollider sphereCollider;
    private new Rigidbody rigidbody;
    private GrenadeLauncher launcher;

    private bool IsActivated
    {
        get { return Time.time > activationTime; }
    }

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        sphereCollider = GetComponentInChildren<SphereCollider>();
    }

    void OnTriggerEnter(Collider other)
    {
        TryExplode(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryExplode(other);
    }

    public void Init(GrenadeLauncher launcher, ConsumableInfo consumableInfo, Vector3 aimPosition)
    {
        gameObject.SetActive(false);

        this.aimPosition = aimPosition;
        this.launcher = launcher;

        sphereCollider.radius = consumableInfo.activationRadius;
        hitMask = MiscTools.ExcludeLayerFromMask(launcher.Owner.HitMask, launcher.Owner.OwnLayer);
    }

    public void Throw(Vector3 fromPosition, Quaternion rotation, float activationDuration)
    {
        transform.position = fromPosition;
        transform.rotation = rotation;

        gameObject.SetActive(true);

        float distanceToTarget = Vector3.Distance(transform.position, aimPosition);
        float timeToTarget = distanceToTarget / speed;
        Vector3 throwForce = CalculateThrowForce(transform.position, aimPosition, timeToTarget);

        rigidbody.AddForce(throwForce, ForceMode.VelocityChange);

        activationTime = Time.time + activationDuration;
    }

    /// <summary>
    /// http://answers.unity3d.com/answers/456066/view.html
    /// </summary>
    private Vector3 CalculateThrowForce(Vector3 origin, Vector3 target, float timeToTarget)
    {
        // calculate vectors
        Vector3 toTarget = target - origin;
        Vector3 toTargetXZ = toTarget;
        toTargetXZ.y = 0;

        // calculate xz and y
        float y = toTarget.y;
        float xz = toTargetXZ.magnitude;

        // calculate starting speeds for xz and y. Physics forumulase deltaX = v0 * t + 1/2 * a * t * t
        // where a is "-gravity" but only on the y plane, and a is 0 in xz plane.
        // so xz = v0xz * t => v0xz = xz / t
        // and y = v0y * t - 1/2 * gravity * t * t => v0y * t = y + 1/2 * gravity * t * t => v0y = y / t + 1/2 * gravity * t
        float t = timeToTarget;
        float v0y = y / t + 0.5f * Physics.gravity.magnitude * t;
        float v0xz = xz / t;

        // create result vector for calculated starting speeds
        Vector3 result = toTargetXZ.normalized;        // get direction of xz but with magnitude 1
        result *= v0xz;                                // set magnitude of xz to v0xz (starting speed in xz plane)
        result.y = v0y;                                // set y to v0y (starting speed of y plane)

        return result;
    }

    private void TryExplode(Collider other)
    {
        if (!CheckExplosion(other))
            return;

        if (launcher != null)
            launcher.SendDamage(other, this);

        Explode();
    }

    private void Explode()
    {
        isExploded = true;
        PlayEffects();
        Destroy(gameObject);
    }

    private bool CheckExplosion(Collider other)
    {
        if (!IsActivated || isExploded)
            return false;

        return MiscTools.CheckIfLayerInMask(hitMask, other.gameObject.layer);
    }

    private void PlayEffects()
    {
        EffectPoolDispatcher.GetFromPool(explosionEffect, transform.position, Quaternion.identity);
        AudioDispatcher.PlayClipAtPosition(explosionSounds.GetRandomItem(), transform.position);
    }
}
