using System;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections;

public class EventInfo_V2 : EventInfo
{

    public Vector2 vector;

    public EventInfo_V2() { }

    public EventInfo_V2(Vector2 vect)
    {
        vector = vect;
    }

    public override byte[] Serialize()
    {
        byte[] bytes = new byte[2 * 4];
        SerializeVector2(vector).CopyTo(bytes, 4);
        return bytes;
    }

    public override void Deserialize(byte[] serialized, int startIndex)
    {
        vector = DeserializeVector2(serialized, startIndex);
    }
}