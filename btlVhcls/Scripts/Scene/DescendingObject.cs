using UnityEngine;

public class DescendingObject : MonoBehaviour
{
    public float hidingRatio = 0.7f;
    public float hidingSpeed = 3.0f;
    public Transform moveablePart;
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
        startPosition = moveablePart.position;
        bounds = moveablePart.GetComponent<Renderer>().bounds;
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

        destination = startPosition + (-moveablePart.up * bounds.size.y * hidingRatio);

        moveablePart.position = Vector3.MoveTowards(moveablePart.position, destination, hidingSpeed * Time.deltaTime);

        isHidden = moveablePart.position == destination;

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
        moveablePart.position = Vector3.MoveTowards(moveablePart.position, startPosition, hidingSpeed * Time.deltaTime);
        isHidden = moveablePart.position != startPosition;
    }

    private bool Check(Collider collider)
    {
        return collider.attachedRigidbody != null && !collider.transform.IsChildOf(transform.parent);
    }
}
