using UnityEngine;
using System.Collections;

public static class CoroutineExtensions
{
    public static Coroutine StartCoroutineEx(this MonoBehaviour monoBehaviour, IEnumerator routine, out CoroutineController coroutineController)
    {
        if (routine == null)
        {
            throw new System.ArgumentNullException("routine");
        }

        coroutineController = new CoroutineController(routine);
        return monoBehaviour.StartCoroutine(coroutineController.Start());
    }
}
