using System;
using UnityEngine;

public class TutorialSprite : MonoBehaviour
{

    [SerializeField] private tk2dBaseSprite sprite;
    private SpriteFromRes spriteFromRes;

    public tk2dBaseSprite Sprite { get { return sprite; } }

	public void Initialize ()
	{
	    SetSpriteFromRes();
	}

    private void SetSpriteFromRes()
    {
        spriteFromRes = GetComponent<SpriteFromRes>();

        if (spriteFromRes == null)
            return;

        string texName = null;
        Vector2 dimensions = Vector2.zero;

        switch (GameData.ClearGameFlags(GameData.CurrentGame))
        {
            case Game.IronTanks:
                texName = "Lady";
                dimensions = new Vector2(564, 990);
                break;
            case Game.FutureTanks:
                texName = "Lady";
                dimensions = new Vector2(652, 1214);
                break;
            case Game.ToonWars:
                texName = "General_USA";
                dimensions = new Vector2(550, 785);
                break;
            case Game.SpaceJet:
                texName = "Lady";
                dimensions = new Vector2(668, 996);
                break;
            case Game.BattleOfWarplanes:
                texName = "Lady";
                dimensions = new Vector2(720, 880);
                break;
            case Game.BattleOfHelicopters:
                texName = "LadyBlonde";
                dimensions = new Vector2(729, 990);
                break;
            case Game.Armada:
                texName = "tutorCharacter";
                dimensions = new Vector2(304, 302);
                break;
            case Game.WWR:
                texName = "tutorCharacter";
                dimensions = new Vector2(304, 302);
                break;
            case Game.ApocalypticCars:
                break;
            case Game.FTRobotsInvasion:
                texName = "Lady";
                dimensions = new Vector2(824, 1200);
                break;
            default:
                throw new Exception(GameData.ClearGameFlags(GameData.CurrentGame) + " case is not defined!");
        }

        spriteFromRes.SetTexture(texName, dimensions.x, dimensions.y);
    }
}
