using System;

public sealed class SocialNetworkInfo
{
	public SocialPlatform Platform { get; private set; }

    public string Name { get; private set; }

    public string PictureName { get; private set; }

    public SocialNetworkInfo(SocialPlatform platform, string name, string pictureName)
	{
        Platform = platform;
		Name = name;
		PictureName = pictureName;
	}
}
