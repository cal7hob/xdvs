using System;
using Tanks.Models;
using UnityEngine;
using System.Collections;
using XD;

public sealed class SocialNetworkGroup
{
    public event Action<string> Joined = delegate { };
    public event Action JoinFailed = delegate { };
    public SocialNetworkGroupInfo Info { get; private set; }
    public ISocialService SocialService { get; private set; }
    public bool IsMember { get; private set; }

    public SocialNetworkGroup(SocialNetworkGroupInfo groupInfo, bool isMember, ISocialService socialService)
    {
        Info = groupInfo;
        SocialService = socialService;
        IsMember = isMember;
    }
    private void ShowJoinPopup(Action<MessageBox.Answer> callback)
    {
        string joinMessage = Localizer.GetText("lblJoinGroupConfirm",Info.Name);
        MessageBox.Show(MessageBox.Type.Question, joinMessage, callback);
    }
    public void Join()
    {
        AddEventHandlers();
        ShowJoinPopup(delegate(MessageBox.Answer result)
		{
			if (result == MessageBox.Answer.Yes)
			{
                SocialService.JoinGroup(Info.Id);
			}
		});
    }

    private void AddEventHandlers()
    {
        SocialService.GroupJoined += GroupJoined;
        SocialService.GroupJoinErrorOccured += ErrorOccurred;
    }

    private void RemoveEventHandlers()
    {
        SocialService.GroupJoined -= GroupJoined;
        SocialService.GroupJoinErrorOccured -= ErrorOccurred;
    }

    private void ErrorOccurred()
    {
        RemoveEventHandlers();
        JoinFailed();
    }

    private void GroupJoined(string groupId)
    {
        if (groupId == Info.Id)
        {
            IsMember = true;
            RemoveEventHandlers();
            Joined(Info.Id);
            if (groupId == (string) GameData.socialGroups["fb"] 
                || groupId == (string) GameData.socialGroups["vk"] 
                && !ProfileInfo.socialActivity.Contains(SocialAction.joined))
            {
                StaticType.SocialSettings.Instance<ISocialSettings>().ReportSocialActivity(SocialAction.joined);
            }
        }
    }
}
