using System;
using ExitGames.Client.Photon;

public class EventInfo_IIIIB : EventInfo
{
    private const short DATA_SIZE = 1 + 4 * 4;
    private static byte[] bytes = new byte[DATA_SIZE];

    public bool bool1;
    public int int1;
    public int int2;
    public int int3;
    public int int4;

    public EventInfo_IIIIB() { }

    public EventInfo_IIIIB(bool _bool1, int _int1, int _int2, int _int3, int _int4)
    {
        bool1 = _bool1;
        int1 = _int1;
        int2 = _int2;
        int3 = _int3;
        int4 = _int4;
    }

    public override short Serialize(StreamBuffer outStream)
    {
        byte bl = Convert.ToByte(bool1);
        bytes[0] = bl;

        int index = 1;
        Protocol.Serialize(int1, bytes, ref index);
        Protocol.Serialize(int2, bytes, ref index);
        Protocol.Serialize(int3, bytes, ref index);
        Protocol.Serialize(int4, bytes, ref index);
        
        outStream.Write(bytes, 0, DATA_SIZE);

        return DATA_SIZE;
    }

    public override void Deserialize(StreamBuffer inBuffer, short length)
    {
        inBuffer.Read(bytes, 0, DATA_SIZE);

        bool1 = Convert.ToBoolean(bytes[0]);
        int1 = BitConverter.ToInt32(bytes, 1);
        int2 = BitConverter.ToInt32(bytes, 5);
        int3 = BitConverter.ToInt32(bytes, 9);
        int4 = BitConverter.ToInt32(bytes, 13);
    }
}
