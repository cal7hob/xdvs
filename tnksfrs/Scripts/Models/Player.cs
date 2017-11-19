using System;
using UnityEngine;

namespace Tanks.Models
{
    [Serializable]
    public class Social
    {
        public SocialPlatform Platform
        {
            get
            {
                return platform;
            }
            set
            {
                platform = value;
            }
        }
        public string Uid
        {
            get
            {
                return uid;
            }
            set
            {
                uid = value;
            }
        }
        public string FirstName
        {
            get; set;
        }
        public string LastName
        {
            get; set;
        }
        public string AvatarURL
        {
            get; set;
        }

        [SerializeField]
        private SocialPlatform platform;
        [SerializeField]
        private string uid;

        public Social()
        {
        }

        public Social(SocialPlatform platform, string uid)
        {
            this.platform = platform;
            this.uid = uid;
        }

        public Social(SocialPlatform platform, string uid, string firstName, string lastName, string avatarUrl)
        {
            this.platform = platform;
            this.uid = uid;
            this.FirstName = firstName;
            this.LastName = lastName;
            this.AvatarURL = avatarUrl;
        }
        public static Social Create(JsonPrefs prefs)
        {
            var social = new Social();
            #region Test mockup
            //social.Platform = SocialPlatform.Facebook;
            //social.Uid = "389323967900148";
            //return social;
            #endregion

            foreach (SocialPlatform platform in Enum.GetValues(typeof(SocialPlatform)))
            {
                if (!string.IsNullOrEmpty(social.uid = prefs.ValueString(SocialPlatform.GetName(typeof(SocialPlatform), platform).ToLower())))
                {
                    social.platform = platform;

                    return social;
                }
            }

            return null;
        }

        public override string ToString()
        {
            return string.Format("[Social: Platform={0}, Uid={1}]", Platform, Uid);
        }
    }

    [Serializable]
    public class Player
    {
        public int Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }

        public string NickName
        {
            get
            {
                return nickName;
            }
            set
            {
                nickName = value;
            }
        }

        public int Score
        {
            get
            {
                return score;
            }
            set
            {
                score = value;
            }
        }

        public long LastActivityTimestamp
        {
            get
            {
                return lastActivityTimestamp;
            }
            set
            {
                lastActivityTimestamp = value;
            }
        }

        public bool IsVip
        {
            get
            {
                return isVip;
            }
            set
            {
                isVip = value;
            }
        }

        public Social Social
        {
            get
            {
                return social;
            }
            set
            {
                social = value;
            }
        }

        public Clan Clan
        {
            get
            {
                return clan;
            }
            set
            {
                clan = value;
            }
        }

        public string CountryCode
        {
            get
            {
                return countryCode;
            }
            set
            {
                countryCode = value;
            }
        }

        public bool IsOnline
        {
            get
            {
                if (lastActivityTimestamp > 0)
                    return Convert.ToInt64(GameData.CorrectedCurrentTimeStamp) - lastActivityTimestamp < SocialSettings.IDLE_THRESHOLD;

                return false;
            }
        }

        [SerializeField]
        private int id;
        [SerializeField]
        private string nickName;
        [SerializeField]
        private int score;
        [SerializeField]
        private long lastActivityTimestamp;
        [SerializeField]
        private bool isVip;
        [SerializeField]
        private Social social = null;
        [SerializeField]
        private Clan clan;
        [SerializeField]
        private string countryCode;

        private const string LAST_ACTIVITY_TIMESTAMP_KEY = "lastActiveTime";
        private const string NICKNAME_KEY = "nickName";
        private const string SCORE_KEY = "score";
        private const string PLAYERID_KEY = "id";
        private const string VIP_KEY = "isVip";
        private const string CLAN_KEY = "clan";

        public Player()
        {
        }

        public Player(SocialPlatform platform, string uid)
        {
            this.social = new Social(platform, uid);
        }
        public Player(SocialPlatform platform, string uid, string firstName, string lastName, string avatarUrl)
        {
            this.social = new Social(platform, uid, firstName, lastName, avatarUrl);
        }

        public Player(int id, string nickName, int score = 0, SocialPlatform platform = SocialPlatform.Undefined, string uid = "", long lastActivityTimestamp = 0, bool isVip = false, Clan clan = null)
        {
            this.id = id;
            this.nickName = nickName;
            this.score = score;
            this.social = new Social(platform, uid);
            this.lastActivityTimestamp = lastActivityTimestamp;
            this.isVip = isVip;
            this.clan = clan;
        }

        // Static factory
        public static Player Create(int id, string nickName, int score = 0, SocialPlatform platform = SocialPlatform.Undefined, string uid = "", long lastActivityTimestamp = 0, bool isVip = false, Clan clan = null)
        {
            return new Player(id, nickName, score, platform, uid, lastActivityTimestamp, isVip, clan);
        }

        // Static factory
        public static Player Create(JsonPrefs prefs)
        {
            var player = new Player
            {
                id = prefs.ValueInt(PLAYERID_KEY),
                nickName = prefs.ValueString(NICKNAME_KEY),
                score = prefs.ValueInt(SCORE_KEY),
                lastActivityTimestamp = prefs.ValueLong(LAST_ACTIVITY_TIMESTAMP_KEY),
                isVip = prefs.ValueBool(VIP_KEY),
                Social = Social.Create(prefs)
            };

            var clanDict = prefs.ValueObjectDict(CLAN_KEY, null);

            if (clanDict != null)
            {
                player.Clan = Clan.Create(new JsonPrefs(clanDict));
            }

            return player;
        }

        public override string ToString()
        {
            return string.Format("[Player: Id={0}, NickName={1}, Score={2}, " +
                "Social={3}, LastActivityTimestamp={4}, Vip={5}, Clan={6}]",
                Id, NickName, Score, Social, LastActivityTimestamp, IsVip, Clan);
        }
    }
}
