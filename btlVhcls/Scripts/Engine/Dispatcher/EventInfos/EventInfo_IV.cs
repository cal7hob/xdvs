using System;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections;

public class EventInfo_IV : EventInfo
{
	public int int1;
	public Vector3 vector;

	public EventInfo_IV(){}
	
	public EventInfo_IV(int _num, Vector3 vect)
	{
		int1 = _num;
		vector = vect;
	}

	public override byte[] Serialize()
	{
		byte[] bytes = new byte[4 + 3 * 4];
		BitConverter.GetBytes(int1).CopyTo(bytes, 0);
		SerializeVector3(vector).CopyTo(bytes, 4);

		return bytes;
	}

	public override void Deserialize(byte[] serialized, int startIndex)
	{
		int1 = BitConverter.ToInt32(serialized, startIndex);
		vector = DeserializeVector3(serialized, startIndex + 4);
	}
}
