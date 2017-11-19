using UnityEngine;
using System;
using System.Collections.Generic;

public class MoreGamesController : HangarPage 
{
    [SerializeField] private Factory factory;

    public GameObject button;

    private List<GamePromo> gamesPromos = new List<GamePromo>();

    #region Our Games Ids
    Dictionary<string, object> promos = new Dictionary<string, object>
    {
        {"promos", new List<object>
        {
            new Dictionary<string, object>
            {
                {"id", "MetalForce"},
                {"texture", "MetalForcePromo"},
                {"googleBundleId", "com.extremedevelopers.metalforce"},
                {"amazonBundleId", "com.extremedevelopers.metalforce"},
                {"displayName", "METAL FORCE"},
                {"wsaPdpId", "9nt2lhrr5g56"},
                {"iosId", "1223391730"},
                {"iosScheme", "com.extremedevelopers.metalforce"},
                {"macId", "1223395936"},
                {
                    "webUrls", new Dictionary<string, string>
                    {
                        {"Facebook", "https://apps.facebook.com/metalforce"},
                        {"Vkontakte", "https://vk.com/app6014326"},
                        {"Odnoklassniki", "https://ok.ru/game/1250983168"},
                        {"Mail", "https://my.mail.ru/apps/753745"}
                    }
                }
            },
            new Dictionary<string, object>
            {
                {"id", "IronTanks"},
                {"texture", "IronTanksPromo"},
                {"displayName", "IRON TANKS"},
                {"googleBundleId", "com.extremedevelopers.irontanks"},
                {"amazonBundleId", "com.extremedevelopers.irontanksam"},
                {"wsaPdpId", "9nblggh0c2gx"},
                {"iosId", "970307148"},
                {"iosScheme", "com.extremedevelopers.irontanks"},
                {"macId", "1049682043"},
                {
                    "webUrls", new Dictionary<string, string>
                    {
                        {"Facebook", "https://apps.facebook.com/irontanks"},
                        {"Vkontakte", "https://vk.com/app4681980"},
                        {"Odnoklassniki", "https://ok.ru/game/irontanks"},
                        {"Mail", "http://my.mail.ru/apps/728044"}
                    }
                }
            },
            new Dictionary<string, object>
            {
                {"id", "FutureTanks"},
                {"texture", "FutureTanksPromo"},
                {"googleBundleId", "com.extremedevelopers.futuretanks"},
                {"amazonBundleId", "com.extremedevelopers.futuretanksam"},
                {"displayName", "FUTURE TANKS"},
                {"wsaPdpId", "9wzdncrd21vb"},
                {"iosId", "960612716"},
                {"iosScheme", "com.extremedevelopers.futuretanks"},
                {"macId", "1054872882"},
                {
                    "webUrls", new Dictionary<string, string>
                    {
                        {"Facebook", "https://apps.facebook.com/futuretanks"},
                        {"Vkontakte", "https://vk.com/app4731235"},
                        {"Odnoklassniki", "https://ok.ru/game/futuretanks"},
                        {"Mail", "http://my.mail.ru/apps/729272"}
                    }
                }
            },
            new Dictionary<string, object>
            {
                {"id", "ToonWars"},
                {"texture", "ToonWarsPromo"},
                {"googleBundleId", "com.extremedevelopers.toonboom"},
                {"amazonBundleId", "com.extremedevelopers.toonboomam"},
                {"displayName", "TOON WARS"},
                {"wsaPdpId", "9nblggh1z1wg"},
                {"iosId", "1016351204"},
                {"iosScheme", "com.extremedevelopers.toonboom"},
                {"macId", "1054873394"},
                {
                    "webUrls", new Dictionary<string, string>
                    {
                        {"Facebook", "https://apps.facebook.com/toon_wars"},
                        {"Vkontakte", "https://vk.com/ToonWarsGame"},
                        {"Odnoklassniki", "https://ok.ru/game/toonwars"},
                        {"Mail", "http://my.mail.ru/apps/736174"}
                    }
                }
            },
            new Dictionary<string, object>
            {
                {"id", "SpaceJet"},
                {"texture", "SpaceJetPromo"},
                {"googleBundleId", "com.extremedevelopers.spacejet"},
                {"amazonBundleId", "com.extremedevelopers.spacejet"},
                {"displayName", "SPACE JET"},
                {"wsaPdpId", "9nblggh6cp73"},
                {"iosId", "1047588988"},
                {"iosScheme", "com.extremedevelopers.spacejet"},
                {"macId", "1054873402"},
                {
                    "webUrls", new Dictionary<string, string>
                    {
                        {"Facebook", "https://apps.facebook.com/space_jet"},
                        {"Vkontakte", "https://vk.com/SpaceJet_Game"},
                        {"Odnoklassniki", "https://ok.ru/game/spacejet"},
                        {"Mail", "http://my.mail.ru/apps/738207"}
                    }
                }
            },
            new Dictionary<string, object>
            {
                {"id", "Armada"},
                {"texture", "ArmadaPromo"},
                {"googleBundleId", "com.extremedevelopers.armada"},
                {"amazonBundleId", "com.extremedevelopers.armada"},
                {"displayName", "ARMADA MODERN TANKS"},
                {"wsaPdpId", "9nblggh4w4xh"},
                {"iosId", "1137864805"},
                {"iosScheme", "com.extremedevelopers.armada"},
                {"macId", "1159273530"},
                {
                    "webUrls", new Dictionary<string, string>
                    {
                        {"Facebook", "https://apps.facebook.com/armada_world"},
                        {"Vkontakte", "https://vk.com/armada_tank_game"},
                        {"Odnoklassniki", "https://ok.ru/game/armada_world_of_modern_tanks"},
                        {"Mail", "http://my.mail.ru/apps/746811"}
                    }
                }
            },
            new Dictionary<string, object>
            {
                {"id", "Undefined"},
                {"texture", "GrandTanksPromo"},
                {"googleBundleId", "com.extremedevelopers.grandtanks"},
                {"amazonBundleId", "com.extremedevelopers.grandtanks"},
                {"displayName", "GRAND TANKS"},
                {"wsaPdpId", "9n7b2z93vnw7"},
                {"iosId", "1227399554"},
                {"iosScheme", "com.extremedevelopers.grandtanks"},
                {"macId", ""},//TODO: add actual value
                {
                    "webUrls", new Dictionary<string, string>
                    {
                        {"Facebook", "https://apps.facebook.com/grand_tanks"},
                        {"Vkontakte", "https://vk.com/app6014330_15317653"},
                        {"Odnoklassniki", "https://ok.ru/grandtanks"},
                        {"Mail", "https://my.mail.ru/apps/753746"}
                    }
                }
            },
            new Dictionary<string, object>
            {
                {"id", "BattleOfWarplanes"},
                {"texture", "BattleOfWarplanesPromo"},
                {"googleBundleId", "com.extremedevelopers.battleofwarplanes"},
                {"amazonBundleId", "com.extremedevelopers.battleofwarplanes"},
                {"displayName", "BATTLE OF WARPLANES"},
                {"wsaPdpId", "9nblggh6ggr3"},
                {"iosId", "1068916398"},
                {"iosScheme", "com.extremedevelopers.battleofwarplanes"},
                {"macId", ""},
                {
                    "webUrls", new Dictionary<string, string>
                    {
                        {"Facebook", "https://apps.facebook.com/battleofwarplanes"},
                        {"Vkontakte", "https://vk.com/bow_game"},
                        {"Odnoklassniki", "https://ok.ru/game/battleofwarplanes"},
                        {"Mail", "http://my.mail.ru/apps/740408"}
                    }
                }
            },
            new Dictionary<string, object>
            {
                {"id", "BattleOfHelicopters"},
                {"texture", "BattleOfHelicoptersPromo"},
                {"googleBundleId", "com.extremedevelopers.battleofhelicopters"},
                {"amazonBundleId", "com.extremedevelopers.battleofhelicopters"},
                {"displayName", "BATTLE OF HELICOPTERS"},
                {"wsaPdpId", "9nblggh5kqx5"},
                {"iosId", "1072045428"},
                {"iosScheme", "com.extremedevelopers.battleofhelicopters"},
                {"macId", ""},
                {
                    "webUrls", new Dictionary<string, string>
                    {
                        {"Facebook", "https://apps.facebook.com/battleofhelicopters"},
                        {"Vkontakte", "https://vk.com/app5334083"},
                        {"Odnoklassniki", "https://ok.ru/game/battleofhelicopters"},
                        {"Mail", "http://my.mail.ru/apps/742323"}
                    }
                }
            },
            new Dictionary<string, object>
            {
                {"id", "Undefined"},
                {"texture", "TanksVsRobotsPromo"},
                {"googleBundleId", "com.extremedevelopers.tanksvsrobots"},
                {"amazonBundleId", "com.extremedevelopers.tanksvsrobots"},
                {"displayName", "TANKS VS ROBOTS"},
                {"wsaPdpId", "9pb3502r2jqg"},
                {"iosId", "1236426650"},
                {"iosScheme", "com.extremedevelopers.tanksvsrobots"},
                {"macId", "1236426799"},
                {
                    "webUrls", new Dictionary<string, string>
                    {
                        {"Facebook", "https://apps.facebook.com/tanksvsrobots"},
                        {"Vkontakte", "https://vk.com/tanksvsrobots"},
                        {"Odnoklassniki", "https://ok.ru/game/1251136512"},
                        {"Mail", "https://my.mail.ru/apps/754019"}
                    }
                }
            },
            new Dictionary<string, object>
            {
                {"id", "Undefined"},
                {"texture", "TankForcePromo"},
                {"googleBundleId", "com.extremedevelopers.tankforce"},
                {"amazonBundleId", "com.extremedevelopers.tankforce"},
                {"displayName", "TANK FORCE"},
                {"wsaPdpId", "9p26r81l3j3f"},
                {"iosId", "1211576995"},
                {"iosScheme", "com.extremedevelopers.tankforce"},
                {"macId", "1223401325"},
                {
                    "webUrls", new Dictionary<string, string>
                    {
                        {"Facebook", "https://apps.facebook.com/tankforce"},
                        {"Vkontakte", "https://vk.com/app6110471_1585310"},
                        {"Odnoklassniki", "https://ok.ru/game/1252182528"},
                        {"Mail", "https://my.mail.ru/apps/755007"}
                    }
                }
            },

        }}
    };
    #endregion

    protected override void Create()
    {
        base.Create();

#if UNITY_WSA && !UNITY_WSA_10_0
        if (button != null) {
            button.SetActive (false);
        }
        return;
#endif
#pragma warning disable 162
        foreach (var p in promos["promos"] as List<object>)
        {
            var promo = GamePromo.fromDictionary(p as Dictionary<string, object>);
            if (promo.GameId != GameData.ClearGameFlags(GameData.CurrentGame)
                && !(RuntimePlatform.OSXPlayer == Application.platform && string.IsNullOrEmpty(promo.MacId))
                && !(RuntimePlatform.Android == Application.platform && GameData.IsGame(Game.AmazonBuild) && string.IsNullOrEmpty(promo.AmazonBundleId))
                && !(RuntimePlatform.Android == Application.platform && !GameData.IsGame(Game.AmazonBuild) && string.IsNullOrEmpty(promo.GoogleBundleId))
                && !(RuntimePlatform.IPhonePlayer == Application.platform && string.IsNullOrEmpty(promo.IosId))
                && !(RuntimePlatform.WebGLPlayer == Application.platform && (promo.webUrls.Count == 0)))
                gamesPromos.Add(promo);
        }
#pragma warning restore 162
    }

    protected override void Show()
    {
        base.Show();

        if (factory.Items.Count == 0)
            factory.CreateAll(gamesPromos);
    }

    public void OnMoreGamesClick()
    {
        GUIPager.SetActivePage("MoreGames", true);
    }
}

public class GamePromo
{
    public Game GameId { get; private set; }
    public string Texture { get; private set; }
    public string GoogleBundleId { get; private set; }
    public string AmazonBundleId { get; private set; }
    public string Name { get; private set; }
    public string WsaPdpId { get; private set; }
    public string IosId { get; private set; }
    public string IosScheme { get; private set; }
    public string MacId { get; private set; }
    public Dictionary<SocialPlatform, string> webUrls = new Dictionary<SocialPlatform, string>(); 

    public static GamePromo fromDictionary(Dictionary<string, object> dict)
    {
        var promo = new GamePromo();

        var data = new JsonPrefs(dict);
        promo.GameId = (Game)Enum.Parse(typeof(Game), data.ValueString("id"));
        promo.Texture = data.ValueString("texture", "");
        promo.GoogleBundleId = data.ValueString("googleBundleId", "");
        promo.AmazonBundleId = data.ValueString("amazonBundleId", "");
        promo.Name = data.ValueString("displayName", "");
        promo.WsaPdpId = data.ValueString("wsaPdpId", "");
        promo.IosId = data.ValueString("iosId", "");
        promo.IosScheme = data.ValueString("iosScheme", "");
        promo.MacId = data.ValueString("macId", "");
        var weburls = (Dictionary<string, string>)dict["webUrls"];
        foreach (var weburl in weburls)
        {
            var platform = (SocialPlatform) Enum.Parse(typeof (SocialPlatform), weburl.Key);
            promo.webUrls[platform] = (string)weburl.Value;
        }

        return promo;
    }
}
