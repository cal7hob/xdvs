using System;
using ExitGames.Client.Photon;

public class EventInfo_U : EventInfo
{
    private object[] objects;

    public EventInfo_U(){}

    public EventInfo_U(params object[] objectsToStore)
    {
        objects = objectsToStore;
    }
    
    public override short Serialize(StreamBuffer outStream)
    {
        byte[] bytes = Protocol.Serialize(objects);
        outStream.Write(bytes, 0, bytes.Length);

        return (short)bytes.Length;
    }

    public override void Deserialize(StreamBuffer inStream, short length)
    {
        byte[] bytes = new byte[length];
        inStream.Read(bytes, 0, length);
        objects = (object[])Protocol.Deserialize(bytes);
    }

    public object this[int index]
    {
        get { return objects[index]; }
        set { objects[index] = value; }
    }
}
