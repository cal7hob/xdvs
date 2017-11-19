using System;
using ExitGames.Client.Photon;


public class EventInfo_III : EventInfo
{
	public int int1;
	public int int2;
	public int int3;

	public EventInfo_III() { }

	public EventInfo_III(int _num1, int _num2, int _num3)
	{
		int1 = _num1;
		int2 = _num2;
		int3 = _num3;
	}

	public override byte[] Serialize()
	{
		byte[] bytes = new byte[12];
		int index = 0;
		Protocol.Serialize(int1, bytes, ref index);
		Protocol.Serialize(int2, bytes, ref index);
		Protocol.Serialize(int3, bytes, ref index);

		return bytes;
	}

	public override void Deserialize(byte[] serialized, int startIndex)
	{
		Protocol.Deserialize(out int1, serialized, ref startIndex);
		Protocol.Deserialize(out int2, serialized, ref startIndex);
		Protocol.Deserialize(out int3, serialized, ref startIndex);
	}

	public override string ToString()
	{
		return string.Format("EventInfo_III: int1 = {0}, int2 = {1}, int3 = {2} ", int1, int2, int3);
	}
}
