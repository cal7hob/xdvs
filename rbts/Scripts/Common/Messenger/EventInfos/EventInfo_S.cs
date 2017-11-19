using System.Text;
using ExitGames.Client.Photon;

using System;
public class EventInfo_S : EventInfo
{
    public string str1;

    private static byte[] bytes = new byte[512];
	
	public EventInfo_S()
	{ }

	public EventInfo_S(string _str1)
	{
		str1 = _str1;
	}

	public override short Serialize(StreamBuffer outStream)
	{
        Encoding.UTF8.GetBytes(str1, 0, str1.Length, bytes, 0);

        short size = (short)str1.Length;
        outStream.Write(bytes, 0, size);        
		return size;
	}

	public override void Deserialize(StreamBuffer inStream, short length)
	{
        int index = 0;
        Protocol.Serialize((int)length, bytes, ref index);
        inStream.Read(bytes, 4, length);
        index = 0;
        str1 = DeserializeString(bytes, ref index);
	}
}