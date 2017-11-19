using UnityEngine;

public class MaskAlign : MonoBehaviour {
    
    [SerializeField]
    private GameObject bottomMask;
    [SerializeField]
    private GameObject topMask;
    private int lastCamHeight;
    [SerializeField]
    private GameObject bottomAnchor;
    [SerializeField]
    private GameObject topAnchor;
    [SerializeField]
    private bool topAnchorMaxBounds = false;

    void Awake()
    {
        Messenger.Subscribe(EventId.ResolutionChanged, VerticalAlign);
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.ResolutionChanged, VerticalAlign);
    }

    void OnEnable()
    {
       Invoke("Exec", 0.02f);
    }

    void Exec()
    {
        VerticalAlign(EventId.ResolutionChanged, null);
    }
   
    public void VerticalAlign(EventId id, EventInfo info)
    {
        topMask.transform.position = new Vector3(topMask.transform.position.x, topAnchorMaxBounds ? topAnchor.GetComponent<Renderer>().bounds.max.y : topAnchor.GetComponent<Renderer>().bounds.min.y, topMask.transform.position.z);

        bottomMask.transform.position = new Vector3(bottomMask.transform.position.x, bottomAnchor.GetComponent<Renderer>().bounds.max.y, bottomMask.transform.position.z);
    }
   
}
