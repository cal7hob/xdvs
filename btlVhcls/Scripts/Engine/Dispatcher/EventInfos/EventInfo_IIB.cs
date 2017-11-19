using System;
using ExitGames.Client.Photon;

public class EventInfo_IIB : EventInfo
{
    public int int1;
    public int int2;
    public bool bool1;

    public EventInfo_IIB() { }

    public EventInfo_IIB(int num, int num2, bool flag)
    {
        int1 = num;
        int2 = num2;
        bool1 = flag;
    }

    public override byte[] Serialize()
    {
        int index = 0;
        byte[] bytes = new byte[9];

        Protocol.Serialize(int1, bytes, ref index);
        Protocol.Serialize(int2, bytes, ref index);
        bytes[index] = Convert.ToByte(bool1);

        return bytes;
    }

    public override void Deserialize(byte[] serialized, int startIndex)
    {
        byte boolByte;

        Protocol.Deserialize(out int1, serialized, ref startIndex);
        Protocol.Deserialize(out int2, serialized, ref startIndex);
        boolByte = serialized[startIndex];

        bool1 = Convert.ToBoolean(boolByte);
    }

    public override string ToString()
    {
        return string.Format("EventInfo_IIB: int1 = {0}, int2 = {1}, bool1 = {2}", int1, int2, bool1);
    }
}
