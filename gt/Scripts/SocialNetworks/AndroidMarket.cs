using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class AndroidMarket : MonoBehaviour {

    [SerializeField]
    private List<GameObject> amazonObjects = new List<GameObject>();
    [SerializeField]
    private List<GameObject> googlePlayObjects = new List<GameObject>();

	void Start () 
    {
        foreach (var amazonObject in amazonObjects)
	    {
	        amazonObject.SetActive(false);
	    }    
	}
}
