using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class VisualSlot : MonoBehaviour
{
    public Transform handle;
    public Transform wheelLeft;
    public Transform wheelRight;
    public Transform wheelCenter;
    private Tweener left;
    private Tweener right;
    private Tweener center;
    private bool InProcces;

    private Dictionary<float, string> slotsPosition = new Dictionary<float, string>
    {
        {0f, "grape"},
        {40f, "cherry"},
        {85f, "bar" },
        {120f, "watermelon"},
        {166f, "star" },
        {200f, "seven"},
        {240f, "plum" },
        {280f, "lemon" },
        {323f, "orange"}
    };

    public void OnClick()
    {
        if (InProcces)
        {
            return;
        }
        Debug.LogError("Клик прошёл");
        handle.DORotate(new Vector3(90, 0, 0), 0.5f);
        InProcces = true;
        RotateWheels();
        Invoke("ReverseHandle", 0.5f);
    }

    private void ReverseHandle()
    {
        handle.DORotate(new Vector3(0, 0, 0), 0.5f);
    }

    private void RotateWheels()
    {
        Debug.LogError(wheelLeft.localRotation);
        left.Kill();
        right.Complete();
        center.Complete();
        wheelLeft.localEulerAngles = Vector3.zero;
        wheelRight.localEulerAngles = Vector3.zero;
        wheelCenter.localEulerAngles = Vector3.zero;
        //wheelLeft.localPosition(new Quaternion());
        //wheelCenter.localRotation(Vector3.zero);
        wheelRight.Rotate(Vector3.zero);
        left = wheelLeft.DOLocalRotate(new Vector3(3640, 0, 0), 7f, RotateMode.LocalAxisAdd);
        right = wheelRight.DOLocalRotate(new Vector3(3685, 0, 0),8f, RotateMode.LocalAxisAdd);
        center = wheelCenter.DOLocalRotate(new Vector3(3720, 0, 0), 9f, RotateMode.LocalAxisAdd);
        InProcces = false;
        //   Invoke("StopWheels", 2f);
    }

    private void StopWheels()
    {
        left.Kill();
        right.Complete();
        center.Complete();
        //  Invoke("GoToEndPosition", 1f);
    }

    private void GoToEndPosition()
    {
        wheelLeft.DOLocalRotate(new Vector3(40, 0, 0), 2f, RotateMode.FastBeyond360);
        wheelRight.DOLocalRotate(new Vector3(85, 0, 0), 2f, RotateMode.FastBeyond360);
        wheelCenter.DOLocalRotate(new Vector3(120, 0, 0), 2f, RotateMode.FastBeyond360);
        //    Invoke("ResetProgress", 2f);
    }

    private void ResetProgress()
    {
        InProcces = false;
    }


}
