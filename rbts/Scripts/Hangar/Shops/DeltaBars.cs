using System;
using UnityEngine;

public class DeltaBars : MonoBehaviour
{
    [SerializeField]
    private UniAlignerBase deltaBarsAligner;
    [SerializeField]
    private tk2dSlicedSprite backgroundToStretch;
    [SerializeField]
    private GameObject deltaBarsWrapperForGUIPager;
    [SerializeField]
    private int itemHeight = 88;
    [SerializeField]
    private int verticalPadding = 36;

    private void Awake()
    {
        deltaBarsAligner.alignItemStateChanged += OnAlignItemStateChangedHandler;
    }

    private void OnDestroy()
    {
        deltaBarsAligner.alignItemStateChanged -= OnAlignItemStateChangedHandler;
    }

    private void OnAlignItemStateChangedHandler(StateEventSender stateEventSender, bool state)
    {
        if (!deltaBarsWrapperForGUIPager.GetActive())
            return;

        //Debug.LogErrorFormat("State changed: {0}, {1}", stateEventSender, state);

        var activeBars = 0;

        foreach (var item in deltaBarsAligner.GetItemsList())
        {
            if (!item.gameObject.GetActive())
                continue;

            activeBars++;
        }

        //Debug.LogErrorFormat("activeBars: {0}", activeBars);

        if (backgroundToStretch != null)
        {
            backgroundToStretch.dimensions =
                new Vector3(backgroundToStretch.dimensions.x, itemHeight * activeBars + 2 * verticalPadding);
        }
    }
}
