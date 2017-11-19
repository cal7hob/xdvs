using UnityEngine;
using System.Collections;

public class MaskAlign : MonoBehaviour
{

    [SerializeField]
    private GameObject bottomMask;
    [SerializeField]
    private GameObject topMask;
    private int lastCamHeight;
    [SerializeField]
    private GameObject bottomAnchor;
    [SerializeField]
    private GameObject topAnchor;

    private const float DELAY = 0.1f;
    
    void OnDisable()
    {
         Dispatcher.Unsubscribe(EventId.ResolutionChanged, VerticalAlign);
    }

    void OnEnable()
    {
        Dispatcher.Subscribe(EventId.ResolutionChanged, VerticalAlign);
        VerticalAlign(EventId.ResolutionChanged, null);
    }

    public void VerticalAlign(EventId id, EventInfo info)
    {
        Invoke("Align", DELAY);
    }

    void Align()
    {
        topMask.transform.position = new Vector3(topMask.transform.position.x, topAnchor.GetComponent<Renderer>().bounds.min.y, topMask.transform.position.z);

        bottomMask.transform.position = new Vector3(bottomMask.transform.position.x, bottomAnchor.GetComponent<Renderer>().bounds.max.y, bottomMask.transform.position.z);
    }

}
