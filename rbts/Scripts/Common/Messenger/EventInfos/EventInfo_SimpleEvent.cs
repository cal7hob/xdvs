using UnityEngine;
using System.Collections;
using ExitGames.Client.Photon;

public class EventInfo_SimpleEvent : EventInfo
{
	public override short Serialize(StreamBuffer outStream)
	{
		return 0;
	}

	public override void Deserialize(StreamBuffer inStream, short length)
	{
	}
}
