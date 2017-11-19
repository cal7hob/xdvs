using System;
using UnityEngine;

public sealed class SocialNetworkGroupInfo
{
	public string Id { get; private set; }

    public string Name { get; private set; }

    public string PictureName { get; private set; }

    public string Url { get; private set; }

	public SocialNetworkGroupInfo(string id, string name, string pictureName, string url)
	{
		Id = id;
		Name = name;
		PictureName = pictureName;
		Url = url;
	}

    public override string ToString()
    {
        return string.Format("SocialNetworkGroupInfo:{{id:{0};name:{1};pictureName:{2};url:{3}}}", Id, Name, PictureName, Url);
    }
}
