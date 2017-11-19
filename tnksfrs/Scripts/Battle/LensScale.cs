using UnityEngine;

public class LensScale : MonoBehaviour 
{
	public float nearSize = 0.4f;
	public float farSize = 0.085f;

    private const float MAX_DISTANCE = 70.0f;

    private LensFlare lens;
    private float distance;
    private float distanceRatio;
    private float size;


    void Awake()
    {
        lens = GetComponent<LensFlare>();
    }

	void Update() 	
	{
        //if (BattleCamera.Instance == null || BattleCamera.Instance.transform == null)
		//	return;
        //
		//distance = Vector3.Distance(BattleCamera.Instance.transform.position, transform.position);

        distanceRatio = distance / MAX_DISTANCE;
        size = Mathf.Lerp(nearSize, farSize, distanceRatio);

        lens.brightness = size;
	}
}
