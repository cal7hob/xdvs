using UnityEngine;

public class SpinningObject : MonoBehaviour
{
    public float speed;

    void Update()
    {
        transform.RotateAround(
            point:  transform.position,
            axis:   transform.up,
            angle:  speed * Time.deltaTime);
    }
}
