using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SocialPlatforms;

public static class AchievementsIds
{
    public enum PlayServiceType
    {
        Disabled,
        GooglePlay,
        IOSGameCenter,
        MACGameCenter,
        SocialNetworks
    }

    public enum Id
    {
        ReachedLevel2 = 2,
        ReachedLevel3,
        ReachedLevel4,
        ReachedLevel5,
        ReachedLevel6,
        ReachedLevel7,
        ReachedLevel8,
        ReachedLevel9,
        ReachedLevel10,
        ReachedLevel11,
        ReachedLevel12,
        ReachedLevel13,
        ReachedLevel14,
        ReachedLevel15,
        ReachedLevel16,
        ReachedLevel17,
        ReachedLevel18,
        ReachedLevel19,
        ReachedLevel20,
        ReachedLevel21,
        ReachedLevel22,
        ReachedLevel23,
        ReachedLevel24,
        ReachedLevel25,
        ReachedLevel26,
        ReachedLevel27,
        ReachedLevel28,
        ReachedLevel29,
        ReachedLevel30,
        ReachedLevel31,
        ReachedLevel32,
        ReachedLevel33,
        ReachedLevel34,
        ReachedLevel35,
        ReachedLevel36,
        NewbieTankMan,
        ExperiencedTanker,
        ProfessionalTanker,
        Shredder,
        Killer,
        Terminator,
        Ordinary,
        Sergeant,
        Colonel,
        Gambler,
        Admirer,
        Fans,
        Scout,
        Travelers,
        Wanderer,
        Survivor
    }

    public static PlayServiceType CurrentPlayService
    {
        get
        {
            switch (Application.platform)
            {
                case RuntimePlatform.IPhonePlayer: return PlayServiceType.IOSGameCenter;
                case RuntimePlatform.OSXPlayer: return PlayServiceType.Disabled;
                case RuntimePlatform.Android:  return GameData.IsGame(Game.AmazonBuild) ? PlayServiceType.Disabled : PlayServiceType.GooglePlay;
                case RuntimePlatform.WebGLPlayer: return PlayServiceType.SocialNetworks;
                default:
                    return PlayServiceType.Disabled;
            }
        }
    }

    public static bool isInited = false;
    public static Dictionary<string, Id> storeIdToEnum = new Dictionary<string, Id>();
    public static Dictionary<string, Id> enumStringToEnum = new Dictionary<string, Id>();

    public static readonly Dictionary<Interface, Dictionary<PlayServiceType, Dictionary<Id, string>>> data = new Dictionary<Interface, Dictionary<PlayServiceType, Dictionary<Id, string>>>
	{
		{
            Interface.IronTanks, new Dictionary<PlayServiceType, Dictionary<Id, string>> 
			{
                {
                    PlayServiceType.GooglePlay, new Dictionary<Id, string>
                    {
                        { Id.ReachedLevel2,  "CgkI5ubipOgZEAIQEA" }, { Id.ReachedLevel3,  "CgkI5ubipOgZEAIQEg" }, { Id.ReachedLevel4,  "CgkI5ubipOgZEAIQEw" },
                        { Id.ReachedLevel5,  "CgkI5ubipOgZEAIQFA" }, { Id.ReachedLevel6,  "CgkI5ubipOgZEAIQFQ" }, { Id.ReachedLevel7,  "CgkI5ubipOgZEAIQFg" },
                        { Id.ReachedLevel8,  "CgkI5ubipOgZEAIQFw" }, { Id.ReachedLevel9,  "CgkI5ubipOgZEAIQGA" }, { Id.ReachedLevel10, "CgkI5ubipOgZEAIQGQ" },
                        { Id.ReachedLevel11, "CgkI5ubipOgZEAIQGg" }, { Id.ReachedLevel12, "CgkI5ubipOgZEAIQGw" }, { Id.ReachedLevel13, "CgkI5ubipOgZEAIQHA" },
                        { Id.ReachedLevel14, "CgkI5ubipOgZEAIQHQ" }, { Id.ReachedLevel15, "CgkI5ubipOgZEAIQHg" }, { Id.ReachedLevel16, "CgkI5ubipOgZEAIQHw" },
                        { Id.ReachedLevel17, "CgkI5ubipOgZEAIQIA" }, { Id.ReachedLevel18, "CgkI5ubipOgZEAIQIQ" }, { Id.ReachedLevel19, "CgkI5ubipOgZEAIQIg" },
                        { Id.ReachedLevel20, "CgkI5ubipOgZEAIQIw" }, { Id.ReachedLevel21, "CgkI5ubipOgZEAIQJA" }, { Id.ReachedLevel22, "CgkI5ubipOgZEAIQJQ" },
                        { Id.ReachedLevel23, "CgkI5ubipOgZEAIQJg" }, { Id.ReachedLevel24, "CgkI5ubipOgZEAIQJw" }, { Id.ReachedLevel25, "CgkI5ubipOgZEAIQKA" },
                        { Id.ReachedLevel26, "CgkI5ubipOgZEAIQKQ" }, { Id.ReachedLevel27, "CgkI5ubipOgZEAIQKg" }, { Id.ReachedLevel28, "CgkI5ubipOgZEAIQKw" },
                        { Id.ReachedLevel29, "CgkI5ubipOgZEAIQLA" }, { Id.ReachedLevel30, "CgkI5ubipOgZEAIQLQ" }, { Id.ReachedLevel31, "CgkI5ubipOgZEAIQLg" },
                        { Id.ReachedLevel32, "CgkI5ubipOgZEAIQLw" }, { Id.ReachedLevel33, "CgkI5ubipOgZEAIQMA" }, { Id.ReachedLevel34, "CgkI5ubipOgZEAIQMQ" },
                        { Id.ReachedLevel35, "CgkI5ubipOgZEAIQMg" }, { Id.ReachedLevel36, "CgkI5ubipOgZEAIQMw" },

                        { Id.NewbieTankMan, "CgkI5ubipOgZEAIQAA"}, { Id.ExperiencedTanker, "CgkI5ubipOgZEAIQAQ" }, { Id.ProfessionalTanker, "CgkI5ubipOgZEAIQAg" },
                        { Id.Shredder,      "CgkI5ubipOgZEAIQAw"}, { Id.Killer,            "CgkI5ubipOgZEAIQBA" }, { Id.Terminator,         "CgkI5ubipOgZEAIQBQ" },

                        { Id.Ordinary, "CgkI5ubipOgZEAIQBg" }, { Id.Sergeant,  "CgkI5ubipOgZEAIQBw" }, {Id.Colonel,  "CgkI5ubipOgZEAIQCA"},
                        { Id.Gambler,  "CgkI5ubipOgZEAIQCQ" }, { Id.Admirer,   "CgkI5ubipOgZEAIQCg" }, {Id.Fans,     "CgkI5ubipOgZEAIQCw"},
                        { Id.Scout,    "CgkI5ubipOgZEAIQDA" }, { Id.Travelers, "CgkI5ubipOgZEAIQDQ" }, {Id.Wanderer, "CgkI5ubipOgZEAIQDg"},
                        { Id.Survivor, "CgkI5ubipOgZEAIQDw" }
                    }
                },
                {
                    PlayServiceType.IOSGameCenter,new Dictionary<Id,string>
                    {
                        //{ Id.ReachedLevel2,  "52" }, { Id.ReachedLevel3,  "53" }, { Id.ReachedLevel4,  "54" }, { Id.ReachedLevel5,  "55" }, { Id.ReachedLevel6,  "56" },
                        //{ Id.ReachedLevel7,  "57" }, { Id.ReachedLevel8,  "58" }, { Id.ReachedLevel9,  "59" }, { Id.ReachedLevel10, "60" }, { Id.ReachedLevel11, "61" },
                        //{ Id.ReachedLevel12, "62" }, { Id.ReachedLevel13, "63" }, { Id.ReachedLevel14, "64" }, { Id.ReachedLevel15, "65" }, { Id.ReachedLevel16, "66" },
                        //{ Id.ReachedLevel17, "67" }, { Id.ReachedLevel18, "68" }, { Id.ReachedLevel19, "69" }, { Id.ReachedLevel20, "70" }, { Id.ReachedLevel21, "71" },
                        //{ Id.ReachedLevel22, "72" }, { Id.ReachedLevel23, "73" }, { Id.ReachedLevel24, "74" }, { Id.ReachedLevel25, "75" }, { Id.ReachedLevel26, "76" },
                        //{ Id.ReachedLevel27, "77" }, { Id.ReachedLevel28, "78" }, { Id.ReachedLevel29, "79" }, { Id.ReachedLevel30, "80" }, { Id.ReachedLevel31, "81" },
                        //{ Id.ReachedLevel32, "82" }, { Id.ReachedLevel33, "83" }, { Id.ReachedLevel34, "84" }, { Id.ReachedLevel35, "85" }, { Id.ReachedLevel36, "86" },

                        //{ Id.NewbieTankMan, "87"}, { Id.ExperiencedTanker, "88" }, { Id.ProfessionalTanker, "89" },
                        //{ Id.Shredder,      "90"}, { Id.Killer,            "91" }, { Id.Terminator,         "92" },

                        //{ Id.Ordinary,  "93"}, { Id.Sergeant,  "94"},  { Id.Colonel,  "95"},
                        //{ Id.Gambler,   "96"}, { Id.Admirer,   "97"},  { Id.Fans,     "98"},
                        //{ Id.Scout,     "99"}, { Id.Travelers, "100"}, { Id.Wanderer, "101"},
                        //{ Id.Survivor,  "102"}
                        { Id.ReachedLevel2,  "grp.452"}, { Id.ReachedLevel3,   "grp.453" }, { Id.ReachedLevel4,  "grp.454" }, { Id.ReachedLevel5,  "grp.455" }, { Id.ReachedLevel6,  "grp.456" },
                        { Id.ReachedLevel7,  "grp.457" }, { Id.ReachedLevel8,  "grp.458" }, { Id.ReachedLevel9,  "grp.459" }, { Id.ReachedLevel10, "grp.460" }, { Id.ReachedLevel11, "grp.461" },
                        { Id.ReachedLevel12, "grp.462" }, { Id.ReachedLevel13, "grp.463" }, { Id.ReachedLevel14, "grp.464" }, { Id.ReachedLevel15, "grp.465" }, { Id.ReachedLevel16, "grp.466" },
                        { Id.ReachedLevel17, "grp.467" }, { Id.ReachedLevel18, "grp.468" }, { Id.ReachedLevel19, "grp.469" }, { Id.ReachedLevel20, "grp.470" }, { Id.ReachedLevel21, "grp.471" },
                        { Id.ReachedLevel22, "grp.472" }, { Id.ReachedLevel23, "grp.473" }, { Id.ReachedLevel24, "grp.474" }, { Id.ReachedLevel25, "grp.475" }, { Id.ReachedLevel26, "grp.476" },
                        { Id.ReachedLevel27, "grp.477" }, { Id.ReachedLevel28, "grp.478" }, { Id.ReachedLevel29, "grp.479" }, { Id.ReachedLevel30, "grp.480" }, { Id.ReachedLevel31, "grp.481" },
                        { Id.ReachedLevel32, "grp.482" }, { Id.ReachedLevel33, "grp.483" }, { Id.ReachedLevel34, "grp.484" }, { Id.ReachedLevel35, "grp.485" }, { Id.ReachedLevel36, "grp.486" },

                        { Id.NewbieTankMan, "grp.487"}, { Id.ExperiencedTanker, "grp.488" }, { Id.ProfessionalTanker, "grp.489" },
                        { Id.Shredder,      "grp.490"}, { Id.Killer,            "grp.491" }, { Id.Terminator,         "grp.492" },

                        { Id.Ordinary,  "grp.493"}, { Id.Sergeant,  "grp.494"},  { Id.Colonel,  "grp.495"},
                        { Id.Gambler,   "grp.496"}, { Id.Admirer,   "grp.497"},  { Id.Fans,     "grp.498"},
                        { Id.Scout,     "grp.499"}, { Id.Travelers, "grp.400"}, { Id.Wanderer, "grp.401"},
                        { Id.Survivor,  "grp.402"}
                    }
                },
                {
                    PlayServiceType.MACGameCenter,new Dictionary<Id,string>
                    {
						{ Id.ReachedLevel2,  "grp.452"}, { Id.ReachedLevel3,   "grp.453" }, { Id.ReachedLevel4,  "grp.454" }, { Id.ReachedLevel5,  "grp.455" }, { Id.ReachedLevel6,  "grp.456" },
                        { Id.ReachedLevel7,  "grp.457" }, { Id.ReachedLevel8,  "grp.458" }, { Id.ReachedLevel9,  "grp.459" }, { Id.ReachedLevel10, "grp.460" }, { Id.ReachedLevel11, "grp.461" },
                        { Id.ReachedLevel12, "grp.462" }, { Id.ReachedLevel13, "grp.463" }, { Id.ReachedLevel14, "grp.464" }, { Id.ReachedLevel15, "grp.465" }, { Id.ReachedLevel16, "grp.466" },
                        { Id.ReachedLevel17, "grp.467" }, { Id.ReachedLevel18, "grp.468" }, { Id.ReachedLevel19, "grp.469" }, { Id.ReachedLevel20, "grp.470" }, { Id.ReachedLevel21, "grp.471" },
                        { Id.ReachedLevel22, "grp.472" }, { Id.ReachedLevel23, "grp.473" }, { Id.ReachedLevel24, "grp.474" }, { Id.ReachedLevel25, "grp.475" }, { Id.ReachedLevel26, "grp.476" },
                        { Id.ReachedLevel27, "grp.477" }, { Id.ReachedLevel28, "grp.478" }, { Id.ReachedLevel29, "grp.479" }, { Id.ReachedLevel30, "grp.480" }, { Id.ReachedLevel31, "grp.481" },
                        { Id.ReachedLevel32, "grp.482" }, { Id.ReachedLevel33, "grp.483" }, { Id.ReachedLevel34, "grp.484" }, { Id.ReachedLevel35, "grp.485" }, { Id.ReachedLevel36, "grp.486" },

                        { Id.NewbieTankMan, "grp.487"}, { Id.ExperiencedTanker, "grp.488" }, { Id.ProfessionalTanker, "grp.489" },
                        { Id.Shredder,      "grp.490"}, { Id.Killer,            "grp.491" }, { Id.Terminator,         "grp.492" },

                        { Id.Ordinary,  "grp.493"}, { Id.Sergeant,  "grp.494"},  { Id.Colonel,  "grp.495"},
                        { Id.Gambler,   "grp.496"}, { Id.Admirer,   "grp.497"},  { Id.Fans,     "grp.498"},
                        { Id.Scout,     "grp.499"}, { Id.Travelers, "grp.400"}, { Id.Wanderer, "grp.401"},
                        { Id.Survivor,  "grp.402"}
                    }
                },
            }
		},
        {
            Interface.FutureTanks, new Dictionary<PlayServiceType, Dictionary<Id, string>>
            {
                {
                    PlayServiceType.GooglePlay,new Dictionary<Id,string>
                    {
                        { Id.ReachedLevel2,  "CgkI7Om0kosCEAIQEQ"}, { Id.ReachedLevel3,  "CgkI7Om0kosCEAIQEg" }, { Id.ReachedLevel4,  "CgkI7Om0kosCEAIQEw" },
                        { Id.ReachedLevel5,  "CgkI7Om0kosCEAIQFA"}, { Id.ReachedLevel6,  "CgkI7Om0kosCEAIQFQ" }, { Id.ReachedLevel7,  "CgkI7Om0kosCEAIQFg" },
                        { Id.ReachedLevel8,  "CgkI7Om0kosCEAIQFw"}, { Id.ReachedLevel9,  "CgkI7Om0kosCEAIQGA" }, { Id.ReachedLevel10, "CgkI7Om0kosCEAIQGQ" },
                        { Id.ReachedLevel11, "CgkI7Om0kosCEAIQGg"}, { Id.ReachedLevel12, "CgkI7Om0kosCEAIQGw" }, { Id.ReachedLevel13, "CgkI7Om0kosCEAIQHA" },
                        { Id.ReachedLevel14, "CgkI7Om0kosCEAIQHQ"}, { Id.ReachedLevel15, "CgkI7Om0kosCEAIQHg" }, { Id.ReachedLevel16, "CgkI7Om0kosCEAIQHw" },
                        { Id.ReachedLevel17, "CgkI7Om0kosCEAIQIA"}, { Id.ReachedLevel18, "CgkI7Om0kosCEAIQIQ" }, { Id.ReachedLevel19, "CgkI7Om0kosCEAIQIg" },
                        { Id.ReachedLevel20, "CgkI7Om0kosCEAIQIw"}, { Id.ReachedLevel21, "CgkI7Om0kosCEAIQJA" }, { Id.ReachedLevel22, "CgkI7Om0kosCEAIQJQ" },
                        { Id.ReachedLevel23, "CgkI7Om0kosCEAIQJg"}, { Id.ReachedLevel24, "CgkI7Om0kosCEAIQJw" }, { Id.ReachedLevel25, "CgkI7Om0kosCEAIQKA" },
                        { Id.ReachedLevel26, "CgkI7Om0kosCEAIQKQ"}, { Id.ReachedLevel27, "CgkI7Om0kosCEAIQKg" }, { Id.ReachedLevel28, "CgkI7Om0kosCEAIQKw" },
                        { Id.ReachedLevel29, "CgkI7Om0kosCEAIQLA"}, { Id.ReachedLevel30, "CgkI7Om0kosCEAIQLQ" }, { Id.ReachedLevel31, "CgkI7Om0kosCEAIQLg" },
                        { Id.ReachedLevel32, "CgkI7Om0kosCEAIQLw"}, { Id.ReachedLevel33, "CgkI7Om0kosCEAIQMA" }, { Id.ReachedLevel34, "CgkI7Om0kosCEAIQMQ" },
                        { Id.ReachedLevel35, "CgkI7Om0kosCEAIQMg"}, { Id.ReachedLevel36, "CgkI7Om0kosCEAIQMw" },

                        { Id.NewbieTankMan, "CgkI7Om0kosCEAIQAQ"}, { Id.ExperiencedTanker, "CgkI7Om0kosCEAIQAg" }, { Id.ProfessionalTanker, "CgkI7Om0kosCEAIQAw" },
                        { Id.Shredder,      "CgkI7Om0kosCEAIQBA"}, { Id.Killer,            "CgkI7Om0kosCEAIQBQ" }, { Id.Terminator,         "CgkI7Om0kosCEAIQBg" },

                        { Id.Ordinary,   "CgkI7Om0kosCEAIQBw"}, { Id.Sergeant,   "CgkI7Om0kosCEAIQCA" }, { Id.Colonel,    "CgkI7Om0kosCEAIQCQ" },
                        { Id.Gambler,    "CgkI7Om0kosCEAIQCg"}, { Id.Admirer,    "CgkI7Om0kosCEAIQCw" }, { Id.Fans,       "CgkI7Om0kosCEAIQDA" },
                        { Id.Scout,      "CgkI7Om0kosCEAIQDQ"}, { Id.Travelers,  "CgkI7Om0kosCEAIQDg" }, { Id.Wanderer,   "CgkI7Om0kosCEAIQDw" },
                        { Id.Survivor,   "CgkI7Om0kosCEAIQEA"}
                    }
                },
                {
                    PlayServiceType.IOSGameCenter, new Dictionary<Id, string>
                    {
                        //{ Id.ReachedLevel2,   "1" }, { Id.ReachedLevel3,   "2" }, { Id.ReachedLevel4,   "3" }, { Id.ReachedLevel5,   "4" }, { Id.ReachedLevel6,   "5" },
                        //{ Id.ReachedLevel7,   "6" }, { Id.ReachedLevel8,   "7" }, { Id.ReachedLevel9,   "8" }, { Id.ReachedLevel10,  "9" }, { Id.ReachedLevel11, "10" },
                        //{ Id.ReachedLevel12, "11" }, { Id.ReachedLevel13, "12" }, { Id.ReachedLevel14, "13" }, { Id.ReachedLevel15, "14" }, { Id.ReachedLevel16, "15" },
                        //{ Id.ReachedLevel17, "16" }, { Id.ReachedLevel18, "17" }, { Id.ReachedLevel19, "18" }, { Id.ReachedLevel20, "19" }, { Id.ReachedLevel21, "20" },
                        //{ Id.ReachedLevel22, "21" }, { Id.ReachedLevel23, "22" }, { Id.ReachedLevel24, "23" }, { Id.ReachedLevel25, "24" }, { Id.ReachedLevel26, "25" },
                        //{ Id.ReachedLevel27, "26" }, { Id.ReachedLevel28, "27" }, { Id.ReachedLevel29, "28" }, { Id.ReachedLevel30, "29" }, { Id.ReachedLevel31, "30" },
                        //{ Id.ReachedLevel32, "31" }, { Id.ReachedLevel33, "32" }, { Id.ReachedLevel34, "33" }, { Id.ReachedLevel35, "34" }, { Id.ReachedLevel36, "35" },

                        //{ Id.NewbieTankMan, "36" }, { Id.ExperiencedTanker, "37" }, { Id.ProfessionalTanker, "38" },
                        //{ Id.Shredder,      "39" }, { Id.Killer,            "40" }, { Id.Terminator,         "41" },

                        //{ Id.Ordinary,  "42" }, { Id.Sergeant,  "43" }, { Id.Colonel,   "44" }, { Id.Gambler,  "45"}, { Id.Admirer,    "46" },
                        //{ Id.Fans,      "47" }, { Id.Scout,     "48" }, { Id.Travelers, "49" }, { Id.Wanderer, "50"}, { Id.Survivor,   "51" }
                        { Id.ReachedLevel2,   "grp.501" }, { Id.ReachedLevel3,   "grp.502" }, { Id.ReachedLevel4,   "grp.503" }, { Id.ReachedLevel5,   "grp.504" }, { Id.ReachedLevel6,   "grp.505" },
                        { Id.ReachedLevel7,   "grp.506" }, { Id.ReachedLevel8,   "grp.507" }, { Id.ReachedLevel9,   "grp.508" }, { Id.ReachedLevel10,  "grp.509" }, { Id.ReachedLevel11, "grp.510" },
                        { Id.ReachedLevel12, "grp.511" }, { Id.ReachedLevel13, "grp.512" }, { Id.ReachedLevel14, "grp.513" }, { Id.ReachedLevel15, "grp.514" }, { Id.ReachedLevel16, "grp.515" },
                        { Id.ReachedLevel17, "grp.516" }, { Id.ReachedLevel18, "grp.517" }, { Id.ReachedLevel19, "grp.518" }, { Id.ReachedLevel20, "grp.519" }, { Id.ReachedLevel21, "grp.520" },
                        { Id.ReachedLevel22, "grp.521" }, { Id.ReachedLevel23, "grp.522" }, { Id.ReachedLevel24, "grp.523" }, { Id.ReachedLevel25, "grp.524" }, { Id.ReachedLevel26, "grp.525" },
                        { Id.ReachedLevel27, "grp.526" }, { Id.ReachedLevel28, "grp.527" }, { Id.ReachedLevel29, "grp.528" }, { Id.ReachedLevel30, "grp.529" }, { Id.ReachedLevel31, "grp.530" },
                        { Id.ReachedLevel32, "grp.531" }, { Id.ReachedLevel33, "grp.532" }, { Id.ReachedLevel34, "grp.533" }, { Id.ReachedLevel35, "grp.534" }, { Id.ReachedLevel36, "grp.535" },

                        { Id.NewbieTankMan, "grp.536" }, { Id.ExperiencedTanker, "grp.537" }, { Id.ProfessionalTanker, "grp.538" },
                        { Id.Shredder,      "grp.539" }, { Id.Killer,            "grp.540" }, { Id.Terminator,         "grp.541" },

                        { Id.Ordinary,  "grp.542" }, { Id.Sergeant,  "grp.543" }, { Id.Colonel,   "grp.544" }, { Id.Gambler,  "grp.545"}, { Id.Admirer,    "grp.546" },
                        { Id.Fans,      "grp.547" }, { Id.Scout,     "grp.548" }, { Id.Travelers, "grp.549" }, { Id.Wanderer, "grp.550"}, { Id.Survivor,   "grp.551" }
                    }
                    
                },
                {
                    PlayServiceType.MACGameCenter, new Dictionary<Id, string>
                    {
                        { Id.ReachedLevel2,   "grp.501" }, { Id.ReachedLevel3,   "grp.502" }, { Id.ReachedLevel4,   "grp.503" }, { Id.ReachedLevel5,   "grp.504" }, { Id.ReachedLevel6,   "grp.505" },
                        { Id.ReachedLevel7,   "grp.506" }, { Id.ReachedLevel8,   "grp.507" }, { Id.ReachedLevel9,   "grp.508" }, { Id.ReachedLevel10,  "grp.509" }, { Id.ReachedLevel11, "grp.510" },
                        { Id.ReachedLevel12, "grp.511" }, { Id.ReachedLevel13, "grp.512" }, { Id.ReachedLevel14, "grp.513" }, { Id.ReachedLevel15, "grp.514" }, { Id.ReachedLevel16, "grp.515" },
                        { Id.ReachedLevel17, "grp.516" }, { Id.ReachedLevel18, "grp.517" }, { Id.ReachedLevel19, "grp.518" }, { Id.ReachedLevel20, "grp.519" }, { Id.ReachedLevel21, "grp.520" },
                        { Id.ReachedLevel22, "grp.521" }, { Id.ReachedLevel23, "grp.522" }, { Id.ReachedLevel24, "grp.523" }, { Id.ReachedLevel25, "grp.524" }, { Id.ReachedLevel26, "grp.525" },
                        { Id.ReachedLevel27, "grp.526" }, { Id.ReachedLevel28, "grp.527" }, { Id.ReachedLevel29, "grp.528" }, { Id.ReachedLevel30, "grp.529" }, { Id.ReachedLevel31, "grp.530" },
                        { Id.ReachedLevel32, "grp.531" }, { Id.ReachedLevel33, "grp.532" }, { Id.ReachedLevel34, "grp.533" }, { Id.ReachedLevel35, "grp.534" }, { Id.ReachedLevel36, "grp.535" },

                        { Id.NewbieTankMan, "grp.536" }, { Id.ExperiencedTanker, "grp.537" }, { Id.ProfessionalTanker, "grp.538" },
                        { Id.Shredder,      "grp.539" }, { Id.Killer,            "grp.540" }, { Id.Terminator,         "grp.541" },

                        { Id.Ordinary,  "grp.542" }, { Id.Sergeant,  "grp.543" }, { Id.Colonel,   "grp.544" }, { Id.Gambler,  "grp.545"}, { Id.Admirer,    "grp.546" },
                        { Id.Fans,      "grp.547" }, { Id.Scout,     "grp.548" }, { Id.Travelers, "grp.549" }, { Id.Wanderer, "grp.550"}, { Id.Survivor,   "grp.551" }
                    }
                },
            }
        },
        {
            Interface.ToonWars, new Dictionary<PlayServiceType, Dictionary<Id, string>> 
            {
                {
                    PlayServiceType.GooglePlay, new Dictionary<Id, string>
                    {
                        { Id.ReachedLevel2,  "CgkIy9i9sKkXEAIQEA" }, { Id.ReachedLevel3,  "CgkIy9i9sKkXEAIQEQ" }, { Id.ReachedLevel4,  "CgkIy9i9sKkXEAIQEg" },
                        { Id.ReachedLevel5,  "CgkIy9i9sKkXEAIQEw" }, { Id.ReachedLevel6,  "CgkIy9i9sKkXEAIQFA" }, { Id.ReachedLevel7,  "CgkIy9i9sKkXEAIQFQ" },
                        { Id.ReachedLevel8,  "CgkIy9i9sKkXEAIQFg" }, { Id.ReachedLevel9,  "CgkIy9i9sKkXEAIQFw" }, { Id.ReachedLevel10, "CgkIy9i9sKkXEAIQGA" },
                        { Id.ReachedLevel11, "CgkIy9i9sKkXEAIQGQ" }, { Id.ReachedLevel12, "CgkIy9i9sKkXEAIQGg" }, { Id.ReachedLevel13, "CgkIy9i9sKkXEAIQGw" },
                        { Id.ReachedLevel14, "CgkIy9i9sKkXEAIQHA" }, { Id.ReachedLevel15, "CgkIy9i9sKkXEAIQHQ" }, { Id.ReachedLevel16, "CgkIy9i9sKkXEAIQHg" },
                        { Id.ReachedLevel17, "CgkIy9i9sKkXEAIQHw" }, { Id.ReachedLevel18, "CgkIy9i9sKkXEAIQIA" }, { Id.ReachedLevel19, "CgkIy9i9sKkXEAIQIQ" },
                        { Id.ReachedLevel20, "CgkIy9i9sKkXEAIQIg" }, { Id.ReachedLevel21, "CgkIy9i9sKkXEAIQIw" }, { Id.ReachedLevel22, "CgkIy9i9sKkXEAIQJA" },
                        { Id.ReachedLevel23, "CgkIy9i9sKkXEAIQJQ" }, { Id.ReachedLevel24, "CgkIy9i9sKkXEAIQJg" }, { Id.ReachedLevel25, "CgkIy9i9sKkXEAIQJw" },
                        { Id.ReachedLevel26, "CgkIy9i9sKkXEAIQKA" }, { Id.ReachedLevel27, "CgkIy9i9sKkXEAIQKQ" }, { Id.ReachedLevel28, "CgkIy9i9sKkXEAIQKg" },
                        { Id.ReachedLevel29, "CgkIy9i9sKkXEAIQKw" }, { Id.ReachedLevel30, "CgkIy9i9sKkXEAIQLA" }, { Id.ReachedLevel31, "CgkIy9i9sKkXEAIQLQ" },
                        { Id.ReachedLevel32, "CgkIy9i9sKkXEAIQLg" }, { Id.ReachedLevel33, "CgkIy9i9sKkXEAIQLw" }, { Id.ReachedLevel34, "CgkIy9i9sKkXEAIQMA" },
                        { Id.ReachedLevel35, "CgkIy9i9sKkXEAIQMQ" }, { Id.ReachedLevel36, "CgkIy9i9sKkXEAIQMg" },
                        
                        { Id.NewbieTankMan, "CgkIy9i9sKkXEAIQAA"}, { Id.ExperiencedTanker, "CgkIy9i9sKkXEAIQAQ" }, { Id.ProfessionalTanker, "CgkIy9i9sKkXEAIQAg" },
                        { Id.Shredder,      "CgkIy9i9sKkXEAIQAw"}, { Id.Killer,            "CgkIy9i9sKkXEAIQBA" }, { Id.Terminator,         "CgkIy9i9sKkXEAIQBQ" },
                        
                        { Id.Ordinary, "CgkIy9i9sKkXEAIQBg" }, { Id.Sergeant,  "CgkIy9i9sKkXEAIQBw" }, {Id.Colonel,  "CgkIy9i9sKkXEAIQCA"},
                        { Id.Gambler,  "CgkIy9i9sKkXEAIQCQ" }, { Id.Admirer,   "CgkIy9i9sKkXEAIQCg" }, {Id.Fans,     "CgkIy9i9sKkXEAIQCw"},
                        { Id.Scout,    "CgkIy9i9sKkXEAIQDA" }, { Id.Travelers, "CgkIy9i9sKkXEAIQDQ" }, {Id.Wanderer, "CgkIy9i9sKkXEAIQDg"},
                        { Id.Survivor, "CgkIy9i9sKkXEAIQDw" }
                    }
                },
                {
                    PlayServiceType.IOSGameCenter,new Dictionary<Id,string>
                    {
                        //{ Id.ReachedLevel2,  "200" }, { Id.ReachedLevel3,  "201" }, { Id.ReachedLevel4,  "202" }, { Id.ReachedLevel5,  "203" }, { Id.ReachedLevel6,  "204" },
                        //{ Id.ReachedLevel7,  "205" }, { Id.ReachedLevel8,  "206" }, { Id.ReachedLevel9,  "207" }, { Id.ReachedLevel10, "208" }, { Id.ReachedLevel11, "209" },
                        //{ Id.ReachedLevel12, "210" }, { Id.ReachedLevel13, "211" }, { Id.ReachedLevel14, "212" }, { Id.ReachedLevel15, "213" }, { Id.ReachedLevel16, "214" },
                        //{ Id.ReachedLevel17, "215" }, { Id.ReachedLevel18, "216" }, { Id.ReachedLevel19, "217" }, { Id.ReachedLevel20, "218" }, { Id.ReachedLevel21, "219" },
                        //{ Id.ReachedLevel22, "220" }, { Id.ReachedLevel23, "221" }, { Id.ReachedLevel24, "222" }, { Id.ReachedLevel25, "223" }, { Id.ReachedLevel26, "224" },
                        //{ Id.ReachedLevel27, "225" }, { Id.ReachedLevel28, "226" }, { Id.ReachedLevel29, "227" }, { Id.ReachedLevel30, "228" }, { Id.ReachedLevel31, "229" },
                        //{ Id.ReachedLevel32, "230" }, { Id.ReachedLevel33, "231" }, { Id.ReachedLevel34, "232" }, { Id.ReachedLevel35, "233" }, { Id.ReachedLevel36, "234" },

                        //{ Id.NewbieTankMan, "235"}, { Id.ExperiencedTanker, "236" }, { Id.ProfessionalTanker, "237" },
                        //{ Id.Shredder,      "238"}, { Id.Killer,            "239" }, { Id.Terminator,         "240" },

                        //{ Id.Ordinary,  "241"}, { Id.Sergeant,  "242"},  { Id.Colonel,  "243"},
                        //{ Id.Gambler,   "244"}, { Id.Admirer,   "245"},  { Id.Fans,     "246"},
                        //{ Id.Scout,     "247"}, { Id.Travelers, "248"}, { Id.Wanderer, "249"},
                        //{ Id.Survivor,  "250"}
                        { Id.ReachedLevel2,  "grp.652" }, { Id.ReachedLevel3,  "grp.653" }, { Id.ReachedLevel4,  "grp.654" }, { Id.ReachedLevel5,  "grp.603" }, { Id.ReachedLevel6,  "grp.604" },
                        { Id.ReachedLevel7,  "grp.605" }, { Id.ReachedLevel8,  "grp.606" }, { Id.ReachedLevel9,  "grp.607" }, { Id.ReachedLevel10, "grp.608" }, { Id.ReachedLevel11, "grp.609" },
                        { Id.ReachedLevel12, "grp.610" }, { Id.ReachedLevel13, "grp.611" }, { Id.ReachedLevel14, "grp.612" }, { Id.ReachedLevel15, "grp.613" }, { Id.ReachedLevel16, "grp.614" },
                        { Id.ReachedLevel17, "grp.615" }, { Id.ReachedLevel18, "grp.616" }, { Id.ReachedLevel19, "grp.617" }, { Id.ReachedLevel20, "grp.618" }, { Id.ReachedLevel21, "grp.619" },
                        { Id.ReachedLevel22, "grp.620" }, { Id.ReachedLevel23, "grp.621" }, { Id.ReachedLevel24, "grp.622" }, { Id.ReachedLevel25, "grp.623" }, { Id.ReachedLevel26, "grp.624" },
                        { Id.ReachedLevel27, "grp.625" }, { Id.ReachedLevel28, "grp.626" }, { Id.ReachedLevel29, "grp.627" }, { Id.ReachedLevel30, "grp.628" }, { Id.ReachedLevel31, "grp.629" },
                        { Id.ReachedLevel32, "grp.630" }, { Id.ReachedLevel33, "grp.631" }, { Id.ReachedLevel34, "grp.632" }, { Id.ReachedLevel35, "grp.633" }, { Id.ReachedLevel36, "grp.634" },

                        { Id.NewbieTankMan, "grp.535"}, { Id.ExperiencedTanker, "grp.536" }, { Id.ProfessionalTanker, "grp.537" },
                        { Id.Shredder,      "grp.538"}, { Id.Killer,            "grp.539" }, { Id.Terminator,         "grp.540" },

                        { Id.Ordinary,  "grp.641"}, { Id.Sergeant,  "grp.642"},  { Id.Colonel,  "grp.643"},
                        { Id.Gambler,   "grp.644"}, { Id.Admirer,   "grp.645"},  { Id.Fans,     "grp.646"},
                        { Id.Scout,     "grp.647"}, { Id.Travelers, "grp.648"}, { Id.Wanderer, "grp.649"},
                        { Id.Survivor,  "grp.650"}
                    }
                },
                {
                    PlayServiceType.MACGameCenter,new Dictionary<Id,string>//Гребаный Эпл не дал назвать с 600 мол такой ид уже есть,хотя он блять не занят никем
                    {
                        { Id.ReachedLevel2,  "grp.652" }, { Id.ReachedLevel3,  "grp.653" }, { Id.ReachedLevel4,  "grp.654" }, { Id.ReachedLevel5,  "grp.603" }, { Id.ReachedLevel6,  "grp.604" },
                        { Id.ReachedLevel7,  "grp.605" }, { Id.ReachedLevel8,  "grp.606" }, { Id.ReachedLevel9,  "grp.607" }, { Id.ReachedLevel10, "grp.608" }, { Id.ReachedLevel11, "grp.609" },
                        { Id.ReachedLevel12, "grp.610" }, { Id.ReachedLevel13, "grp.611" }, { Id.ReachedLevel14, "grp.612" }, { Id.ReachedLevel15, "grp.613" }, { Id.ReachedLevel16, "grp.614" },
                        { Id.ReachedLevel17, "grp.615" }, { Id.ReachedLevel18, "grp.616" }, { Id.ReachedLevel19, "grp.617" }, { Id.ReachedLevel20, "grp.618" }, { Id.ReachedLevel21, "grp.619" },
                        { Id.ReachedLevel22, "grp.620" }, { Id.ReachedLevel23, "grp.621" }, { Id.ReachedLevel24, "grp.622" }, { Id.ReachedLevel25, "grp.623" }, { Id.ReachedLevel26, "grp.624" },
                        { Id.ReachedLevel27, "grp.625" }, { Id.ReachedLevel28, "grp.626" }, { Id.ReachedLevel29, "grp.627" }, { Id.ReachedLevel30, "grp.628" }, { Id.ReachedLevel31, "grp.629" },
                        { Id.ReachedLevel32, "grp.630" }, { Id.ReachedLevel33, "grp.631" }, { Id.ReachedLevel34, "grp.632" }, { Id.ReachedLevel35, "grp.633" }, { Id.ReachedLevel36, "grp.634" },
                        
                        { Id.NewbieTankMan, "grp.535"}, { Id.ExperiencedTanker, "grp.536" }, { Id.ProfessionalTanker, "grp.537" },
                        { Id.Shredder,      "grp.538"}, { Id.Killer,            "grp.539" }, { Id.Terminator,         "grp.540" },
                        
                        { Id.Ordinary,  "grp.641"}, { Id.Sergeant,  "grp.642"},  { Id.Colonel,  "grp.643"},
                        { Id.Gambler,   "grp.644"}, { Id.Admirer,   "grp.645"},  { Id.Fans,     "grp.646"},
                        { Id.Scout,     "grp.647"}, { Id.Travelers, "grp.648"}, { Id.Wanderer, "grp.649"},
                        { Id.Survivor,  "grp.650"} 
                    }
                },
            }
        },


        {
            Interface.SpaceJet, new Dictionary<PlayServiceType, Dictionary<Id, string>>
            {
                {
                    PlayServiceType.GooglePlay, new Dictionary<Id, string>
                    {
                        { Id.ReachedLevel2,  "CgkItvrh7-cHEAIQFA" }, { Id.ReachedLevel3,  "CgkItvrh7-cHEAIQFQ" }, { Id.ReachedLevel4,  "CgkItvrh7-cHEAIQFg" },
                        { Id.ReachedLevel5,  "CgkItvrh7-cHEAIQFw" }, { Id.ReachedLevel6,  "CgkItvrh7-cHEAIQGA" }, { Id.ReachedLevel7,  "CgkItvrh7-cHEAIQGQ" },
                        { Id.ReachedLevel8,  "CgkItvrh7-cHEAIQGg" }, { Id.ReachedLevel9,  "CgkItvrh7-cHEAIQGw" }, { Id.ReachedLevel10, "CgkItvrh7-cHEAIQHA" },
                        { Id.ReachedLevel11, "CgkItvrh7-cHEAIQHQ" }, { Id.ReachedLevel12, "CgkItvrh7-cHEAIQHg" }, { Id.ReachedLevel13, "CgkItvrh7-cHEAIQHw" },
                        { Id.ReachedLevel14, "CgkItvrh7-cHEAIQIA" }, { Id.ReachedLevel15, "CgkItvrh7-cHEAIQIQ" }, { Id.ReachedLevel16, "CgkItvrh7-cHEAIQIg" },
                        { Id.ReachedLevel17, "CgkItvrh7-cHEAIQIw" }, { Id.ReachedLevel18, "CgkItvrh7-cHEAIQJA" }, { Id.ReachedLevel19, "CgkItvrh7-cHEAIQJQ" },
                        { Id.ReachedLevel20, "CgkItvrh7-cHEAIQJg" }, { Id.ReachedLevel21, "CgkItvrh7-cHEAIQJw" }, { Id.ReachedLevel22, "CgkItvrh7-cHEAIQKA" },
                        { Id.ReachedLevel23, "CgkItvrh7-cHEAIQKQ" }, { Id.ReachedLevel24, "CgkItvrh7-cHEAIQKg" }, { Id.ReachedLevel25, "CgkItvrh7-cHEAIQKw" },
                        { Id.ReachedLevel26, "CgkItvrh7-cHEAIQLA" }, { Id.ReachedLevel27, "CgkItvrh7-cHEAIQLQ" }, { Id.ReachedLevel28, "CgkItvrh7-cHEAIQLg" },
                        { Id.ReachedLevel29, "CgkItvrh7-cHEAIQLw" }, { Id.ReachedLevel30, "CgkItvrh7-cHEAIQMA" }, { Id.ReachedLevel31, "CgkItvrh7-cHEAIQMQ" },
                        { Id.ReachedLevel32, "CgkItvrh7-cHEAIQMg" }, { Id.ReachedLevel33, "CgkItvrh7-cHEAIQMw" }, { Id.ReachedLevel34, "CgkItvrh7-cHEAIQNA" },
                        { Id.ReachedLevel35, "CgkItvrh7-cHEAIQNQ" }, { Id.ReachedLevel36, "CgkItvrh7-cHEAIQNg" },

                        { Id.NewbieTankMan, "CgkItvrh7-cHEAIQBA"}, { Id.ExperiencedTanker, "CgkItvrh7-cHEAIQBQ" }, { Id.ProfessionalTanker, "CgkItvrh7-cHEAIQBg" },
                        { Id.Shredder,      "CgkItvrh7-cHEAIQBw"}, { Id.Killer,            "CgkItvrh7-cHEAIQCA" }, { Id.Terminator,         "CgkItvrh7-cHEAIQCQ" },

                        { Id.Ordinary, "CgkItvrh7-cHEAIQCg" }, { Id.Sergeant,  "CgkItvrh7-cHEAIQCw" }, {Id.Colonel,  "CgkItvrh7-cHEAIQDA"},
                        { Id.Gambler,  "CgkItvrh7-cHEAIQDQ" }, { Id.Admirer,   "CgkItvrh7-cHEAIQDg" }, {Id.Fans,     "CgkItvrh7-cHEAIQDw"},
                        { Id.Scout,    "CgkItvrh7-cHEAIQEA" }, { Id.Travelers, "CgkItvrh7-cHEAIQEQ" }, {Id.Wanderer, "CgkItvrh7-cHEAIQEg"},
                        { Id.Survivor, "CgkItvrh7-cHEAIQEw" }
                    }
                },
                {
                    PlayServiceType.IOSGameCenter,new Dictionary<Id,string>
                    {
                        //{ Id.ReachedLevel2,  "300" }, { Id.ReachedLevel3,  "301" }, { Id.ReachedLevel4,  "302" }, { Id.ReachedLevel5,  "303" }, { Id.ReachedLevel6,  "304" },
                        //{ Id.ReachedLevel7,  "305" }, { Id.ReachedLevel8,  "306" }, { Id.ReachedLevel9,  "307" }, { Id.ReachedLevel10, "308" }, { Id.ReachedLevel11, "309" },
                        //{ Id.ReachedLevel12, "310" }, { Id.ReachedLevel13, "311" }, { Id.ReachedLevel14, "312" }, { Id.ReachedLevel15, "313" }, { Id.ReachedLevel16, "314" },
                        //{ Id.ReachedLevel17, "315" }, { Id.ReachedLevel18, "316" }, { Id.ReachedLevel19, "317" }, { Id.ReachedLevel20, "318" }, { Id.ReachedLevel21, "319" },
                        //{ Id.ReachedLevel22, "320" }, { Id.ReachedLevel23, "321" }, { Id.ReachedLevel24, "322" }, { Id.ReachedLevel25, "323" }, { Id.ReachedLevel26, "324" },
                        //{ Id.ReachedLevel27, "325" }, { Id.ReachedLevel28, "326" }, { Id.ReachedLevel29, "327" }, { Id.ReachedLevel30, "328" }, { Id.ReachedLevel31, "329" },
                        //{ Id.ReachedLevel32, "330" }, { Id.ReachedLevel33, "331" }, { Id.ReachedLevel34, "332" }, { Id.ReachedLevel35, "333" }, { Id.ReachedLevel36, "334" },

                        //{ Id.NewbieTankMan, "335"}, { Id.ExperiencedTanker, "336" }, { Id.ProfessionalTanker, "337" },
                        //{ Id.Shredder,      "338"}, { Id.Killer,            "339" }, { Id.Terminator,         "340" },

                        //{ Id.Ordinary,  "341"}, { Id.Sergeant,  "342"},  { Id.Colonel,  "343"},
                        //{ Id.Gambler,   "344"}, { Id.Admirer,   "345"},  { Id.Fans,     "346"},
                        //{ Id.Scout,     "347"}, { Id.Travelers, "348"}, { Id.Wanderer, "349"},
                        //{ Id.Survivor,  "350"}
                        { Id.ReachedLevel2,  "grp.700" }, { Id.ReachedLevel3,  "grp.701" }, { Id.ReachedLevel4,  "grp.702" }, { Id.ReachedLevel5,  "grp.703" }, { Id.ReachedLevel6,  "grp.704" },
                        { Id.ReachedLevel7,  "grp.705" }, { Id.ReachedLevel8,  "grp.706" }, { Id.ReachedLevel9,  "grp.707" }, { Id.ReachedLevel10, "grp.708" }, { Id.ReachedLevel11, "grp.709" },
                        { Id.ReachedLevel12, "grp.710" }, { Id.ReachedLevel13, "grp.711" }, { Id.ReachedLevel14, "grp.712" }, { Id.ReachedLevel15, "grp.713" }, { Id.ReachedLevel16, "grp.714" },
                        { Id.ReachedLevel17, "grp.715" }, { Id.ReachedLevel18, "grp.716" }, { Id.ReachedLevel19, "grp.717" }, { Id.ReachedLevel20, "grp.718" }, { Id.ReachedLevel21, "grp.719" },
                        { Id.ReachedLevel22, "grp.720" }, { Id.ReachedLevel23, "grp.721" }, { Id.ReachedLevel24, "grp.722" }, { Id.ReachedLevel25, "grp.723" }, { Id.ReachedLevel26, "grp.724" },
                        { Id.ReachedLevel27, "grp.725" }, { Id.ReachedLevel28, "grp.726" }, { Id.ReachedLevel29, "grp.727" }, { Id.ReachedLevel30, "grp.728" }, { Id.ReachedLevel31, "grp.729" },
                        { Id.ReachedLevel32, "grp.730" }, { Id.ReachedLevel33, "grp.731" }, { Id.ReachedLevel34, "grp.732" }, { Id.ReachedLevel35, "grp.733" }, { Id.ReachedLevel36, "grp.734" },

                        { Id.NewbieTankMan, "grp.735"}, { Id.ExperiencedTanker, "grp.736" }, { Id.ProfessionalTanker, "grp.737" },
                        { Id.Shredder,      "grp.738"}, { Id.Killer,            "grp.739" }, { Id.Terminator,         "grp.740" },

                        { Id.Ordinary,  "grp.741"}, { Id.Sergeant,  "grp.742"},  { Id.Colonel,  "grp.743"},
                        { Id.Gambler,   "grp.744"}, { Id.Admirer,   "grp.745"},  { Id.Fans,     "grp.746"},
                        { Id.Scout,     "grp.747"}, { Id.Travelers, "grp.748"}, { Id.Wanderer, "grp.749"},
                        { Id.Survivor,  "grp.750"}
                    }
                },
                {
                    PlayServiceType.MACGameCenter,new Dictionary<Id,string>
                    {
                        { Id.ReachedLevel2,  "grp.700" }, { Id.ReachedLevel3,  "grp.701" }, { Id.ReachedLevel4,  "grp.702" }, { Id.ReachedLevel5,  "grp.703" }, { Id.ReachedLevel6,  "grp.704" },
                        { Id.ReachedLevel7,  "grp.705" }, { Id.ReachedLevel8,  "grp.706" }, { Id.ReachedLevel9,  "grp.707" }, { Id.ReachedLevel10, "grp.708" }, { Id.ReachedLevel11, "grp.709" },
                        { Id.ReachedLevel12, "grp.710" }, { Id.ReachedLevel13, "grp.711" }, { Id.ReachedLevel14, "grp.712" }, { Id.ReachedLevel15, "grp.713" }, { Id.ReachedLevel16, "grp.714" },
                        { Id.ReachedLevel17, "grp.715" }, { Id.ReachedLevel18, "grp.716" }, { Id.ReachedLevel19, "grp.717" }, { Id.ReachedLevel20, "grp.718" }, { Id.ReachedLevel21, "grp.719" },
                        { Id.ReachedLevel22, "grp.720" }, { Id.ReachedLevel23, "grp.721" }, { Id.ReachedLevel24, "grp.722" }, { Id.ReachedLevel25, "grp.723" }, { Id.ReachedLevel26, "grp.724" },
                        { Id.ReachedLevel27, "grp.725" }, { Id.ReachedLevel28, "grp.726" }, { Id.ReachedLevel29, "grp.727" }, { Id.ReachedLevel30, "grp.728" }, { Id.ReachedLevel31, "grp.729" },
                        { Id.ReachedLevel32, "grp.730" }, { Id.ReachedLevel33, "grp.731" }, { Id.ReachedLevel34, "grp.732" }, { Id.ReachedLevel35, "grp.733" }, { Id.ReachedLevel36, "grp.734" },

                        { Id.NewbieTankMan, "grp.735"}, { Id.ExperiencedTanker, "grp.736" }, { Id.ProfessionalTanker, "grp.737" },
                        { Id.Shredder,      "grp.738"}, { Id.Killer,            "grp.739" }, { Id.Terminator,         "grp.740" },

                        { Id.Ordinary,  "grp.741"}, { Id.Sergeant,  "grp.742"},  { Id.Colonel,  "grp.743"},
                        { Id.Gambler,   "grp.744"}, { Id.Admirer,   "grp.745"},  { Id.Fans,     "grp.746"},
                        { Id.Scout,     "grp.747"}, { Id.Travelers, "grp.748"}, { Id.Wanderer, "grp.749"},
                        { Id.Survivor,  "grp.750"}
                    }
                },
            }
        },


        {
            Interface.BlowOut, new Dictionary<PlayServiceType, Dictionary<Id, string>>
            {
                {
                    PlayServiceType.GooglePlay, new Dictionary<Id, string>
                    {
                        { Id.ReachedLevel2,  "" }, { Id.ReachedLevel3,  "" }, { Id.ReachedLevel4,  "" },
                        { Id.ReachedLevel5,  "" }, { Id.ReachedLevel6,  "" }, { Id.ReachedLevel7,  "" },
                        { Id.ReachedLevel8,  "" }, { Id.ReachedLevel9,  "" }, { Id.ReachedLevel10, "" },
                        { Id.ReachedLevel11, "" }, { Id.ReachedLevel12, "" }, { Id.ReachedLevel13, "" },
                        { Id.ReachedLevel14, "" }, { Id.ReachedLevel15, "" }, { Id.ReachedLevel16, "" },
                        { Id.ReachedLevel17, "" }, { Id.ReachedLevel18, "" }, { Id.ReachedLevel19, "" },
                        { Id.ReachedLevel20, "" }, { Id.ReachedLevel21, "" }, { Id.ReachedLevel22, "" },
                        { Id.ReachedLevel23, "" }, { Id.ReachedLevel24, "" }, { Id.ReachedLevel25, "" },
                        { Id.ReachedLevel26, "" }, { Id.ReachedLevel27, "" }, { Id.ReachedLevel28, "" },
                        { Id.ReachedLevel29, "" }, { Id.ReachedLevel30, "" }, { Id.ReachedLevel31, "" },
                        { Id.ReachedLevel32, "" }, { Id.ReachedLevel33, "" }, { Id.ReachedLevel34, "" },
                        { Id.ReachedLevel35, "" }, { Id.ReachedLevel36, "" },

                        { Id.NewbieTankMan, ""}, { Id.ExperiencedTanker, "" }, { Id.ProfessionalTanker, "" },
                        { Id.Shredder,      ""}, { Id.Killer,            "" }, { Id.Terminator,         "" },

                        { Id.Ordinary, "" }, { Id.Sergeant,  "" }, {Id.Colonel,  ""},
                        { Id.Gambler,  "" }, { Id.Admirer,   "" }, {Id.Fans,     ""},
                        { Id.Scout,    "" }, { Id.Travelers, "" }, {Id.Wanderer, ""},
                        { Id.Survivor, "" }
                    }
                },
                {
                    PlayServiceType.IOSGameCenter,new Dictionary<Id,string>
                    {
                        { Id.ReachedLevel2,  "400" }, { Id.ReachedLevel3,  "401" }, { Id.ReachedLevel4,  "402" }, { Id.ReachedLevel5,  "403" }, { Id.ReachedLevel6,  "404" },
                        { Id.ReachedLevel7,  "405" }, { Id.ReachedLevel8,  "406" }, { Id.ReachedLevel9,  "407" }, { Id.ReachedLevel10, "408" }, { Id.ReachedLevel11, "409" },
                        { Id.ReachedLevel12, "410" }, { Id.ReachedLevel13, "411" }, { Id.ReachedLevel14, "412" }, { Id.ReachedLevel15, "413" }, { Id.ReachedLevel16, "414" },
                        { Id.ReachedLevel17, "415" }, { Id.ReachedLevel18, "416" }, { Id.ReachedLevel19, "417" }, { Id.ReachedLevel20, "418" }, { Id.ReachedLevel21, "419" },
                        { Id.ReachedLevel22, "420" }, { Id.ReachedLevel23, "421" }, { Id.ReachedLevel24, "422" }, { Id.ReachedLevel25, "423" }, { Id.ReachedLevel26, "424" },
                        { Id.ReachedLevel27, "425" }, { Id.ReachedLevel28, "426" }, { Id.ReachedLevel29, "427" }, { Id.ReachedLevel30, "428" }, { Id.ReachedLevel31, "429" },
                        { Id.ReachedLevel32, "430" }, { Id.ReachedLevel33, "431" }, { Id.ReachedLevel34, "432" }, { Id.ReachedLevel35, "433" }, { Id.ReachedLevel36, "434" },

                        { Id.NewbieTankMan, "435"}, { Id.ExperiencedTanker, "436" }, { Id.ProfessionalTanker, "437" },
                        { Id.Shredder,      "438"}, { Id.Killer,            "439" }, { Id.Terminator,         "440" },

                        { Id.Ordinary,  "441"}, { Id.Sergeant,  "442"},  { Id.Colonel,  "443"},
                        { Id.Gambler,   "444"}, { Id.Admirer,   "445"},  { Id.Fans,     "446"},
                        { Id.Scout,     "447"}, { Id.Travelers, "448"}, { Id.Wanderer, "449"},
                        { Id.Survivor,  "450"}
                    }
                },
                {
                    PlayServiceType.MACGameCenter,new Dictionary<Id,string>
                    {
                        { Id.ReachedLevel2,  "400" }, { Id.ReachedLevel3,  "401" }, { Id.ReachedLevel4,  "402" }, { Id.ReachedLevel5,  "403" }, { Id.ReachedLevel6,  "404" },
                        { Id.ReachedLevel7,  "405" }, { Id.ReachedLevel8,  "406" }, { Id.ReachedLevel9,  "407" }, { Id.ReachedLevel10, "408" }, { Id.ReachedLevel11, "409" },
                        { Id.ReachedLevel12, "410" }, { Id.ReachedLevel13, "411" }, { Id.ReachedLevel14, "412" }, { Id.ReachedLevel15, "413" }, { Id.ReachedLevel16, "414" },
                        { Id.ReachedLevel17, "415" }, { Id.ReachedLevel18, "416" }, { Id.ReachedLevel19, "417" }, { Id.ReachedLevel20, "418" }, { Id.ReachedLevel21, "419" },
                        { Id.ReachedLevel22, "420" }, { Id.ReachedLevel23, "421" }, { Id.ReachedLevel24, "422" }, { Id.ReachedLevel25, "423" }, { Id.ReachedLevel26, "424" },
                        { Id.ReachedLevel27, "425" }, { Id.ReachedLevel28, "426" }, { Id.ReachedLevel29, "427" }, { Id.ReachedLevel30, "428" }, { Id.ReachedLevel31, "429" },
                        { Id.ReachedLevel32, "430" }, { Id.ReachedLevel33, "431" }, { Id.ReachedLevel34, "432" }, { Id.ReachedLevel35, "433" }, { Id.ReachedLevel36, "434" },

                        { Id.NewbieTankMan, "435"}, { Id.ExperiencedTanker, "436" }, { Id.ProfessionalTanker, "437" },
                        { Id.Shredder,      "438"}, { Id.Killer,            "439" }, { Id.Terminator,         "440" },

                        { Id.Ordinary,  "441"}, { Id.Sergeant,  "442"},  { Id.Colonel,  "443"},
                        { Id.Gambler,   "444"}, { Id.Admirer,   "445"},  { Id.Fans,     "446"},
                        { Id.Scout,     "447"}, { Id.Travelers, "448"}, { Id.Wanderer, "449"},
                        { Id.Survivor,  "450"}
                    }
                },
            }
        },

        {
            Interface.BattleOfWarplanes, new Dictionary<PlayServiceType, Dictionary<Id, string>>
            {
                {
                    PlayServiceType.GooglePlay, new Dictionary<Id, string>
                    {
                        { Id.ReachedLevel2,  "CgkI-fuSxP8aEAIQEA" }, { Id.ReachedLevel3,  "CgkI-fuSxP8aEAIQEQ" }, { Id.ReachedLevel4,  "CgkI-fuSxP8aEAIQEg" },
                        { Id.ReachedLevel5,  "CgkI-fuSxP8aEAIQEw" }, { Id.ReachedLevel6,  "CgkI-fuSxP8aEAIQFA" }, { Id.ReachedLevel7,  "CgkI-fuSxP8aEAIQFQ" },
                        { Id.ReachedLevel8,  "CgkI-fuSxP8aEAIQFg" }, { Id.ReachedLevel9,  "CgkI-fuSxP8aEAIQFw" }, { Id.ReachedLevel10, "CgkI-fuSxP8aEAIQGA" },
                        { Id.ReachedLevel11, "CgkI-fuSxP8aEAIQGQ" }, { Id.ReachedLevel12, "CgkI-fuSxP8aEAIQGg" }, { Id.ReachedLevel13, "CgkI-fuSxP8aEAIQGw" },
                        { Id.ReachedLevel14, "CgkI-fuSxP8aEAIQHA" }, { Id.ReachedLevel15, "CgkI-fuSxP8aEAIQHQ" }, { Id.ReachedLevel16, "CgkI-fuSxP8aEAIQHg" },
                        { Id.ReachedLevel17, "CgkI-fuSxP8aEAIQHw" }, { Id.ReachedLevel18, "CgkI-fuSxP8aEAIQIA" }, { Id.ReachedLevel19, "CgkI-fuSxP8aEAIQIQ" },
                        { Id.ReachedLevel20, "CgkI-fuSxP8aEAIQIg" }, { Id.ReachedLevel21, "CgkI-fuSxP8aEAIQIw" }, { Id.ReachedLevel22, "CgkI-fuSxP8aEAIQJA" },
                        { Id.ReachedLevel23, "CgkI-fuSxP8aEAIQJQ" }, { Id.ReachedLevel24, "CgkI-fuSxP8aEAIQJg" }, { Id.ReachedLevel25, "CgkI-fuSxP8aEAIQJw" },
                        { Id.ReachedLevel26, "CgkI-fuSxP8aEAIQKA" }, { Id.ReachedLevel27, "CgkI-fuSxP8aEAIQKQ" }, { Id.ReachedLevel28, "CgkI-fuSxP8aEAIQKg" },
                        { Id.ReachedLevel29, "CgkI-fuSxP8aEAIQKw" }, { Id.ReachedLevel30, "CgkI-fuSxP8aEAIQLA" }, { Id.ReachedLevel31, "CgkI-fuSxP8aEAIQLQ" },
                        { Id.ReachedLevel32, "CgkI-fuSxP8aEAIQLg" }, { Id.ReachedLevel33, "CgkI-fuSxP8aEAIQLw" }, { Id.ReachedLevel34, "CgkI-fuSxP8aEAIQMA" },
                        { Id.ReachedLevel35, "CgkI-fuSxP8aEAIQMQ" }, { Id.ReachedLevel36, "CgkI-fuSxP8aEAIQMg" },

                        { Id.NewbieTankMan, "CgkI-fuSxP8aEAIQAA"}, { Id.ExperiencedTanker, "CgkI-fuSxP8aEAIQAQ" }, { Id.ProfessionalTanker, "CgkI-fuSxP8aEAIQAg" },
                        { Id.Shredder,      "CgkI-fuSxP8aEAIQAw"}, { Id.Killer,            "CgkI-fuSxP8aEAIQBA" }, { Id.Terminator,         "CgkI-fuSxP8aEAIQBQ" },

                        { Id.Ordinary, "CgkI-fuSxP8aEAIQBg" }, { Id.Sergeant,  "CgkI-fuSxP8aEAIQBw" }, {Id.Colonel,  "CgkI-fuSxP8aEAIQCA"},
                        { Id.Gambler,  "CgkI-fuSxP8aEAIQCQ" }, { Id.Admirer,   "CgkI-fuSxP8aEAIQCg" }, {Id.Fans,     "CgkI-fuSxP8aEAIQCw"},
                        { Id.Scout,    "CgkI-fuSxP8aEAIQDA" }, { Id.Travelers, "CgkI-fuSxP8aEAIQDQ" }, {Id.Wanderer, "CgkI-fuSxP8aEAIQDg"},
                        { Id.Survivor, "CgkI-fuSxP8aEAIQDw" }
                    }
                },
                {
                    PlayServiceType.IOSGameCenter,new Dictionary<Id,string>
                    {
						{ Id.ReachedLevel2,  "grp.800" }, { Id.ReachedLevel3,  "grp.801" }, { Id.ReachedLevel4,  "grp.802" }, { Id.ReachedLevel5,  "grp.803" }, { Id.ReachedLevel6,  "grp.804" },
						{ Id.ReachedLevel7,  "grp.805" }, { Id.ReachedLevel8,  "grp.806" }, { Id.ReachedLevel9,  "grp.807" }, { Id.ReachedLevel10, "grp.808" }, { Id.ReachedLevel11, "grp.809" },
						{ Id.ReachedLevel12, "grp.810" }, { Id.ReachedLevel13, "grp.811" }, { Id.ReachedLevel14, "grp.812" }, { Id.ReachedLevel15, "grp.813" }, { Id.ReachedLevel16, "grp.814" },
						{ Id.ReachedLevel17, "grp.815" }, { Id.ReachedLevel18, "grp.816" }, { Id.ReachedLevel19, "grp.817" }, { Id.ReachedLevel20, "grp.818" }, { Id.ReachedLevel21, "grp.819" },
						{ Id.ReachedLevel22, "grp.820" }, { Id.ReachedLevel23, "grp.821" }, { Id.ReachedLevel24, "grp.822" }, { Id.ReachedLevel25, "grp.823" }, { Id.ReachedLevel26, "grp.824" },
						{ Id.ReachedLevel27, "grp.825" }, { Id.ReachedLevel28, "grp.826" }, { Id.ReachedLevel29, "grp.827" }, { Id.ReachedLevel30, "grp.828" }, { Id.ReachedLevel31, "grp.829" },
						{ Id.ReachedLevel32, "grp.830" }, { Id.ReachedLevel33, "grp.831" }, { Id.ReachedLevel34, "grp.832" }, { Id.ReachedLevel35, "grp.833" }, { Id.ReachedLevel36, "grp.834" },

						{ Id.NewbieTankMan, "grp.835"}, { Id.ExperiencedTanker, "grp.836" }, { Id.ProfessionalTanker, "grp.837" },
						{ Id.Shredder,      "grp.838"}, { Id.Killer,            "grp.839" }, { Id.Terminator,         "grp.840" },

						{ Id.Ordinary,  "grp.841"}, { Id.Sergeant,  "grp.842"},  { Id.Colonel,  "grp.843"},
						{ Id.Gambler,   "grp.844"}, { Id.Admirer,   "grp.845"},  { Id.Fans,     "grp.846"},
						{ Id.Scout,     "grp.847"}, { Id.Travelers, "grp.848"}, { Id.Wanderer, "grp.849"},
						{ Id.Survivor,  "grp.850"}
					}
				},
                {
                    PlayServiceType.MACGameCenter,new Dictionary<Id,string>
                    {
						{ Id.ReachedLevel2,  "grp.800" }, { Id.ReachedLevel3,  "grp.801" }, { Id.ReachedLevel4,  "grp.802" }, { Id.ReachedLevel5,  "grp.803" }, { Id.ReachedLevel6,  "grp.804" },
						{ Id.ReachedLevel7,  "grp.805" }, { Id.ReachedLevel8,  "grp.806" }, { Id.ReachedLevel9,  "grp.807" }, { Id.ReachedLevel10, "grp.808" }, { Id.ReachedLevel11, "grp.809" },
						{ Id.ReachedLevel12, "grp.810" }, { Id.ReachedLevel13, "grp.811" }, { Id.ReachedLevel14, "grp.812" }, { Id.ReachedLevel15, "grp.813" }, { Id.ReachedLevel16, "grp.814" },
						{ Id.ReachedLevel17, "grp.815" }, { Id.ReachedLevel18, "grp.816" }, { Id.ReachedLevel19, "grp.817" }, { Id.ReachedLevel20, "grp.818" }, { Id.ReachedLevel21, "grp.819" },
						{ Id.ReachedLevel22, "grp.820" }, { Id.ReachedLevel23, "grp.821" }, { Id.ReachedLevel24, "grp.822" }, { Id.ReachedLevel25, "grp.823" }, { Id.ReachedLevel26, "grp.824" },
						{ Id.ReachedLevel27, "grp.825" }, { Id.ReachedLevel28, "grp.826" }, { Id.ReachedLevel29, "grp.827" }, { Id.ReachedLevel30, "grp.828" }, { Id.ReachedLevel31, "grp.829" },
						{ Id.ReachedLevel32, "grp.830" }, { Id.ReachedLevel33, "grp.831" }, { Id.ReachedLevel34, "grp.832" }, { Id.ReachedLevel35, "grp.833" }, { Id.ReachedLevel36, "grp.834" },
						
						{ Id.NewbieTankMan, "grp.835"}, { Id.ExperiencedTanker, "grp.836" }, { Id.ProfessionalTanker, "grp.837" },
						{ Id.Shredder,      "grp.838"}, { Id.Killer,            "grp.839" }, { Id.Terminator,         "grp.840" },
						
						{ Id.Ordinary,  "grp.841"}, { Id.Sergeant,  "grp.842"},  { Id.Colonel,  "grp.843"},
						{ Id.Gambler,   "grp.844"}, { Id.Admirer,   "grp.845"},  { Id.Fans,     "grp.846"},
						{ Id.Scout,     "grp.847"}, { Id.Travelers, "grp.848"}, { Id.Wanderer, "grp.849"},
						{ Id.Survivor,  "grp.850"}
					}
                },
            }
        },

        {
            Interface.BattleOfHelicopters, new Dictionary<PlayServiceType, Dictionary<Id, string>>
            {
                {
                    PlayServiceType.GooglePlay, new Dictionary<Id, string>
                    {
                        { Id.ReachedLevel2,  "CgkIj72zq4QYEAIQFA" }, { Id.ReachedLevel3,  "CgkIj72zq4QYEAIQFQ" }, { Id.ReachedLevel4,  "CgkIj72zq4QYEAIQFg" },
                        { Id.ReachedLevel5,  "CgkIj72zq4QYEAIQFw" }, { Id.ReachedLevel6,  "CgkIj72zq4QYEAIQGA" }, { Id.ReachedLevel7,  "CgkIj72zq4QYEAIQGQ" },
                        { Id.ReachedLevel8,  "CgkIj72zq4QYEAIQGg" }, { Id.ReachedLevel9,  "CgkIj72zq4QYEAIQGw" }, { Id.ReachedLevel10, "CgkIj72zq4QYEAIQHA" },
                        { Id.ReachedLevel11, "CgkIj72zq4QYEAIQHQ" }, { Id.ReachedLevel12, "CgkIj72zq4QYEAIQHg" }, { Id.ReachedLevel13, "CgkIj72zq4QYEAIQHw" },
                        { Id.ReachedLevel14, "CgkIj72zq4QYEAIQIA" }, { Id.ReachedLevel15, "CgkIj72zq4QYEAIQIQ" }, { Id.ReachedLevel16, "CgkIj72zq4QYEAIQIg" },
                        { Id.ReachedLevel17, "CgkIj72zq4QYEAIQIw" }, { Id.ReachedLevel18, "CgkIj72zq4QYEAIQJA" }, { Id.ReachedLevel19, "CgkIj72zq4QYEAIQJQ" },
                        { Id.ReachedLevel20, "CgkIj72zq4QYEAIQJg" }, { Id.ReachedLevel21, "CgkIj72zq4QYEAIQJw" }, { Id.ReachedLevel22, "CgkIj72zq4QYEAIQKA" },
                        { Id.ReachedLevel23, "CgkIj72zq4QYEAIQKQ" }, { Id.ReachedLevel24, "CgkIj72zq4QYEAIQKg" }, { Id.ReachedLevel25, "CgkIj72zq4QYEAIQKw" },
                        { Id.ReachedLevel26, "CgkIj72zq4QYEAIQLA" }, { Id.ReachedLevel27, "CgkIj72zq4QYEAIQLQ" }, { Id.ReachedLevel28, "CgkIj72zq4QYEAIQLg" },
                        { Id.ReachedLevel29, "CgkIj72zq4QYEAIQLw" }, { Id.ReachedLevel30, "CgkIj72zq4QYEAIQMA" }, { Id.ReachedLevel31, "CgkIj72zq4QYEAIQMQ" },
                        { Id.ReachedLevel32, "CgkIj72zq4QYEAIQMw" }, { Id.ReachedLevel33, "CgkIj72zq4QYEAIQNA" }, { Id.ReachedLevel34, "CgkIj72zq4QYEAIQNQ" },
                        { Id.ReachedLevel35, "CgkIj72zq4QYEAIQNg" }, { Id.ReachedLevel36, "CgkIj72zq4QYEAIQNw" },

                        { Id.NewbieTankMan, "CgkIj72zq4QYEAIQBA"}, { Id.ExperiencedTanker, "CgkIj72zq4QYEAIQBQ" }, { Id.ProfessionalTanker, "CgkIj72zq4QYEAIQBg" },
                        { Id.Shredder,      "CgkIj72zq4QYEAIQBw"}, { Id.Killer,            "CgkIj72zq4QYEAIQCA" }, { Id.Terminator,         "CgkIj72zq4QYEAIQCQ" },

                        { Id.Ordinary, "CgkIj72zq4QYEAIQCg" }, { Id.Sergeant,  "CgkIj72zq4QYEAIQCw" }, {Id.Colonel,  "CgkIj72zq4QYEAIQDA"},
                        { Id.Gambler,  "CgkIj72zq4QYEAIQDQ" }, { Id.Admirer,   "CgkIj72zq4QYEAIQDg" }, {Id.Fans,     "CgkIj72zq4QYEAIQDw"},
                        { Id.Scout,    "CgkIj72zq4QYEAIQEA" }, { Id.Travelers, "CgkIj72zq4QYEAIQEQ" }, {Id.Wanderer, "CgkIj72zq4QYEAIQEg"},
                        { Id.Survivor, "CgkIj72zq4QYEAIQEw" }
                    }
                },
                {
                    PlayServiceType.IOSGameCenter,new Dictionary<Id,string>
                    {
                        { Id.ReachedLevel2,  "grp.900" }, { Id.ReachedLevel3,  "grp.901" }, { Id.ReachedLevel4,  "grp.902" }, { Id.ReachedLevel5,  "grp.903" }, { Id.ReachedLevel6,  "grp.904" },
                        { Id.ReachedLevel7,  "grp.905" }, { Id.ReachedLevel8,  "grp.906" }, { Id.ReachedLevel9,  "grp.907" }, { Id.ReachedLevel10, "grp.908" }, { Id.ReachedLevel11, "grp.909" },
                        { Id.ReachedLevel12, "grp.910" }, { Id.ReachedLevel13, "grp.911" }, { Id.ReachedLevel14, "grp.912" }, { Id.ReachedLevel15, "grp.913" }, { Id.ReachedLevel16, "grp.914" },
                        { Id.ReachedLevel17, "grp.915" }, { Id.ReachedLevel18, "grp.916" }, { Id.ReachedLevel19, "grp.917" }, { Id.ReachedLevel20, "grp.918" }, { Id.ReachedLevel21, "grp.919" },
                        { Id.ReachedLevel22, "grp.920" }, { Id.ReachedLevel23, "grp.921" }, { Id.ReachedLevel24, "grp.922" }, { Id.ReachedLevel25, "grp.923" }, { Id.ReachedLevel26, "grp.924" },
                        { Id.ReachedLevel27, "grp.925" }, { Id.ReachedLevel28, "grp.926" }, { Id.ReachedLevel29, "grp.927" }, { Id.ReachedLevel30, "grp.928" }, { Id.ReachedLevel31, "grp.929" },
                        { Id.ReachedLevel32, "grp.930" }, { Id.ReachedLevel33, "grp.931" }, { Id.ReachedLevel34, "grp.932" }, { Id.ReachedLevel35, "grp.933" }, { Id.ReachedLevel36, "grp.934" },

                        { Id.NewbieTankMan, "grp.935"}, { Id.ExperiencedTanker, "grp.936" }, { Id.ProfessionalTanker, "grp.937" },
                        { Id.Shredder,      "grp.938"}, { Id.Killer,            "grp.939" }, { Id.Terminator,         "grp.940" },

                        { Id.Ordinary,  "grp.941"}, { Id.Sergeant,  "grp.942"},  { Id.Colonel,  "grp.943"},
                        { Id.Gambler,   "grp.944"}, { Id.Admirer,   "grp.945"},  { Id.Fans,     "grp.946"},
                        { Id.Scout,     "grp.947"}, { Id.Travelers, "grp.948"}, { Id.Wanderer, "grp.949"},
                        { Id.Survivor,  "grp.950"}
                    }
                },
                {
                    PlayServiceType.MACGameCenter,new Dictionary<Id,string>
                    {
                        { Id.ReachedLevel2,  "grp.900" }, { Id.ReachedLevel3,  "grp.901" }, { Id.ReachedLevel4,  "grp.902" }, { Id.ReachedLevel5,  "grp.903" }, { Id.ReachedLevel6,  "grp.904" },
                        { Id.ReachedLevel7,  "grp.905" }, { Id.ReachedLevel8,  "grp.906" }, { Id.ReachedLevel9,  "grp.907" }, { Id.ReachedLevel10, "grp.908" }, { Id.ReachedLevel11, "grp.909" },
                        { Id.ReachedLevel12, "grp.910" }, { Id.ReachedLevel13, "grp.911" }, { Id.ReachedLevel14, "grp.912" }, { Id.ReachedLevel15, "grp.913" }, { Id.ReachedLevel16, "grp.914" },
                        { Id.ReachedLevel17, "grp.915" }, { Id.ReachedLevel18, "grp.916" }, { Id.ReachedLevel19, "grp.917" }, { Id.ReachedLevel20, "grp.918" }, { Id.ReachedLevel21, "grp.919" },
                        { Id.ReachedLevel22, "grp.920" }, { Id.ReachedLevel23, "grp.921" }, { Id.ReachedLevel24, "grp.922" }, { Id.ReachedLevel25, "grp.923" }, { Id.ReachedLevel26, "grp.924" },
                        { Id.ReachedLevel27, "grp.925" }, { Id.ReachedLevel28, "grp.926" }, { Id.ReachedLevel29, "grp.927" }, { Id.ReachedLevel30, "grp.928" }, { Id.ReachedLevel31, "grp.929" },
                        { Id.ReachedLevel32, "grp.930" }, { Id.ReachedLevel33, "grp.931" }, { Id.ReachedLevel34, "grp.932" }, { Id.ReachedLevel35, "grp.933" }, { Id.ReachedLevel36, "grp.934" },

                        { Id.NewbieTankMan, "grp.935"}, { Id.ExperiencedTanker, "grp.936" }, { Id.ProfessionalTanker, "grp.937" },
                        { Id.Shredder,      "grp.938"}, { Id.Killer,            "grp.939" }, { Id.Terminator,         "grp.940" },

                        { Id.Ordinary,  "grp.941"}, { Id.Sergeant,  "grp.942"},  { Id.Colonel,  "grp.943"},
                        { Id.Gambler,   "grp.944"}, { Id.Admirer,   "grp.945"},  { Id.Fans,     "grp.946"},
                        { Id.Scout,     "grp.947"}, { Id.Travelers, "grp.948"}, { Id.Wanderer, "grp.949"},
                        { Id.Survivor,  "grp.950"}
                    }
                },
            }
        },

        {
            Interface.Armada, new Dictionary<PlayServiceType, Dictionary<Id, string>>
            {
                {
                    PlayServiceType.GooglePlay, new Dictionary<Id, string>
                    {
                        { Id.ReachedLevel2,  "CggI2cKEumwQAhAU" }, { Id.ReachedLevel3,  "CggI2cKEumwQAhAV" }, { Id.ReachedLevel4,  "CggI2cKEumwQAhAW" },
                        { Id.ReachedLevel5,  "CggI2cKEumwQAhAX" }, { Id.ReachedLevel6,  "CggI2cKEumwQAhAY" }, { Id.ReachedLevel7,  "CggI2cKEumwQAhAZ" },
                        { Id.ReachedLevel8,  "CggI2cKEumwQAhAa" }, { Id.ReachedLevel9,  "CggI2cKEumwQAhAb" }, { Id.ReachedLevel10, "CggI2cKEumwQAhAc" },
                        { Id.ReachedLevel11, "CggI2cKEumwQAhAd" }, { Id.ReachedLevel12, "CggI2cKEumwQAhAe" }, { Id.ReachedLevel13, "CggI2cKEumwQAhAf" },
                        { Id.ReachedLevel14, "CggI2cKEumwQAhAg" }, { Id.ReachedLevel15, "CggI2cKEumwQAhAh" }, { Id.ReachedLevel16, "CggI2cKEumwQAhAi" },
                        { Id.ReachedLevel17, "CggI2cKEumwQAhAj" }, { Id.ReachedLevel18, "CggI2cKEumwQAhAk" }, { Id.ReachedLevel19, "CggI2cKEumwQAhAl" },
                        { Id.ReachedLevel20, "CggI2cKEumwQAhAm" }, { Id.ReachedLevel21, "CggI2cKEumwQAhAn" }, { Id.ReachedLevel22, "CggI2cKEumwQAhAo" },
                        { Id.ReachedLevel23, "CggI2cKEumwQAhAp" }, { Id.ReachedLevel24, "CggI2cKEumwQAhAq" }, { Id.ReachedLevel25, "CggI2cKEumwQAhAr" },
                        { Id.ReachedLevel26, "CggI2cKEumwQAhAs" }, { Id.ReachedLevel27, "CggI2cKEumwQAhAt" }, { Id.ReachedLevel28, "CggI2cKEumwQAhAu" },
                        { Id.ReachedLevel29, "CggI2cKEumwQAhAv" }, { Id.ReachedLevel30, "CggI2cKEumwQAhAw" }, { Id.ReachedLevel31, "CggI2cKEumwQAhAx" },
                        { Id.ReachedLevel32, "CggI2cKEumwQAhAy" }, { Id.ReachedLevel33, "CggI2cKEumwQAhAz" }, { Id.ReachedLevel34, "CggI2cKEumwQAhA0" },
                        { Id.ReachedLevel35, "CggI2cKEumwQAhA1" }, { Id.ReachedLevel36, "CggI2cKEumwQAhA2" },

                        { Id.NewbieTankMan, "CggI2cKEumwQAhAB"}, { Id.ExperiencedTanker, "CggI2cKEumwQAhAF" }, { Id.ProfessionalTanker, "CggI2cKEumwQAhAG" },
                        { Id.Shredder,      "CggI2cKEumwQAhAH"}, { Id.Killer,            "CggI2cKEumwQAhAI" }, { Id.Terminator,         "CggI2cKEumwQAhAJ" },

                        { Id.Ordinary, "CggI2cKEumwQAhAK" }, { Id.Sergeant,  "CggI2cKEumwQAhAL" }, {Id.Colonel,  "CggI2cKEumwQAhAM"},
                        { Id.Gambler,  "CggI2cKEumwQAhAN" }, { Id.Admirer,   "CggI2cKEumwQAhAO" }, {Id.Fans,     "CggI2cKEumwQAhAP"},
                        { Id.Scout,    "CggI2cKEumwQAhAQ" }, { Id.Travelers, "CggI2cKEumwQAhAR" }, {Id.Wanderer, "CggI2cKEumwQAhAS"},
                        { Id.Survivor, "CggI2cKEumwQAhAT" }
                    }
                },
                {
                    PlayServiceType.IOSGameCenter,new Dictionary<Id,string>
                    {
                        { Id.ReachedLevel2,  "grp.1100" }, { Id.ReachedLevel3,  "grp.1101" }, { Id.ReachedLevel4,  "grp.1102" }, { Id.ReachedLevel5,  "grp.1103" }, { Id.ReachedLevel6,  "grp.1104" },
                        { Id.ReachedLevel7,  "grp.1105" }, { Id.ReachedLevel8,  "grp.1106" }, { Id.ReachedLevel9,  "grp.1107" }, { Id.ReachedLevel10, "grp.1108" }, { Id.ReachedLevel11, "grp.1109" },
                        { Id.ReachedLevel12, "grp.1110" }, { Id.ReachedLevel13, "grp.1112" }, { Id.ReachedLevel14, "grp.1113" }, { Id.ReachedLevel15, "grp.1114" }, { Id.ReachedLevel16, "grp.1115" },
                        { Id.ReachedLevel17, "grp.1116" }, { Id.ReachedLevel18, "grp.1117" }, { Id.ReachedLevel19, "grp.1118" }, { Id.ReachedLevel20, "grp.1119" }, { Id.ReachedLevel21, "grp.1120" },
                        { Id.ReachedLevel22, "grp.1121" }, { Id.ReachedLevel23, "grp.1122" }, { Id.ReachedLevel24, "grp.1123" }, { Id.ReachedLevel25, "grp.1124" }, { Id.ReachedLevel26, "grp.1125" },
                        { Id.ReachedLevel27, "grp.1126" }, { Id.ReachedLevel28, "grp.1127" }, { Id.ReachedLevel29, "grp.1128" }, { Id.ReachedLevel30, "grp.1129" }, { Id.ReachedLevel31, "grp.1130" },
                        { Id.ReachedLevel32, "grp.1131" }, { Id.ReachedLevel33, "grp.1132" }, { Id.ReachedLevel34, "grp.1133" }, { Id.ReachedLevel35, "grp.1134" }, { Id.ReachedLevel36, "grp.1151" },

                        { Id.NewbieTankMan, "grp.1135"}, { Id.ExperiencedTanker, "grp.1136" }, { Id.ProfessionalTanker, "grp.1137" },
                        { Id.Shredder,      "grp.1138"}, { Id.Killer,            "grp.1139" }, { Id.Terminator,         "grp.1140" },

                        { Id.Ordinary,  "grp.1141"}, { Id.Sergeant,  "grp.1142"},  { Id.Colonel,  "grp.1143"},
                        { Id.Gambler,   "grp.1144"}, { Id.Admirer,   "grp.1145"},  { Id.Fans,     "grp.1146"},
                        { Id.Scout,     "grp.1147"}, { Id.Travelers, "grp.1148"}, { Id.Wanderer, "grp.1149"},
                        { Id.Survivor,  "grp.1150"}
                    }
                },
                {
                    PlayServiceType.MACGameCenter,new Dictionary<Id,string>
                    {
                        { Id.ReachedLevel2,  "grp.1100" }, { Id.ReachedLevel3,  "grp.1101" }, { Id.ReachedLevel4,  "grp.1102" }, { Id.ReachedLevel5,  "grp.1103" }, { Id.ReachedLevel6,  "grp.1104" },
                        { Id.ReachedLevel7,  "grp.1105" }, { Id.ReachedLevel8,  "grp.1106" }, { Id.ReachedLevel9,  "grp.1107" }, { Id.ReachedLevel10, "grp.1108" }, { Id.ReachedLevel11, "grp.1109" },
                        { Id.ReachedLevel12, "grp.1110" }, { Id.ReachedLevel13, "grp.1112" }, { Id.ReachedLevel14, "grp.1113" }, { Id.ReachedLevel15, "grp.1114" }, { Id.ReachedLevel16, "grp.1115" },
                        { Id.ReachedLevel17, "grp.1116" }, { Id.ReachedLevel18, "grp.1117" }, { Id.ReachedLevel19, "grp.1118" }, { Id.ReachedLevel20, "grp.1119" }, { Id.ReachedLevel21, "grp.1120" },
                        { Id.ReachedLevel22, "grp.1121" }, { Id.ReachedLevel23, "grp.1122" }, { Id.ReachedLevel24, "grp.1123" }, { Id.ReachedLevel25, "grp.1124" }, { Id.ReachedLevel26, "grp.1125" },
                        { Id.ReachedLevel27, "grp.1126" }, { Id.ReachedLevel28, "grp.1127" }, { Id.ReachedLevel29, "grp.1128" }, { Id.ReachedLevel30, "grp.1129" }, { Id.ReachedLevel31, "grp.1130" },
                        { Id.ReachedLevel32, "grp.1131" }, { Id.ReachedLevel33, "grp.1132" }, { Id.ReachedLevel34, "grp.1133" }, { Id.ReachedLevel35, "grp.1134" }, { Id.ReachedLevel36, "grp.1151" },

                        { Id.NewbieTankMan, "grp.1135"}, { Id.ExperiencedTanker, "grp.1136" }, { Id.ProfessionalTanker, "grp.1137" },
                        { Id.Shredder,      "grp.1138"}, { Id.Killer,            "grp.1139" }, { Id.Terminator,         "grp.1140" },

                        { Id.Ordinary,  "grp.1141"}, { Id.Sergeant,  "grp.1142"},  { Id.Colonel,  "grp.1143"},
                        { Id.Gambler,   "grp.1144"}, { Id.Admirer,   "grp.1145"},  { Id.Fans,     "grp.1146"},
                        { Id.Scout,     "grp.1147"}, { Id.Travelers, "grp.1148"}, { Id.Wanderer, "grp.1149"},
                        { Id.Survivor,  "grp.1150"}
                    }
                },
            }
        },

        {
            Interface.WingsOfWar, new Dictionary<PlayServiceType, Dictionary<Id, string>>
            {
                {
                    PlayServiceType.GooglePlay, new Dictionary<Id, string>
                    {
                        { Id.ReachedLevel2,  "" }, { Id.ReachedLevel3,  "" }, { Id.ReachedLevel4,  "" },
                        { Id.ReachedLevel5,  "" }, { Id.ReachedLevel6,  "" }, { Id.ReachedLevel7,  "" },
                        { Id.ReachedLevel8,  "" }, { Id.ReachedLevel9,  "" }, { Id.ReachedLevel10, "" },
                        { Id.ReachedLevel11, "" }, { Id.ReachedLevel12, "" }, { Id.ReachedLevel13, "" },
                        { Id.ReachedLevel14, "" }, { Id.ReachedLevel15, "" }, { Id.ReachedLevel16, "" },
                        { Id.ReachedLevel17, "" }, { Id.ReachedLevel18, "" }, { Id.ReachedLevel19, "" },
                        { Id.ReachedLevel20, "" }, { Id.ReachedLevel21, "" }, { Id.ReachedLevel22, "" },
                        { Id.ReachedLevel23, "" }, { Id.ReachedLevel24, "" }, { Id.ReachedLevel25, "" },
                        { Id.ReachedLevel26, "" }, { Id.ReachedLevel27, "" }, { Id.ReachedLevel28, "" },
                        { Id.ReachedLevel29, "" }, { Id.ReachedLevel30, "" }, { Id.ReachedLevel31, "" },
                        { Id.ReachedLevel32, "" }, { Id.ReachedLevel33, "" }, { Id.ReachedLevel34, "" },
                        { Id.ReachedLevel35, "" }, { Id.ReachedLevel36, "" },

                        { Id.NewbieTankMan, ""}, { Id.ExperiencedTanker, "" }, { Id.ProfessionalTanker, "" },
                        { Id.Shredder,      ""}, { Id.Killer,            "" }, { Id.Terminator,         "" },

                        { Id.Ordinary, "" }, { Id.Sergeant,  "" }, {Id.Colonel,  ""},
                        { Id.Gambler,  "" }, { Id.Admirer,   "" }, {Id.Fans,     ""},
                        { Id.Scout,    "" }, { Id.Travelers, "" }, {Id.Wanderer, ""},
                        { Id.Survivor, "" }
                    }
                },
                {
                    PlayServiceType.IOSGameCenter,new Dictionary<Id,string>
                    {
                        { Id.ReachedLevel2,  "" }, { Id.ReachedLevel3,  "" }, { Id.ReachedLevel4,  "" }, { Id.ReachedLevel5,  "" }, { Id.ReachedLevel6,  "" },
                        { Id.ReachedLevel7,  "" }, { Id.ReachedLevel8,  "" }, { Id.ReachedLevel9,  "" }, { Id.ReachedLevel10, "" }, { Id.ReachedLevel11, "" },
                        { Id.ReachedLevel12, "" }, { Id.ReachedLevel13, "" }, { Id.ReachedLevel14, "" }, { Id.ReachedLevel15, "" }, { Id.ReachedLevel16, "" },
                        { Id.ReachedLevel17, "" }, { Id.ReachedLevel18, "" }, { Id.ReachedLevel19, "" }, { Id.ReachedLevel20, "" }, { Id.ReachedLevel21, "" },
                        { Id.ReachedLevel22, "" }, { Id.ReachedLevel23, "" }, { Id.ReachedLevel24, "" }, { Id.ReachedLevel25, "" }, { Id.ReachedLevel26, "" },
                        { Id.ReachedLevel27, "" }, { Id.ReachedLevel28, "" }, { Id.ReachedLevel29, "" }, { Id.ReachedLevel30, "" }, { Id.ReachedLevel31, "" },
                        { Id.ReachedLevel32, "" }, { Id.ReachedLevel33, "" }, { Id.ReachedLevel34, "" }, { Id.ReachedLevel35, "" }, { Id.ReachedLevel36, "" },

                        { Id.NewbieTankMan, ""}, { Id.ExperiencedTanker, "" }, { Id.ProfessionalTanker, "" },
                        { Id.Shredder,      ""}, { Id.Killer,            "" }, { Id.Terminator,         "" },

                        { Id.Ordinary,  ""}, { Id.Sergeant,  ""},  { Id.Colonel,  ""},
                        { Id.Gambler,   ""}, { Id.Admirer,   ""},  { Id.Fans,     ""},
                        { Id.Scout,     ""}, { Id.Travelers, ""},  { Id.Wanderer, ""},
                        { Id.Survivor,  ""}
                    }
                },
                {
                    PlayServiceType.MACGameCenter,new Dictionary<Id,string>
                    {
                        { Id.ReachedLevel2,  "" }, { Id.ReachedLevel3,  "" }, { Id.ReachedLevel4,  "" }, { Id.ReachedLevel5,  "" }, { Id.ReachedLevel6,  "" },
                        { Id.ReachedLevel7,  "" }, { Id.ReachedLevel8,  "" }, { Id.ReachedLevel9,  "" }, { Id.ReachedLevel10, "" }, { Id.ReachedLevel11, "" },
                        { Id.ReachedLevel12, "" }, { Id.ReachedLevel13, "" }, { Id.ReachedLevel14, "" }, { Id.ReachedLevel15, "" }, { Id.ReachedLevel16, "" },
                        { Id.ReachedLevel17, "" }, { Id.ReachedLevel18, "" }, { Id.ReachedLevel19, "" }, { Id.ReachedLevel20, "" }, { Id.ReachedLevel21, "" },
                        { Id.ReachedLevel22, "" }, { Id.ReachedLevel23, "" }, { Id.ReachedLevel24, "" }, { Id.ReachedLevel25, "" }, { Id.ReachedLevel26, "" },
                        { Id.ReachedLevel27, "" }, { Id.ReachedLevel28, "" }, { Id.ReachedLevel29, "" }, { Id.ReachedLevel30, "" }, { Id.ReachedLevel31, "" },
                        { Id.ReachedLevel32, "" }, { Id.ReachedLevel33, "" }, { Id.ReachedLevel34, "" }, { Id.ReachedLevel35, "" }, { Id.ReachedLevel36, "" },

                        { Id.NewbieTankMan, ""}, { Id.ExperiencedTanker, "" }, { Id.ProfessionalTanker, "" },
                        { Id.Shredder,      ""}, { Id.Killer,            "" }, { Id.Terminator,         "" },

                        { Id.Ordinary,  ""}, { Id.Sergeant,  ""},  { Id.Colonel,  ""},
                        { Id.Gambler,   ""}, { Id.Admirer,   ""},  { Id.Fans,     ""},
                        { Id.Scout,     ""}, { Id.Travelers, ""},  { Id.Wanderer, ""},
                        { Id.Survivor,  ""}
                    }
                },
            }
        },

        {
            Interface.MetalForce, new Dictionary<PlayServiceType, Dictionary<Id, string>>
            {
                {
                    PlayServiceType.GooglePlay, new Dictionary<Id, string>
                    {
                        { Id.ReachedLevel2,  "CgkIm9fX9KoEEAIQFQ" }, { Id.ReachedLevel3,  "CgkIm9fX9KoEEAIQFg" }, { Id.ReachedLevel4,  "CgkIm9fX9KoEEAIQFw" },
                        { Id.ReachedLevel5,  "CgkIm9fX9KoEEAIQGA" }, { Id.ReachedLevel6,  "CgkIm9fX9KoEEAIQGQ" }, { Id.ReachedLevel7,  "CgkIm9fX9KoEEAIQGg" },
                        { Id.ReachedLevel8,  "CgkIm9fX9KoEEAIQGw" }, { Id.ReachedLevel9,  "CgkIm9fX9KoEEAIQHA" }, { Id.ReachedLevel10, "CgkIm9fX9KoEEAIQHQ" },
                        { Id.ReachedLevel11, "CgkIm9fX9KoEEAIQHw" }, { Id.ReachedLevel12, "CgkIm9fX9KoEEAIQIA" }, { Id.ReachedLevel13, "CgkIm9fX9KoEEAIQIQ" },
                        { Id.ReachedLevel14, "CgkIm9fX9KoEEAIQIg" }, { Id.ReachedLevel15, "CgkIm9fX9KoEEAIQIw" }, { Id.ReachedLevel16, "CgkIm9fX9KoEEAIQJA" },
                        { Id.ReachedLevel17, "CgkIm9fX9KoEEAIQJQ" }, { Id.ReachedLevel18, "CgkIm9fX9KoEEAIQJg" }, { Id.ReachedLevel19, "CgkIm9fX9KoEEAIQJw" },
                        { Id.ReachedLevel20, "CgkIm9fX9KoEEAIQKA" }, { Id.ReachedLevel21, "CgkIm9fX9KoEEAIQKQ" }, { Id.ReachedLevel22, "CgkIm9fX9KoEEAIQKg" },
                        { Id.ReachedLevel23, "CgkIm9fX9KoEEAIQKw" }, { Id.ReachedLevel24, "CgkIm9fX9KoEEAIQLA" }, { Id.ReachedLevel25, "CgkIm9fX9KoEEAIQLQ" },
                        { Id.ReachedLevel26, "CgkIm9fX9KoEEAIQLg" }, { Id.ReachedLevel27, "CgkIm9fX9KoEEAIQLw" }, { Id.ReachedLevel28, "CgkIm9fX9KoEEAIQMA" },
                        { Id.ReachedLevel29, "CgkIm9fX9KoEEAIQMQ" }, { Id.ReachedLevel30, "CgkIm9fX9KoEEAIQMg" }, { Id.ReachedLevel31, "CgkIm9fX9KoEEAIQMw" },
                        { Id.ReachedLevel32, "CgkIm9fX9KoEEAIQNA" }, { Id.ReachedLevel33, "CgkIm9fX9KoEEAIQNQ" }, { Id.ReachedLevel34, "CgkIm9fX9KoEEAIQNg" },
                        { Id.ReachedLevel35, "CgkIm9fX9KoEEAIQNw" }, { Id.ReachedLevel36, "CgkIm9fX9KoEEAIQOA" },

                        { Id.NewbieTankMan, "CgkIm9fX9KoEEAIQAA"}, { Id.ExperiencedTanker, "CgkIm9fX9KoEEAIQAg" }, { Id.ProfessionalTanker, "CgkIm9fX9KoEEAIQAw" },
                        { Id.Shredder,      "CgkIm9fX9KoEEAIQBA"}, { Id.Killer,            "CgkIm9fX9KoEEAIQBQ" }, { Id.Terminator,         "CgkIm9fX9KoEEAIQBg" },

                        { Id.Ordinary, "CgkIm9fX9KoEEAIQBw" }, { Id.Sergeant,  "CgkIm9fX9KoEEAIQCA" }, {Id.Colonel,  "CgkIm9fX9KoEEAIQCQ"},
                        { Id.Gambler,  "CgkIm9fX9KoEEAIQCg" }, { Id.Admirer,   "CgkIm9fX9KoEEAIQCw" }, {Id.Fans,     "CgkIm9fX9KoEEAIQEA"},
                        { Id.Scout,    "CgkIm9fX9KoEEAIQEQ" }, { Id.Travelers, "CgkIm9fX9KoEEAIQEg" }, {Id.Wanderer, "CgkIm9fX9KoEEAIQEw"},
                        { Id.Survivor, "CgkIm9fX9KoEEAIQFA" }
                    }
                },
                {
                    PlayServiceType.IOSGameCenter,new Dictionary<Id,string>
                    {
                        { Id.ReachedLevel2,  "grp.1302" }, { Id.ReachedLevel3,  "grp.1303" }, { Id.ReachedLevel4,  "grp.1304" }, { Id.ReachedLevel5,  "grp.1305" }, { Id.ReachedLevel6,  "grp.1306" },
                        { Id.ReachedLevel7,  "grp.1307" }, { Id.ReachedLevel8,  "grp.1308" }, { Id.ReachedLevel9,  "grp.1309" }, { Id.ReachedLevel10, "grp.1310" }, { Id.ReachedLevel11, "grp.1311" },
                        { Id.ReachedLevel12, "grp.1312" }, { Id.ReachedLevel13, "grp.1313" }, { Id.ReachedLevel14, "grp.1314" }, { Id.ReachedLevel15, "grp.1315" }, { Id.ReachedLevel16, "grp.1316" },
                        { Id.ReachedLevel17, "grp.1317" }, { Id.ReachedLevel18, "grp.1318" }, { Id.ReachedLevel19, "grp.1319" }, { Id.ReachedLevel20, "grp.1320" }, { Id.ReachedLevel21, "grp.1321" },
                        { Id.ReachedLevel22, "grp.1322" }, { Id.ReachedLevel23, "grp.1323" }, { Id.ReachedLevel24, "grp.1324" }, { Id.ReachedLevel25, "grp.1325" }, { Id.ReachedLevel26, "grp.1326" },
                        { Id.ReachedLevel27, "grp.1327" }, { Id.ReachedLevel28, "grp.1328" }, { Id.ReachedLevel29, "grp.1329" }, { Id.ReachedLevel30, "grp.1330" }, { Id.ReachedLevel31, "grp.1331" },
                        { Id.ReachedLevel32, "grp.1332" }, { Id.ReachedLevel33, "grp.1333" }, { Id.ReachedLevel34, "grp.1334" }, { Id.ReachedLevel35, "grp.1335" }, { Id.ReachedLevel36, "grp.1336" },

                        { Id.NewbieTankMan, "grp.1338"}, { Id.ExperiencedTanker, "grp.1339" }, { Id.ProfessionalTanker, "grp.1340" },
                        { Id.Shredder,      "grp.1341"}, { Id.Killer,            "grp.1342" }, { Id.Terminator,         "grp.1343" },

                        { Id.Ordinary,  "grp.1344"}, { Id.Sergeant,  "grp.1345"},  { Id.Colonel,  "grp.1346"},
                        { Id.Gambler,   "grp.1347"}, { Id.Admirer,   "grp.1348"},  { Id.Fans,     "grp.1349"},
                        { Id.Scout,     "grp.1350"}, { Id.Travelers, "grp.1351"},  { Id.Wanderer, "grp.1352"},
                        { Id.Survivor,  "grp.1353"}
                    }
                },
                {
                    PlayServiceType.MACGameCenter,new Dictionary<Id,string>
                    {
                        { Id.ReachedLevel2,  "grp.1302" }, { Id.ReachedLevel3,  "grp.1303" }, { Id.ReachedLevel4,  "grp.1304" }, { Id.ReachedLevel5,  "grp.1305" }, { Id.ReachedLevel6,  "grp.1306" },
                        { Id.ReachedLevel7,  "grp.1307" }, { Id.ReachedLevel8,  "grp.1308" }, { Id.ReachedLevel9,  "grp.1309" }, { Id.ReachedLevel10, "grp.1310" }, { Id.ReachedLevel11, "grp.1311" },
                        { Id.ReachedLevel12, "grp.1312" }, { Id.ReachedLevel13, "grp.1313" }, { Id.ReachedLevel14, "grp.1314" }, { Id.ReachedLevel15, "grp.1315" }, { Id.ReachedLevel16, "grp.1316" },
                        { Id.ReachedLevel17, "grp.1317" }, { Id.ReachedLevel18, "grp.1318" }, { Id.ReachedLevel19, "grp.1319" }, { Id.ReachedLevel20, "grp.1320" }, { Id.ReachedLevel21, "grp.1321" },
                        { Id.ReachedLevel22, "grp.1322" }, { Id.ReachedLevel23, "grp.1323" }, { Id.ReachedLevel24, "grp.1324" }, { Id.ReachedLevel25, "grp.1325" }, { Id.ReachedLevel26, "grp.1326" },
                        { Id.ReachedLevel27, "grp.1327" }, { Id.ReachedLevel28, "grp.1328" }, { Id.ReachedLevel29, "grp.1329" }, { Id.ReachedLevel30, "grp.1330" }, { Id.ReachedLevel31, "grp.1331" },
                        { Id.ReachedLevel32, "grp.1332" }, { Id.ReachedLevel33, "grp.1333" }, { Id.ReachedLevel34, "grp.1334" }, { Id.ReachedLevel35, "grp.1335" }, { Id.ReachedLevel36, "grp.1336" },

                        { Id.NewbieTankMan, "grp.1338"}, { Id.ExperiencedTanker, "grp.1339" }, { Id.ProfessionalTanker, "grp.1340" },
                        { Id.Shredder,      "grp.1341"}, { Id.Killer,            "grp.1342" }, { Id.Terminator,         "grp.1343" },

                        { Id.Ordinary,  "grp.1344"}, { Id.Sergeant,  "grp.1345"},  { Id.Colonel,  "grp.1346"},
                        { Id.Gambler,   "grp.1347"}, { Id.Admirer,   "grp.1348"},  { Id.Fans,     "grp.1349"},
                        { Id.Scout,     "grp.1350"}, { Id.Travelers, "grp.1351"},  { Id.Wanderer, "grp.1352"},
                        { Id.Survivor,  "grp.1353"}
                    }
                },
            }
        },

    };

    public static readonly Dictionary<Interface, Dictionary<PlayServiceType, string>> leaderBoardIds = new Dictionary<Interface, Dictionary<PlayServiceType, string>>
	{
		{
            Interface.FutureTanks, new Dictionary<PlayServiceType,string> // FutureTanks.
			{
			    { PlayServiceType.GooglePlay,    "CgkI7Om0kosCEAIQNA" },
                { PlayServiceType.IOSGameCenter, "grp.9" },
                { PlayServiceType.MACGameCenter, "grp.9" }
            }
		},
		{
            Interface.IronTanks, new Dictionary<PlayServiceType,string> // IronTanks.
			{
                { PlayServiceType.GooglePlay,    "CgkI5ubipOgZEAIQNA" },
				{ PlayServiceType.IOSGameCenter, "grp.8"},//2
                { PlayServiceType.MACGameCenter, "grp.8"},//2
            }
		},
        {
            Interface.ToonWars, new Dictionary<PlayServiceType,string>
            {
                { PlayServiceType.GooglePlay,      "CgkIy9i9sKkXEAIQNw" },
                { PlayServiceType.IOSGameCenter, "grp.toonwars_leaderboard"} ,//3
                { PlayServiceType.MACGameCenter, "grp.toonwars_leaderboard"} ,//3
            }
        },
        {
        Interface.SpaceJet, new Dictionary<PlayServiceType,string>
            {
                { PlayServiceType.GooglePlay,      "CgkItvrh7-cHEAIQOA" },
                { PlayServiceType.IOSGameCenter, "grp.spacejet_leaderboard"},//4
                { PlayServiceType.MACGameCenter, "grp.spacejet_leaderboard"},//4
            }
        },
        {
        Interface.BlowOut, new Dictionary<PlayServiceType,string>
            {
                { PlayServiceType.GooglePlay,      "" },//TODO: Установить реальный ИД
                { PlayServiceType.IOSGameCenter, "grp.blowout_leaderboard"},
                { PlayServiceType.MACGameCenter, "grp.blowout_leaderboard"},
            }
        },
        {
        Interface.BattleOfWarplanes, new Dictionary<PlayServiceType,string>
            {
                { PlayServiceType.GooglePlay,    "CgkI-fuSxP8aEAIQNw" },
                { PlayServiceType.IOSGameCenter, "grp.battleofwarplanes_leaderboard"},
                { PlayServiceType.MACGameCenter, "grp.battleofwarplanes_leaderboard"},
            }
        }
        ,
        {
        Interface.BattleOfHelicopters, new Dictionary<PlayServiceType,string>
            {
                { PlayServiceType.GooglePlay,    "CgkIj72zq4QYEAIQOA" },
                { PlayServiceType.IOSGameCenter, "grp.battleofhelicopters_leaderboard"},
                { PlayServiceType.MACGameCenter, "grp.battleofhelicopters_leaderboard"},
            }
        },
        {
        Interface.Armada, new Dictionary<PlayServiceType,string>
            {
                { PlayServiceType.GooglePlay,    "CggI2cKEumwQAhA3" },
                { PlayServiceType.IOSGameCenter, "grp.armada_leaderboard"},
                { PlayServiceType.MACGameCenter, "grp.armada_leaderboard"},
            }
        },
        {
        Interface.WingsOfWar, new Dictionary<PlayServiceType,string>
            {
                { PlayServiceType.GooglePlay,    "" },
                { PlayServiceType.IOSGameCenter, "grp.wingsofwar_leaderboard"},
                { PlayServiceType.MACGameCenter, "grp.wingsofwar_leaderboard"},
            }
        },
        {
        Interface.MetalForce, new Dictionary<PlayServiceType,string>
            {
                { PlayServiceType.GooglePlay,    "CgkIm9fX9KoEEAIQAQ" },
                { PlayServiceType.IOSGameCenter, "grp.metalforce_leaderboard"},
                { PlayServiceType.MACGameCenter, "grp.metalforce_leaderboard"},
            }
        }
    };

    public static void Init()
    {
        if (isInited)
            return;

        if (!data.ContainsKey(GameData.CurInterface) || !data[GameData.CurInterface].ContainsKey(CurrentPlayService))
        {
            Debug.LogError("!!! YOU MUST DEFINE ACHIEVEMENTS IDs !!!");
            return;
        }

        storeIdToEnum.Clear();
        enumStringToEnum.Clear();
        foreach (var ach in data[GameData.CurInterface][CurrentPlayService])
        {
            storeIdToEnum[ach.Value] = ach.Key;
            enumStringToEnum[ach.Key.ToString()] = ach.Key;
        }

        isInited = true;
    }

    public static bool GetAchievementIdFromStoreId(string stringId, ref Id _id)
    {
        if(!storeIdToEnum.ContainsKey(stringId))
        {
            Debug.LogErrorFormat("!!! storeIdToEnum not contains key {0} !!!", stringId);
            return false;
        }

        _id = storeIdToEnum[stringId];
        return true;
    }

    public static bool GetAchieveIdFromEnumString(string stringId, ref Id _id)
    {
        if (!enumStringToEnum.ContainsKey(stringId))
        {
            Debug.LogErrorFormat("!!! enumStringToEnum not contains key {0} !!!", stringId);
            return false;
        }

        _id = enumStringToEnum[stringId];
        return true;
    }

    public static string GetLeaderboardId()
    {
        if (!leaderBoardIds.ContainsKey(GameData.CurInterface) || !leaderBoardIds[GameData.CurInterface].ContainsKey(CurrentPlayService))
        {
            Debug.LogError("!!! YOU MUST DEFINE LEADERBOARD ID !!!");
            return "";
        }

        return leaderBoardIds[GameData.CurInterface][CurrentPlayService];
    }

    public static string GetAchievementIdFromDataDic(Id id)
    {
        //Debug.Log (GameData.IsGame(Game.IronTanks) + " " + curPlayService.ToString() + " achiev id " + id.ToString());
        return data[GameData.CurInterface][CurrentPlayService][id];
    }
}
