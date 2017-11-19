using System.Collections;
using System.Linq;
using UnityEngine;

public class GoToVehicleShopTutorial : Tutorial
{
    public static int NeededShopVehicleCellId { get; private set; }
    public static bool HasEnoughMoneyToBuyVehicle { get; private set; }

    public override bool IsActive
    {
        get
        {
            return ProfileInfo.TutorialIndex == index &&
                   GUIPager.ActivePage == page &&
                   HasEnoughMoneyToBuyVehicle &&
                   !HasOtherVehicles;
        }
    }

    public static bool HasOtherVehicles
    {
        get
        {
            var vehiclesCount = ProfileInfo.vehicleUpgrades.Count(vehicle => vehicle.Value.vehicleId != VehicleOffersController.FreeVehicleIds[GameData.ClearGameFlags(GameData.CurrentGame)]);
            return vehiclesCount > 1;
        }
    }

    protected override void Awake()
    {
        Dispatcher.Subscribe(EventId.ProfileInfoLoadedFromServer, CheckMoney);
        base.Awake();
    }

    protected override void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.ProfileInfoLoadedFromServer, CheckMoney);
        base.OnDestroy();
    }

    protected override void Init(EventId id = 0, EventInfo info = null)
    {
        base.Init(id, info);

        index = (int)Tutorials.buyVehicle;
        page = TutorialPages.MainMenu.ToString();

        CheckMoney();
        Refresh();
    }

    protected override void InstantiateTutorialParts()
    {
        var position
            = GameData.IsGame(Game.WWT2)
                ? TutorialsController.MainMenuButtons.VehicleShopBtn.localPosition
                : TutorialsController.MainMenuButtons.ModuleShopBtn.localPosition; // Ставим перса правее, чтобы не перекрывал стрелку.

        var character
            = MenuController.InstantiateTutorialPart(
                holder:         holder,
                path:           "Tutorials/sprCharacterFromRes",
                anchor:         CharacterAnchor,
                position:       position + Vector3.right * CharacterXOffset,
                yPos:           CharacterYOffset,
                eulerAngles:    Vector3.zero,
                partName:       "Character");

        var characterSprFromRes = character.GetComponent<SpriteFromRes>();

        var characterSpriteDimensions
            = characterSprFromRes
                ? ((tk2dSlicedSprite)characterSprFromRes.Sprite).dimensions
                : character.GetComponent<tk2dSlicedSprite>().dimensions;

        MenuController.InstantiateTutorialPart(
            holder:         holder,
            path:           "Tutorials/ArrowPointerWrapper",
            anchor:         TutorialHolder.CamAnchors.lowerLeft,
            position:       TutorialsController.MainMenuButtons.VehicleShopBtn.localPosition,
            yPos:           TutorialsController.MainMenuButtons.VehicleShopBtn.localPosition.y + ArrowYOffset,
            eulerAngles:    Vector3.zero,
            partName:       "ArrowPointerWrapper");

        var messageOffset
            = GameData.IsGame(Game.WWT2)
                ? 0.0f
                : 350.0f; // Ставим сообщение правее, чтобы не перекрывало стрелку (положение перса почему-то не влияет).

        MenuController.InstantiateTutorialPart(
            holder:                 holder,
            path:                   TutorialMessagePath,
            anchor:                 TutorialMessageAnchor,
            position:               characterSpriteDimensions + Vector2.right * (MessageXOffset + messageOffset),
            yPos:                   characterSpriteDimensions.y + MessageYOffset,
            eulerAngles:            Vector3.zero,
            partName:               "tutorialMessage_4",
            isLocalizationNeded:    true,
            parent:                 character.transform);
    }

    private static void CheckMoney(EventId id = 0, EventInfo info = null)
    {
        if (VehicleShop.Selectors == null)
            return;

        var firstSilverVehicleCell = VehicleShop.Selectors.FirstOrDefault(vehicle => vehicle.Value.UserVehicle.Info.Price.currency == ProfileInfo.PriceCurrency.Silver && vehicle.Value.UserVehicle.Info.id != 1);
        var firstGoldVehicleCell = VehicleShop.Selectors.FirstOrDefault(vehicle => vehicle.Value.UserVehicle.Info.Price.currency == ProfileInfo.PriceCurrency.Gold && vehicle.Value.UserVehicle.Info.id != 1);

        if (ProfileInfo.Gold >= firstGoldVehicleCell.Value.UserVehicle.Info.Price.value)
        {
            NeededShopVehicleCellId = firstGoldVehicleCell.Key;
            HasEnoughMoneyToBuyVehicle = true;
            return;
        }

        if (ProfileInfo.Silver >= firstSilverVehicleCell.Value.UserVehicle.Info.Price.value)
        {
            NeededShopVehicleCellId = firstSilverVehicleCell.Key;
            HasEnoughMoneyToBuyVehicle = true;
            return;
        }

        HasEnoughMoneyToBuyVehicle = false;
    }

    protected override IEnumerator RefreshingRoutine()
    {
        holder.Wrapper.SetActive(IsActive);

        yield return new WaitForEndOfFrame();  

        if (IsActive && !didPlayVoice)
        {
            didPlayVoice = true;
            Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.BuyNewVehicleLesson));
        }
    }
}
