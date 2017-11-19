using System;
using UnityEngine;
using System.Collections;
using System.Security.Cryptography;
using ExitGames.Client.Photon;

public class EventInfo_U : EventInfo
{
    private object[] objects;
    public EventInfo_U(){}

    public EventInfo_U(params object[] objectsToStore)
    {
        objects = objectsToStore;
    }
    
    public override byte[] Serialize()
    {
        return Protocol.Serialize(objects);
    }

    public override void Deserialize(byte[] serialized, int startIndex)
    {
        byte[] bytes = new byte[serialized.Length - startIndex];
        Array.Copy(serialized, startIndex, bytes, 0, serialized.Length - startIndex);
        objects = (object[])Protocol.Deserialize(bytes);
    }

    public object this[int index]
    {
        get { return objects[index]; }
    }
}
