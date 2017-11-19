using System;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections;

public class EventInfo_V : EventInfo
{

	public Vector3 vector;

	public EventInfo_V() { }

	public EventInfo_V(Vector3 vect)
	{
		vector = vect;
	}

	public override byte[] Serialize()
	{
        byte[] bytes = new byte[3 * 4];
        SerializeVector3(vector).CopyTo(bytes, 4);
        return bytes;
	}

    public override void Deserialize(byte[] serialized, int startIndex)
    {
        vector = DeserializeVector3(serialized, startIndex);
    }
}
