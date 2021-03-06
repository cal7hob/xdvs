using System;
using System.IO;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections;

public class EventInfo_IIIV : EventInfo
{
	private const int SIZE = 4 + 4 + 4 + (4 * 3);

	public int int1;
	public int int2;
	public int int3;
	public Vector3 vector;

	public EventInfo_IIIV()
	{ }

	public EventInfo_IIIV(int _int1, int _int2, int _int3, Vector3 _point)
	{
		int1 = _int1;
		int2 = _int2;
		int3 = _int3;
		vector = _point;
	}

	public override byte[] Serialize()
	{
		byte[] bytes = new byte[SIZE];
		BitConverter.GetBytes(int1).CopyTo(bytes, 0);
		BitConverter.GetBytes(int2).CopyTo(bytes, 4);
		BitConverter.GetBytes(int3).CopyTo(bytes, 8);
		SerializeVector3(vector).CopyTo(bytes, 12);

		return bytes;
	}

	public override void Deserialize(byte[] bytes, int startIndex)
	{
		int1 = BitConverter.ToInt32(bytes, startIndex);
		int2 = BitConverter.ToInt32(bytes, startIndex + 4);
		int3 = BitConverter.ToInt32(bytes, startIndex + 8);
		vector = DeserializeVector3(bytes, startIndex + 12);
	}
}
