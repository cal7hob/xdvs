public class PlayerStat
{
	public int playerId;
	public int teamId;
	public string playerName;
	public string clanName;
	public int playerLevel;
	public int deaths = 0;
	public int kills = 0;
	public int score = -1;
	public float inactivityTime = 0;
	public string countryCode;
	public bool vip;
    public int profileId;

	public PlayerStat(){}
		
	public PlayerStat(int playerId, int teamId, int playerLevel, string playerName, string countryCode, bool vip, int profileId, string clanName)
	{
		this.playerId = playerId;
		this.teamId = teamId;
		this.playerLevel = playerLevel;
		this.playerName = playerName;
	    this.countryCode = countryCode;
		this.vip = vip;
	    this.profileId = profileId;
		this.clanName = clanName;
	}
}
