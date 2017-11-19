using UnityEngine;

[RequireComponent(typeof(Light))]
public class PulsingRangeLight : MonoBehaviour
{
    public float minRange;
    public float maxRange;
    public float speed = 1.0f;

    private new Light light;

    void Awake()
    {
        light = GetComponent<Light>();
    }

    void Update()
    {
        light.range = minRange + Mathf.PingPong(speed * Time.time, maxRange - minRange);
    }
}
