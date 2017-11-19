using System;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections;

public class EventInfo_IID : EventInfo
{
	public int int1;
	public int int2;
	public double dbl1;

	public EventInfo_IID() { }

	public EventInfo_IID(int num1, int num2, double num3)
	{
		int1 = num1;
		int2 = num2;
		dbl1 = num3;
	}

	public override byte[] Serialize()
	{
		int index = 0;
		byte[] bytes = new byte[16];
		Protocol.Serialize(int1, bytes, ref index);
		Protocol.Serialize(int2, bytes, ref index);
		BitConverter.GetBytes(dbl1).CopyTo(bytes, index);

		return bytes;
	}

	public override void Deserialize(byte[] serialized, int startIndex)
	{
		Protocol.Deserialize(out int1, serialized, ref startIndex);
		Protocol.Deserialize(out int2, serialized, ref startIndex);
		dbl1 = BitConverter.ToDouble(serialized, startIndex);
	}
}
