using UnityEngine;
using System.Collections;

public class EventInfo_SimpleEvent : EventInfo
{
	public override byte[] Serialize()
	{
		return null;
	}

	public override void Deserialize(byte[] serialized, int startIndex)
	{
	}
}
