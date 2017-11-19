using XD;
using System.Collections.Generic;

public class PlayerStat : ISimpleStat
{
    public int PlayerID
    {
        get;
        set;
    }

    public int Team
    {
        get;
        set;
    }

    public string Nick
    {
        get;
        set;
    }

    public string ClanName
    {
        get;
        set;
    }

    public int Level
    {
        get;
        set;
    }

    public int UnitID
    {
        get;
        set;
    }

    public bool VIP
    {
        get;
        set;
    }

    public int InnerID
    {
        get;
        set;
    }    

    private Dictionary<StatisticParameter, int> stats = null;

    public Dictionary<StatisticParameter, int> Stats
    {
        get
        {
            if (stats == null)
            {
                stats = new Dictionary<StatisticParameter, int>();
                stats.Add(StatisticParameter.Kills, 0);
                stats.Add(StatisticParameter.Deaths, 0);
                stats.Add(StatisticParameter.Experience, 0);
                stats.Add(StatisticParameter.Damage, 0);
            }

            return stats;
        }
    }

    public PlayerStat()
    {
    }

    public PlayerStat(int _playerId, int _teamId, int _playerLevel, string _playerName, bool _vip, int _innerId, string _clanName, int _unitID)
    {
        PlayerID = _playerId;
        Team = _teamId;
        Level = _playerLevel;
        Nick = _playerName;
        VIP = _vip;
        InnerID = _innerId;
        ClanName = _clanName;
        UnitID = _unitID;
    }

    public override string ToString()
    {
        string stat = "";
        foreach (KeyValuePair<StatisticParameter, int> pair in Stats)
        {
            stat += "\n" + pair.Key + ": " + pair.Value;
        }
        string result = string.Format("[{0}] {1}:{2}",PlayerID, Nick, stat);
        return result;
    }
}
