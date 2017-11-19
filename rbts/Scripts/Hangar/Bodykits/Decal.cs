public class Decal : Bodykit
{
    private const int VICTORY_DAY_STICKER_ID_DEFAULT = 11;
    private const int VICTORY_DAY_STICKER_ID_FT = 12;

    public Decal(DecalInEditor sourcePattern) : base(sourcePattern) {}

    public bool IsForVictoryDay
    {
        get
        {
            if (GameData.IsGame(Game.FutureTanks))
                return id == VICTORY_DAY_STICKER_ID_FT;

            return id == VICTORY_DAY_STICKER_ID_DEFAULT;
        }
    }
}
