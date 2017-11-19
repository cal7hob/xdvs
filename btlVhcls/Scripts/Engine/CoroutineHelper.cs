using System.Collections;
using UnityEngine;

public class CoroutineHelper : MonoBehaviour
{
    private static CoroutineHelper instance;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public static Coroutine Start(IEnumerator coroutine)
    {
        return instance.StartCoroutine(coroutine);
    }

    public static void Stop(IEnumerator coroutine)
    {
        if (instance != null)
            instance.StopCoroutine(coroutine);
    }
}
