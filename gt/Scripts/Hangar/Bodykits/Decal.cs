public class Decal : Bodykit
{
    private const int VICTORY_DAY_STICKER_ID_DEFAULT = 11;

    public Decal(DecalInEditor sourcePattern) : base(sourcePattern) {}

    public bool IsForVictoryDay
    {
        get { return id == VICTORY_DAY_STICKER_ID_DEFAULT; }
    }
}
