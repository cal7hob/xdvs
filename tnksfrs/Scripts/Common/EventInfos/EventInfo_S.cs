using System.Text;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections;


using System;
public class EventInfo_S : EventInfo
{
	public string str1;
	
	public EventInfo_S()
	{ }

	public EventInfo_S(string _str1)
	{
		str1 = _str1;
	}

	public override byte[] Serialize()
	{
		byte[] strBytes1 = Encoding.UTF8.GetBytes(str1);
		byte[] bytes = new byte[strBytes1.Length + 4];
		int index = 0;
		BitConverter.GetBytes(strBytes1.Length).CopyTo(bytes, index);
		index += 4;
		strBytes1.CopyTo(bytes, index);
		
		return bytes;
	}

	public override void Deserialize(byte[] bytes, int startIndex)
	{
		str1 = DeserializeString(bytes, ref startIndex);
	}
}