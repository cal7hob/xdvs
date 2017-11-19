using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeDestroyTimer : MonoBehaviour
{
    public float lifeTime;
    private void Awake() 
    {
        StartCoroutine(Destroy());
    }
   
    IEnumerator Destroy()
    {
        yield return new WaitForSeconds(lifeTime);
        Destroy(gameObject);
    }
}