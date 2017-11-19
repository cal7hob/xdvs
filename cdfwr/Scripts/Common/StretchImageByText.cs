using UnityEngine;
using System.Collections;

public class StretchImageByText : MonoBehaviour
{
    private tk2dTextMesh textMesh;
    private tk2dSlicedSprite frame;
    public float addToTextLength = 15;
    public float addToTextHeight = 15;
    public bool dontStretchHeight = false;


    void Awake ()
    {
        textMesh = GetComponent<tk2dTextMesh>();
        frame = transform.Find("sprFrame").GetComponent<tk2dSlicedSprite>();
        if (Debug.isDebugBuild && textMesh.anchor.ToString() != frame.anchor.ToString())
            DT.LogError("You must setup the same anchor on the text and frame! Cur values: text.anchor = {0}, frame.anchor = {1}", textMesh.anchor, frame.anchor);
        textMesh.OnTextChange -= StretchImage;
        textMesh.OnTextChange += StretchImage;
    }

    void Start() 
    {
        StretchImage();
    }

    void OnEnable()
    {
        StretchImage();
    }

    void OnDestroy()
    {
        textMesh.OnTextChange -= StretchImage;
    }

    public void StretchImage(EventId evId, EventInfo ev)
    {
        //if (transform.name == "lblOldPrice" && transform.parent.name == "OldPrice" && transform.parent.parent.name == "SaleRentBox")
            //DT3.LogError("textMesh.text = {0}, width = {1}", textMesh.text, textMesh.GetEstimatedMeshBoundsForString(textMesh.text).size);

        if (textMesh == null)
        {
            DT.LogError("Object {0}. TextMesh not found on this object! ", MiscTools.GetFullTransformName(transform));
            return;
        }
        if (frame == null)
        {
            DT.LogError("Label {0}. Image to stretch not found! ", MiscTools.GetFullTransformName(transform));
            return;
        }
        if (!gameObject.GetActive())//Не обновляем фреймы неактивных объектов, для них еще будет OnEnable()
        {
            //DT.LogWarning("Object {0} is not active! activeSelf = {1}", DT.GetFullTransformName(transform),gameObject.activeSelf);
            return;
        }

        //if(transform.name == "lblSpecialOfferRemaining")//lblTimer
        //    DT.Log("SetupFrame on label {0}. text = {1}, textLength = {2}, ImageLength = {3}",
        //        DT.GetFullTransformName(textMesh.transform),textMesh.text, textMesh.GetEstimatedMeshBoundsForString(textMesh.text).size.x, textMesh.GetEstimatedMeshBoundsForString(textMesh.text).size.x + addToTextLength);
        frame.gameObject.SetActive(textMesh.text.Length > 0);

        if (Mathf.Approximately(frame.scale.x, 0) || Mathf.Approximately(frame.scale.y, 0))//division by zero protection
        {
            frame.dimensions = new Vector2(0, 0);
            DT.LogError("StretchImageByText. Some scale is 0 in object {0}. Hiding frame...",MiscTools.GetFullTransformName(frame.transform));
        }
        else
            frame.dimensions = new Vector2(textMesh.GetEstimatedMeshBoundsForString(textMesh.text).size.x / Mathf.Abs(frame.scale.x)  + addToTextLength,
                dontStretchHeight ?
                frame.dimensions.y :
                textMesh.GetEstimatedMeshBoundsForString(textMesh.text).size.y / Mathf.Abs(frame.scale.y) + addToTextHeight);
    }

    private void StretchImage()
    {
        StretchImage(EventId.AfterHangarInit, null);
    }
}
