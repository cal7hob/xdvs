using System.Collections;
using DemetriTools.Optimizations;
using UnityEngine;

public class VehiclePhysicsStabilizer : MonoBehaviour
{
    private enum StabilizationStatus
    {
        Off,
        InAirStabilization,
        GroundStabilization
    }

    private const float FALLING_COS_THRESHOLD = 0.766f; // 40 degrees
    private const float STABILIZATION_LOW_COS_THRESHOLD = 0.258f; // 65 degrees
    private const float STABILIZATION_HIGH_COS_THRESHOLD = 0.996f; // 5 degrees

    public float pendulumThreshold = 0.4f;

    private Rigidbody rb;
    private bool delayed;
    private IEnumerator delayedCoroutine;
    private VehicleController vehicle;
    private StabilizationStatus upStabStatus = StabilizationStatus.Off;
    private RepeatingOptimizer groundCheckFirstOptimizer = new RepeatingOptimizer(0.1f);
    private RepeatingOptimizer groundCheckSecondOptimizer = new RepeatingOptimizer(0.2f);
    private int checkgroundMatchCount = 0;

    private Quaternion normalRotation;

    void OnPhotonInstantiate()
    {
        vehicle = GetComponent<VehicleController>();
        vehicle.OnAvailabilityChanged += OnAvailabilityChanged;
        rb = GetComponent<Rigidbody>();
        groundCheckFirstOptimizer.Reset((float)MiscTools.random.NextDouble() * 0.3f);
        groundCheckSecondOptimizer.Reset((float)MiscTools.random.NextDouble() * 0.3f);
    }

    void FixedUpdate()
    {
        if (!vehicle.PhotonView.isMine || delayed)
            return;

        if (!CheckFallingOff())
            CheckGround();
        if (!HeadUpStabilization())
            CheckPendulum();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!vehicle.PhotonView.isMine || delayed)
            return;

        Vector3 hitImpulse = collision.impulse;
        if (Vector3.Dot(transform.up, hitImpulse.normalized) > 0.9f)
            MiscTools.ScaleLocalRotation(rb, new Vector3(0f, 1f, 0f));
    }

    void OnDestroy()
    {
        vehicle.OnAvailabilityChanged -= OnAvailabilityChanged;
    }

    private bool CheckFallingOff() //Подтолкнуть с обрыва, если надо
    {
        float cos = Vector3.Dot(transform.up, Vector3.up);
        if (upStabStatus != StabilizationStatus.Off || !vehicle.OnGround || cos > FALLING_COS_THRESHOLD)
            return false;

        float pushingSign = -Mathf.Sign(cos);
        rb.AddForce(transform.forward * 10f * pushingSign, ForceMode.VelocityChange);
        return true;
    }

    private bool HeadUpStabilization()
    {
        if (vehicle.OnGround)
        {
            if (upStabStatus != StabilizationStatus.GroundStabilization &&
                Vector3.Dot(Vector3.up, transform.up) < STABILIZATION_LOW_COS_THRESHOLD)
            {
                upStabStatus = StabilizationStatus.GroundStabilization;
                Vector3 normalForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
                normalRotation = Quaternion.LookRotation(normalForward, Vector3.up);
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }

            if (upStabStatus == StabilizationStatus.InAirStabilization)
            {
                rb.constraints = RigidbodyConstraints.None;
                upStabStatus = StabilizationStatus.Off;
                return false;
            }

            if (upStabStatus == StabilizationStatus.Off)
                return false;
        }

        if (upStabStatus == StabilizationStatus.GroundStabilization &&
            Vector3.Dot(Vector3.up, transform.up) > STABILIZATION_HIGH_COS_THRESHOLD)
        {
            rb.constraints = RigidbodyConstraints.None;
            upStabStatus = StabilizationStatus.Off;
            return false;
        }

        if (upStabStatus == StabilizationStatus.Off)
        {
            upStabStatus = StabilizationStatus.InAirStabilization;
            Vector3 normalForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            normalRotation = Quaternion.LookRotation(normalForward, Vector3.up);
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        transform.rotation = rb.rotation = Quaternion.RotateTowards(transform.rotation, normalRotation, 180f * Time.fixedDeltaTime);
        return true;
    }

    private void CheckPendulum()
    {
        Vector3 localAV = transform.InverseTransformDirection(rb.angularVelocity);
        if (Mathf.Abs(localAV.x) + Mathf.Abs(localAV.z) < pendulumThreshold)
            return;

        localAV.y = 0;
        rb.angularVelocity = transform.TransformDirection(localAV);
    }

    private void CheckGround()
    {
        if (!groundCheckFirstOptimizer.AskPermission())
            return;

        if (vehicle.Rb.velocity.y < -1f)
        {
            checkgroundMatchCount = 0;
            return;
        }

        Bounds vehicleBounds = vehicle.EntireBounds;
        float distance = vehicleBounds.extents.y + 0.5f;

        if (checkgroundMatchCount > 0 && !groundCheckSecondOptimizer.AskPermission())
            return;

        if (Physics.Raycast(vehicleBounds.center, Vector3.down, distance, BattleController.ObstacleMask))
        {
            checkgroundMatchCount = 0;
            return;
        }

        if (checkgroundMatchCount == 0)
        {
            groundCheckSecondOptimizer.Reset((float)MiscTools.random.NextDouble() * 0.1f);
            checkgroundMatchCount++;
            return;
        }

        Vector3 pushDirection = Vector3.zero;
        if (!CheckSideCollide(vehicleBounds, vehicle.transform.right, ref pushDirection)
            && !CheckSideCollide(vehicleBounds, -vehicle.transform.right, ref pushDirection)
            && !CheckSideCollide(vehicleBounds, vehicle.transform.forward, ref pushDirection)
            && !CheckSideCollide(vehicleBounds, -vehicle.transform.forward, ref pushDirection)
            )
            return;

        vehicle.Rb.AddForce(pushDirection * 15f, ForceMode.VelocityChange);
        checkgroundMatchCount = 0;
    }

    private bool CheckSideCollide(Bounds bounds, Vector3 direction, ref Vector3 invertDirection)
    {
        direction.y = 0;
        direction.Normalize();
        Vector3 origin = bounds.center;
        float halfHeight = bounds.extents.y - 0.1f;
        Vector3 endPointOnBounds = bounds.ClosestPoint(origin + direction * 10f); // 10f - макс. полуразмер Bounds юнита с запасом
        if (Physics.CapsuleCast(origin + Vector3.up * halfHeight, origin + Vector3.down * halfHeight, 0.01f, direction,
            Vector3.Distance(endPointOnBounds, origin), BattleController.ObstacleMask))
        {
            invertDirection = -direction;
            return true;
        }

        return false;
    }

    private IEnumerator ChangeAvailability(bool state)
    {
        if (state)
            yield return new WaitForSeconds(0.3f); //Включать не сразу после респауна

        delayed = !state;
    }

    private void OnAvailabilityChanged(bool state)
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (delayedCoroutine != null)
            StopCoroutine(delayedCoroutine);

        delayedCoroutine = ChangeAvailability(state);
        StartCoroutine(delayedCoroutine);
    }
}
