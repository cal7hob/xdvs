using System;
using UnityEngine;
using System.Collections;
using ExitGames.Client.Photon;

public class EventInfo_B : EventInfo
{
    private const int DATA_SIZE = 1;
    public bool bool1;

    private static byte[] buffer = new byte[1];

    public EventInfo_B() { }

	public EventInfo_B(bool flag)
	{
		bool1 = flag;
	}

	public override short Serialize(StreamBuffer outStream)
	{
        buffer[0] = Convert.ToByte(bool1);
        outStream.Write(buffer, 0, DATA_SIZE);
        return DATA_SIZE;
	}

	public override void Deserialize(StreamBuffer inStream, short length)
	{
        inStream.Read(buffer, 0, DATA_SIZE);
        bool1 = BitConverter.ToBoolean(buffer, 0);
	}

	public override string ToString()
	{
		return string.Format("EventInfo_B: bool1 = {0}", bool1);
	}
}
