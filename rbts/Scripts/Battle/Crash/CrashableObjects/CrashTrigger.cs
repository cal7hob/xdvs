using UnityEngine;

public class CrashTrigger : MonoBehaviour, IDamageable
{
    public CrashableObject[] crashableObjects;

    private CrashableObject ownCrashableObject;
    private new Collider collider;

    void Awake()
    {
        ownCrashableObject = GetComponentInChildren<CrashableObject>();
        collider = GetComponentInChildren<Collider>();
    }

    void OnCollisionEnter()
    {
        CallCrash();
    }

    void OnTriggerEnter()
    {
        CallCrash();
    }


    public void TakeDamage(int damage, IDamageInflicter damageInflicter, Vector3 position)
    {
        CallCrash();
    }

    private void CallCrash()
    {
        if (ownCrashableObject)
            ownCrashableObject.Crash();

        foreach (CrashableObject crashableObject in crashableObjects)
        {
            if (crashableObject == null)
            {
                Debug.LogWarning("Missed reference! CrashableObjectBase not found.", gameObject);
                continue;
            }

            crashableObject.Crash();
        }
    }

    public bool Solid { get { return false; } }

    public Bounds Bounds { get { return collider.bounds; } }

    public int Health { get { return 0; } }  //Здесь будет другое значение для объектов со здоровьем
}
