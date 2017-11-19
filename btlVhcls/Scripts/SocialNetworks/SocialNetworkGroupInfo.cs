using System;
using UnityEngine;

public sealed class SocialNetworkGroupInfo
{
    public SocialPlatform Platform { get; private set; }
    public string Id { get; private set; }
    public string Name { get; private set; }
    public string Url { get; private set; }

	public SocialNetworkGroupInfo(SocialPlatform platform, string id, string name, string url)
	{
        Platform = platform;
        Id = id;
		Name = name;
		Url = url;
	}

    public override string ToString()
    {
        return string.Format("SocialNetworkGroupInfo: {{platform:{0}; id:{1}; name:{2}; url:{3}}}", Platform, Id, Name, Url);
    }
}
