using UnityEngine;
using System.Collections;
using System;

public class TextTie : MonoBehaviour
{
	public enum Location
	{
		LeftCenter,
		RightCenter,
	}

	public Transform tiedObject;
	public Location location;
	
	private Vector3 offset;
	private tk2dTextMesh textMesh;
    public tk2dTextMesh TextMesh { get { return textMesh; } }
	
	private Vector3 GetTiePosition()
	{
		if (!textMesh)
			textMesh = GetComponent<tk2dTextMesh>();

		Bounds bounds = textMesh.GetEstimatedMeshBoundsForString(textMesh.text);
		switch (location)
		{
			case Location.LeftCenter:
				return transform.position + bounds.min + bounds.extents.y * Vector3.up;
			case Location.RightCenter:
			default:
				return transform.position + bounds.max - bounds.extents.y * Vector3.up;
		}
	}

	public void SetText(string text)
	{
		try//Возникает непонятный нуллреференс, разбираться некогда, добавил трайкетч
        {
            offset = tiedObject.position - GetTiePosition();
            textMesh.text = text;
            tiedObject.position = GetTiePosition() + offset;
        }
        catch(Exception ex)
        {
            DT.LogError("Exception in TextTie.SetText()\nMessage = {0}\nStackTrace = {1}",ex.Message,ex.StackTrace);
        }

	}
}
