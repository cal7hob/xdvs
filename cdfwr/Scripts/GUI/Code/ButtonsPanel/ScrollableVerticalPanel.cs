using System.Linq;
using UnityEngine;
using System.Collections;
using XDevs.ButtonsPanel;

public class ScrollableVerticalPanel : VerticalPanel {

    public tk2dUIScrollableArea scrollableArea;
    [SerializeField]
    private tk2dUILayout mainLayout;
    [SerializeField]
    private GameObject bottomMask;
    protected float contentLength;
    private int lastCamHeight;


    protected override void Start()
    {
        base.Start();
        mainLayout = gameObject.GetComponentInParent<tk2dUILayout>();
        VerticalAlign();
    }
    override public void Align()
    {
        if (!isActiveAndEnabled)
        {
            doAlignOnEnable = true;
            return;
        }
        if (buttons == null || buttons.Count == 0)
        {
            return;
        }

        SortButtons();
        switch (alignBy)
        {
            case AlignType.Top:
                AlignByTop();
                break;
            case AlignType.Center:
                AlignByCenter();
                break;
            case AlignType.Bottom:
                break;
        }
        if (scrollableArea != null) scrollableArea.ContentLength = contentLength;
    }

    private void SortButtons()
    {
        var rng = new System.Random();
        int n = buttons.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            var value = buttons[k];
            buttons[k] = buttons[n];
            buttons[n] = value;
        }
        buttons = buttons.OrderBy(button => button.priority).ToList();
    }

    override protected void AlignByTop()
    {
        float pos = -startYPos;
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null)
            {
                var b = buttons[i];
                if (!b.isActiveAndEnabled)
                {
                    continue;
                }
                var t = b.transform;
                t.localPosition = new Vector3(t.localPosition.x, pos);
                pos += -b.height - spaceBetweenButtons;
            }
        }
        contentLength = -(pos + startYPos);
    }
    void VerticalAlign()
    {
        float layoutBottomPosition = MenuController.Instance.bottomGuiPanel.GetComponent<Renderer>().bounds.size.y;

        var delta = (layoutBottomPosition + HangarController.Instance.Tk2dGuiCamera.ScreenExtents.yMin) - mainLayout.GetMinBounds().y;

        mainLayout.Reshape(new Vector3(0, delta, 0), Vector3.zero, true);
        bottomMask.transform.position = new Vector3(bottomMask.transform.position.x, 
            MenuController.Instance.bottomGuiPanel.GetComponent<Renderer>().bounds.max.y, bottomMask.transform.position.z);
    }
    void LateUpdate()
    {
        if ((int)HangarController.Instance.Tk2dGuiCamera.ScreenExtents.yMin != lastCamHeight)
        {
            lastCamHeight = (int)HangarController.Instance.Tk2dGuiCamera.ScreenExtents.yMin;
            VerticalAlign();
        }
    }
}
