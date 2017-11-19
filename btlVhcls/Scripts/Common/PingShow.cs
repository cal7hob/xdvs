using UnityEngine;
using System.Collections;

public class PingShow : MonoBehaviour
{
	public tk2dTextMesh textMesh;
	
	void Awake()
	{
		if (!Debug.isDebugBuild)
		{
			gameObject.SetActive(false);
			return;
		}

		this.InvokeRepeating(Show, 0, 1);
	}
	
	void Show()
	{
		textMesh.text = PhotonNetwork.connected ? string.Format("PING: {0}", PhotonNetwork.GetPing().ToString()) : "";
	}
}
