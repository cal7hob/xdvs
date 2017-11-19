using System;
using ExitGames.Client.Photon;

public class EventInfo_II : EventInfo
{
    private const short DATA_SIZE = 8;

    public int int1;
	public int int2;

    private static byte[] bytes = new byte[DATA_SIZE];

	public EventInfo_II(){}

	public EventInfo_II(int _num1, int _num2)
	{
		int1 = _num1;
		int2 = _num2;
	}

	public override short Serialize(StreamBuffer outStream)
	{
		int index = 0;
		
		Protocol.Serialize(int1, bytes, ref index);
		Protocol.Serialize(int2, bytes, ref index);

        outStream.Write(bytes, 0, DATA_SIZE);

        return DATA_SIZE;
	}

	public override void Deserialize(StreamBuffer inStream, short length)
	{
        inStream.Read(bytes, 0, DATA_SIZE);

        int index = 0;
        Protocol.Deserialize(out int1, bytes, ref index);
		Protocol.Deserialize(out int2, bytes, ref index);
	}

	public override string ToString()
	{
		return string.Format("EventInfo_II: int1 = {0}, int2 = {1}", int1, int2);
	}
}
