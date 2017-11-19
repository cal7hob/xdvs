using UnityEngine;

public class MinimapCornerHighlight : MonoBehaviour
{
	public bool drawRaysOnSelected = true;
	public Color32 selectedColor = Color.yellow;
	public Color32 normalColor = Color.cyan;
	public Color32 xRayColor = Color.red;
	public Color32 yRayColor = Color.green;
	public Color32 zRayColor = Color.blue;
	public bool revertRays = false;
	public float size = 1;
	public bool drawSphereNotCube = true;
#if UNITY_EDITOR

	//--- Unity gizmos --------------------------
	void OnDrawGizmos()
	{
		DrawCenter(normalColor);
	}
	
	void OnDrawGizmosSelected()
	{
		DrawCenter(selectedColor);
		if (drawRaysOnSelected)
			DrawRays(xRayColor, yRayColor, zRayColor);
	}
	
	void DrawCenter(Color32 col)
	{
		Gizmos.color = col;
		if (drawSphereNotCube)
			Gizmos.DrawWireSphere(transform.position, size);
		else
			Gizmos.DrawCube(transform.position, Vector3.one * size);
	}
	
	void DrawRays(Color32 xColor, Color32 yColor, Color32 zColor)
	{
		int k = 1000;
		if (revertRays)
			k = -k;
		Gizmos.color = xColor;
		Gizmos.DrawRay(transform.position, transform.right * k);
		Gizmos.color = yColor;
		Gizmos.DrawRay(transform.position, transform.up * k);
		Gizmos.color = zColor;
		Gizmos.DrawRay(transform.position, transform.forward * k);
	}
#endif
}

