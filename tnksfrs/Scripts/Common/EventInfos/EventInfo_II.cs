using System;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections;

public class EventInfo_II : EventInfo
{
	public int int1;
	public int int2;

	public EventInfo_II(){}

	public EventInfo_II(int _num1, int _num2)
	{
		int1 = _num1;
		int2 = _num2;
	}

	public override byte[] Serialize()
	{
		int index = 0;
		byte[] bytes = new byte[8];
		Protocol.Serialize(int1, bytes, ref index);
		Protocol.Serialize(int2, bytes, ref index);

		return bytes;
	}

	public override void Deserialize(byte[] serialized, int startIndex)
	{
		Protocol.Deserialize(out int1, serialized, ref startIndex);
		Protocol.Deserialize(out int2, serialized, ref startIndex);
	}

	public override string ToString()
	{
		return string.Format("EventInfo_II: int1 = {0}, int2 = {1}", int1, int2);
	}
}
