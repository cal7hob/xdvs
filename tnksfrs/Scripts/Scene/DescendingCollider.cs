using UnityEngine;
using XDevs.LiteralKeys;

public class DescendingCollider : MonoBehaviour
{
    public float shiftingRatio = 0.7f;
    public float shiftingSpeed = 0.3f;
    public GameObject particleEffect;

    private bool isHiding;
    private bool isHidden;
    private float startTime;
    private Vector3 startPosition;
    private Vector3 destination;
    private Bounds startBounds;

    void Awake()
    {
        startPosition = transform.position;
        startBounds = GetComponent<Renderer>().bounds;
    }

    private static bool CheckForPlayerCollision(Collider coll)
    {
        return coll.attachedRigidbody != null
            && coll.attachedRigidbody.gameObject.layer == LayerMask.NameToLayer(Layer.Items[Layer.Key.Player]);
    }

    void OnTriggerStay(Collider coll)
    {
        if (!CheckForPlayerCollision(coll)) return;

        isHiding = true;
    }

    void OnTriggerExit(Collider coll)
    {
        if (!CheckForPlayerCollision(coll)) return;

        isHiding = false;
    }

    void Update()
    {
        destination = new Vector3(
            startPosition.x,
            startBounds.min.y - (startBounds.size.y * shiftingRatio),
            startPosition.z);

        if (isHiding && !isHidden)
        {
            startTime = Mathf.Approximately(startTime, 0.0f) ? Time.time : startTime;

            transform.position = Vector3.Lerp(
                transform.position,
                destination,
                ((Time.time - startTime) * shiftingSpeed) / Mathf.Abs(destination.y - transform.position.y));

            isHidden = transform.position == destination;

            startTime = isHidden ? 0.0f : startTime;

            if (isHidden)
                EffectPoolDispatcher.GetFromPool(
                    particleEffect,
                    particleEffect.transform.position,
                    Quaternion.identity);
        }
        else if (!isHiding && isHidden)
        {
            startTime = Mathf.Approximately(startTime, 0.0f) ? Time.time : startTime;

            transform.position = Vector3.Lerp(
                transform.position,
                startPosition,
                ((Time.time - startTime) * shiftingSpeed) / Mathf.Abs(startPosition.y - transform.position.y));

            isHidden = transform.position != startPosition;

            startTime = !isHidden ? 0.0f : startTime;
        }
    }
}
