using UnityEngine;

public class IndestructibleObject : MonoBehaviour
{
    private static bool isInstantiated;

    void Awake()
    {
        if (isInstantiated)
        {
            Destroy(gameObject);
            return;
        }

        isInstantiated = true;

        DontDestroyOnLoad(gameObject);
    }
}
