using System;
using ExitGames.Client.Photon;


public class EventInfo_IIII : EventInfo
{
	public int int1;
	public int int2;
	public int int3;
	public int int4;

	public EventInfo_IIII() { }

	public EventInfo_IIII(int num1, int num2, int num3, int num4)
	{
		int1 = num1;
		int2 = num2;
		int3 = num3;
		int4 = num4;
	}

	public override byte[] Serialize()
	{
		byte[] bytes = new byte[16];
		int index = 0;
		Protocol.Serialize(int1, bytes, ref index);
		Protocol.Serialize(int2, bytes, ref index);
		Protocol.Serialize(int3, bytes, ref index);
		Protocol.Serialize(int4, bytes, ref index);
		

		return bytes;
	}

	public override void Deserialize(byte[] serialized, int startIndex)
	{
		Protocol.Deserialize(out int1, serialized, ref startIndex);
		Protocol.Deserialize(out int2, serialized, ref startIndex);
		Protocol.Deserialize(out int3, serialized, ref startIndex);
		Protocol.Deserialize(out int4, serialized, ref startIndex);
	}

	public override string ToString()
	{
		return string.Format("EventInfo_II: int1 = {0}, int2 = {1}, int3 = {2}, int4 = {3}", int1, int2, int3, int4);
	}
}
