using UnityEngine;

public class PlayerDebugController : MonoBehaviour
{
    public float moveForceAmount = 50.0f;
    public float torqueForceAmount = 0.25f;
    public float jumpForceAmount = 500.0f;
    private new Rigidbody rigidbody;

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

	void FixedUpdate()
	{
	    Vector3 direction = Vector3.zero;
        Vector3 torqueAxis = Vector3.zero;

		if (Input.GetKey(KeyCode.W))
            direction += Vector3.forward;

        if (Input.GetKey(KeyCode.S))
            direction += Vector3.back;

        if (Input.GetKey(KeyCode.A))
            direction += Vector3.left;

        if (Input.GetKey(KeyCode.D))
            direction += Vector3.right;

        if (Input.GetKey(KeyCode.Q))
            torqueAxis = Vector3.down;

        if (Input.GetKey(KeyCode.E))
            torqueAxis = Vector3.up;

        rigidbody.AddRelativeForce(direction * moveForceAmount);

        if (torqueAxis != Vector3.zero)
            rigidbody.AddTorque(torqueAxis * torqueForceAmount);
        else
            rigidbody.AddTorque(-rigidbody.angularVelocity);

        if (Input.GetKeyDown(KeyCode.Space))
            rigidbody.AddForce(Vector3.up * jumpForceAmount, ForceMode.Impulse);
    }
}
