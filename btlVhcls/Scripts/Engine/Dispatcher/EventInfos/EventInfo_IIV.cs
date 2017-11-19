using System;
using System.IO;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections;

public class EventInfo_IIV : EventInfo
{
	private const int SIZE = 4 + 4 + (4 * 3);

	public int int1;
	public int int2;
	public Vector3 vector;

	public EventInfo_IIV()
	{}
	
	public EventInfo_IIV(int _activeId, int _passiveId, Vector3 _point)
	{
		int1 = _activeId;
		int2 = _passiveId;
		vector = _point;
	}

	public override byte[] Serialize()
	{
		byte[] bytes = new byte[SIZE];
		BitConverter.GetBytes(int1).CopyTo(bytes, 0);
		BitConverter.GetBytes(int2).CopyTo(bytes, 4);
		SerializeVector3(vector).CopyTo(bytes, 8);
		
		return bytes;
	}

	public override void Deserialize(byte[] bytes, int startIndex)
	{
		int1 = BitConverter.ToInt32(bytes, startIndex);
		int2 = BitConverter.ToInt32(bytes, startIndex + 4);
		vector = DeserializeVector3(bytes, startIndex + 8);
	}
}
