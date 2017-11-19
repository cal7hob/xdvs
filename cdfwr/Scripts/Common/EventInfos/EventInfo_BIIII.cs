using System;

public class EventInfo_BIIII : EventInfo
{
	private const int SIZE = 4 * 5;

    public bool bool1;
    public int int1;
    public int int2;
    public int int3;
    public int int4;

    public EventInfo_BIIII() { }

    public EventInfo_BIIII(bool _bool1, int _int1, int _int2, int _int3, int _int4)
    {
        bool1 = _bool1;
        int1 = _int1;
        int2 = _int2;
        int3 = _int3;
        int4 = _int4;
    }

    public override byte[] Serialize()
    {
        byte[] bytes = new byte[SIZE];

        BitConverter.GetBytes(Convert.ToInt32(bool1)).CopyTo(bytes, 0);
        BitConverter.GetBytes(int1).CopyTo(bytes, 4);
        BitConverter.GetBytes(int2).CopyTo(bytes, 8);
        BitConverter.GetBytes(int3).CopyTo(bytes, 12);
        BitConverter.GetBytes(int4).CopyTo(bytes, 16);

        return bytes;
    }

    public override void Deserialize(byte[] bytes, int startIndex)
    {
        bool1 = Convert.ToBoolean(BitConverter.ToInt32(bytes, startIndex));
        int1 = BitConverter.ToInt32(bytes, startIndex + 4);
        int2 = BitConverter.ToInt32(bytes, startIndex + 8);
        int3 = BitConverter.ToInt32(bytes, startIndex + 12);
        int4 = BitConverter.ToInt32(bytes, startIndex + 16);
    }
}
