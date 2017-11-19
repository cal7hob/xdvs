using UnityEngine;
using ExitGames.Client.Photon;

public class EventInfo_IV : EventInfo
{
    private const short DATA_SIZE = 16;

    public int int1;
    public Vector3 vector;

    private static byte[] bytes = new byte[DATA_SIZE];

    public EventInfo_IV() { }

    public EventInfo_IV(int _num1, Vector3 vector)
    {
        int1 = _num1;
        this.vector = vector;
    }

    public override short Serialize(StreamBuffer outStream)
    {
        int index = 0;

        Protocol.Serialize(int1, bytes, ref index);
        SerializeVector3(vector, bytes, ref index);

        outStream.Write(bytes, 0, DATA_SIZE);

        return DATA_SIZE;
    }

    public override void Deserialize(StreamBuffer inStream, short length)
    {
        inStream.Read(bytes, 0, DATA_SIZE);

        int index = 0;
        Protocol.Deserialize(out int1, bytes, ref index);
        vector = DeserializeVector3(bytes, index);
    }

    public override string ToString()
    {
        return string.Format("EventInfo_IIIIV: int1 = {0}, vector = {1}", int1, vector);
    }
}
