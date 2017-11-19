using System;
using ExitGames.Client.Photon;

public class EventInfo_III : EventInfo
{
    private const short DATA_SIZE = 12;

    public int int1;
    public int int2;
    public int int3;

    private static byte[] bytes = new byte[DATA_SIZE];

    public EventInfo_III() { }

    public EventInfo_III(int _num1, int _num2, int num3)
    {
        int1 = _num1;
        int2 = _num2;
        int3 = num3;
    }

    public override short Serialize(StreamBuffer outStream)
    {
        int index = 0;

        Protocol.Serialize(int1, bytes, ref index);
        Protocol.Serialize(int2, bytes, ref index);
        Protocol.Serialize(int3, bytes, ref index);

        outStream.Write(bytes, 0, DATA_SIZE);

        return DATA_SIZE;
    }

    public override void Deserialize(StreamBuffer inStream, short length)
    {
        inStream.Read(bytes, 0, DATA_SIZE);

        int index = 0;
        Protocol.Deserialize(out int1, bytes, ref index);
        Protocol.Deserialize(out int2, bytes, ref index);
        Protocol.Deserialize(out int3, bytes, ref index);
    }

    public override string ToString()
    {
        return string.Format("EventInfo_III: int1 = {0}, int2 = {1}, int3 = {2}", int1, int2, int3);
    }
}
