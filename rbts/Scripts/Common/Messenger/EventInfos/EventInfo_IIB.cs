using System;
using ExitGames.Client.Photon;

public class EventInfo_IIB : EventInfo
{
    private const short DATA_SIZE = 9;

    public int int1;
    public int int2;
    public bool bool1;

    private static byte[] bytes = new byte[DATA_SIZE];

    public EventInfo_IIB() { }

    public EventInfo_IIB(int num, int num2, bool flag)
    {
        int1 = num;
        int2 = num2;
        bool1 = flag;
    }

    public override short Serialize(StreamBuffer outStream)
    {
        int index = 0;

        Protocol.Serialize(int1, bytes, ref index);
        Protocol.Serialize(int2, bytes, ref index);
        bytes[index] = Convert.ToByte(bool1);

        return DATA_SIZE;
    }

    public override void Deserialize(StreamBuffer inStream, short length)
    {
        inStream.Read(bytes, 0, DATA_SIZE);

        int index = 0;

        Protocol.Deserialize(out int1, bytes, ref index);
        Protocol.Deserialize(out int2, bytes, ref index);

        byte boolByte = bytes[index];
        bool1 = Convert.ToBoolean(boolByte);
    }

    public override string ToString()
    {
        return string.Format("EventInfo_IIB: int1 = {0}, int2 = {1}, bool1 = {2}", int1, int2, bool1);
    }
}
