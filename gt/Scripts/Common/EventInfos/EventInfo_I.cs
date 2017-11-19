using System;
using UnityEngine;
using System.Collections;

public class EventInfo_I : EventInfo
{
	public int int1;

	public EventInfo_I(){}
	
	public EventInfo_I(int _num)
	{
		int1 = _num;
	}

	public override byte[] Serialize()
	{
		return BitConverter.GetBytes(int1);
	}

	public override void Deserialize(byte[] serialized, int startIndex)
	{
		int1 = BitConverter.ToInt32(serialized, startIndex);
	}

	public override string ToString()
	{
		return string.Format("EventInfo_I: int1 = {0}", int1);
	}
}
