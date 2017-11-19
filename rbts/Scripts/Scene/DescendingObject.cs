using UnityEngine;

public class DescendingObject : MonoBehaviour
{
    public float hidingRatio = 0.7f;
    public float hidingSpeed = 3.0f;
    public GameObject particleEffectPrefab;
    public Transform particleEffectPoint;

    private bool isHidingStarted;
    private bool isHidden;
    private bool isEffectPlayed;
    private int collisionsCount;
    private Vector3 startPosition;
    private Vector3 destination;
    private Bounds bounds;

    private bool IsNeedHiding
    {
        get { return (collisionsCount > 0 || (isHidingStarted && !isHidden)); }
    }

    private bool IsNeedShowing
    {
        get { return collisionsCount <= 0 && isHidden; }
    }

    void Awake()
    {
        startPosition = transform.position;
        bounds = GetComponent<Renderer>().bounds;
    }

    void OnTriggerEnter(Collider collider)
    {
        if (!Check(collider))
            return;

        collisionsCount++;
    }

    void OnTriggerExit(Collider collider)
    {
        if (!Check(collider))
            return;

        collisionsCount--;
    }

    void Update()
    {
        if (IsNeedHiding)
            Hide();

        if (IsNeedShowing)
            Show();
    }

    private void Hide()
    {
        isHidingStarted = true;

        destination = startPosition + (-transform.up * bounds.size.y * hidingRatio);

        transform.position = Vector3.MoveTowards(transform.position, destination, hidingSpeed * Time.deltaTime);

        isHidden = transform.position == destination;

        if (isHidden)
        {
            isHidingStarted = false;

            if (!isEffectPlayed)
            {
                EffectPoolDispatcher.GetFromPool(
                    _effect:    particleEffectPrefab,
                    _position:  particleEffectPoint.position,
                    _rotation:  Quaternion.identity);

                isEffectPlayed = true;
            }
        }
        else
        {
            isEffectPlayed = false;
        }
    }

    private void Show()
    {
        transform.position = Vector3.MoveTowards(transform.position, startPosition, hidingSpeed * Time.deltaTime);
        isHidden = transform.position != startPosition;
    }

    private static bool Check(Collider collider)
    {
        return collider.attachedRigidbody != null;
    }
}
