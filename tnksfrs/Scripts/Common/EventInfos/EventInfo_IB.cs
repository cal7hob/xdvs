using System;
using ExitGames.Client.Photon;

public class EventInfo_IB : EventInfo
{
    public int int1;
    public bool bool1;

    public EventInfo_IB() { }

    public EventInfo_IB(int num, bool flag)
    {
        int1 = num;
        bool1 = flag;
    }

    public override byte[] Serialize()
    {
        int index = 0;
        byte[] bytes = new byte[8];

        Protocol.Serialize(int1, bytes, ref index);
        Protocol.Serialize(Convert.ToInt32(bool1), bytes, ref index);

        return bytes;
    }

    public override void Deserialize(byte[] serialized, int startIndex)
    {
        int boolInt;

        Protocol.Deserialize(out int1, serialized, ref startIndex);
        Protocol.Deserialize(out boolInt, serialized, ref startIndex);

        bool1 = Convert.ToBoolean(boolInt);
    }

    public override string ToString()
    {
        return string.Format("EventInfo_IB: int1 = {0}, bool1 = {1}", int1, bool1);
    }
}
