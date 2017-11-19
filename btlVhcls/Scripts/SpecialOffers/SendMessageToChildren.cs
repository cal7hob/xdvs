using UnityEngine;
using System.Collections;

public class SendMessageToChildren : MonoBehaviour
{

	public string methodName;
	void OnEnable()
	{
		SendMessage(methodName, SendMessageOptions.DontRequireReceiver);
	}
}
