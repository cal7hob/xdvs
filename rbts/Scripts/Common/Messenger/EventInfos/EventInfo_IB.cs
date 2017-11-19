using System;
using ExitGames.Client.Photon;

public class EventInfo_IB : EventInfo
{
    private const short DATA_SIZE = 5;

    public int int1;
    public bool bool1;

    private static byte[] bytes = new byte[DATA_SIZE];

    public EventInfo_IB() { }

    public EventInfo_IB(int num, bool flag)
    {
        int1 = num;
        bool1 = flag;
    }

    public override short Serialize(StreamBuffer outStream)
    {
        int index = 0;

        Protocol.Serialize(int1, bytes, ref index);
        bytes[4] = Convert.ToByte(bool1);
        outStream.Write(bytes, 0, DATA_SIZE);

        return DATA_SIZE;
    }

    public override void Deserialize(StreamBuffer inStream, short length)
    {
        int index = 0;
        inStream.Read(bytes, 0, DATA_SIZE);
        Protocol.Deserialize(out int1, bytes, ref index);
        
        bool1 = Convert.ToBoolean(bytes[4]);
    }

    public override string ToString()
    {
        return string.Format("EventInfo_IB: int1 = {0}, bool1 = {1}", int1, bool1);
    }
}
