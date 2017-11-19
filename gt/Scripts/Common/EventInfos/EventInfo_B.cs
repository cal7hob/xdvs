using System;
using UnityEngine;
using System.Collections;

public class EventInfo_B : EventInfo
{
	public bool bool1;

	public EventInfo_B() { }

	public EventInfo_B(bool flag)
	{
		bool1 = flag;
	}

	public override byte[] Serialize()
	{
		return BitConverter.GetBytes(bool1);
	}

	public override void Deserialize(byte[] serialized, int startIndex)
	{
		bool1 = BitConverter.ToBoolean(serialized, startIndex);
	}

	public override string ToString()
	{
		return string.Format("EventInfo_B: bool1 = {0}", bool1);
	}
}
