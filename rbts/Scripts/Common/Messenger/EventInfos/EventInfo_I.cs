using System;
using UnityEngine;
using System.Collections;
using ExitGames.Client.Photon;

public class EventInfo_I : EventInfo
{
    private const short DATA_SIZE = 4;

    public int int1;

    private byte[] bytes = new byte[DATA_SIZE];

	public EventInfo_I(){}
	
	public EventInfo_I(int _num)
	{
		int1 = _num;
	}

	public override short Serialize(StreamBuffer outSTream)
	{
        int index = 0;
        Protocol.Serialize(int1, bytes, ref index);
        outSTream.Write(bytes, 0, DATA_SIZE);

        return DATA_SIZE;
	}

	public override void Deserialize(StreamBuffer inStream, short length)
	{
        int index = 0;
        inStream.Read(bytes, 0, DATA_SIZE);
        Protocol.Deserialize(out int1, bytes, ref index);
	}

	public override string ToString()
	{
		return string.Format("EventInfo_I: int1 = {0}", int1);
	}
}
