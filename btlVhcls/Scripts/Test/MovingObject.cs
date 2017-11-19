using UnityEngine;

public class MovingObject : MonoBehaviour
{
    [Header("Обычные настройки")]
    public float speed = 5.0f;
    public Vector3 direction = Vector3.forward;

    [Header("Рандомайзер")]
    public bool randomDirectionEnabled;
    public bool verticalDirectionEnabled;
    public float rotationSpeed = 10.0f;
    public float changeDirectionSeconds = 5.0f;
    public float changeRotationSeconds = 2.5f;

    private Quaternion targetRotation;

    void Start()
    {
        targetRotation = transform.localRotation;

        this.InvokeRepeating(ChangeDirection, 0, changeDirectionSeconds);
        this.InvokeRepeating(ChangeRotation, 0, changeRotationSeconds);
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.Self);
        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void ChangeDirection()
    {
        if (!randomDirectionEnabled)
            return;

        direction = Random.onUnitSphere;

        if (!verticalDirectionEnabled)
            direction = new Vector3(direction.x, 0, direction.z).normalized;
    }

    public void ChangeRotation()
    {
        if (!randomDirectionEnabled)
            return;

        targetRotation = Quaternion.LookRotation(Random.onUnitSphere);
    }
}
