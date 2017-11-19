using UnityEngine;

public class DamageableDummy : MonoBehaviour, IDamageable
{
    private new Collider collider;

    public int Health { get { return 1; } }

    public void TakeDamage(int damage, IDamageInflicter damageInflicter, Vector3 position){ }

    public bool Solid { get { return true; } }

    public Bounds Bounds { get { return collider.bounds; } }

    void Awake()
    {
        collider = GetComponentInChildren<Collider>();
    }
}
