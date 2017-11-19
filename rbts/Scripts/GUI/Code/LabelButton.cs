using UnityEngine;
using System.Collections;

public class LabelButton : MonoBehaviour
{
	[SerializeField]
	private tk2dTextMesh textMesh;

	void Awake()
	{
		if (textMesh != null)
			return;

		textMesh = GetComponentInChildren<tk2dTextMesh>();
		if (textMesh == null)
			DT.LogError(gameObject, "No TextMesh reference. Trying to find.");
		
	}

	public string Text
	{
		get { return textMesh.text; }
		set { textMesh.text = value; }
	}
}
