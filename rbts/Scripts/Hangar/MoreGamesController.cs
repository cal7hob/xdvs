using UnityEngine;
using System;
using System.Collections.Generic;

public class MoreGamesController : MonoBehaviour 
{
    public GamePromoButton gamePromoPrefab;
    public GameObject container;
    public float horizontalSpaceBetweenItems = 10;
    public float verticalSpaceBetweenItems = 10;
    public int rowSize = 4;
    List<GamePromo> gamesPromos = new List<GamePromo>();
    public GameObject button;

    Dictionary<string, object> promos = new Dictionary<string, object>
    {
        {"promos", new List<object>
            {
                new Dictionary<string, object>
                {
                    {"id", "Undefined"},
                    {"texture", "MetalForcePromo"},
                    {"googleBundleId", "com.extremedevelopers.metalforce"},
                    {"amazonBundleId", "com.extremedevelopers.metalforce"},
                    {"displayName", "METAL FORCE"},
                    {"wsaPdpId", "9nt2lhrr5g56"},
                    {"wsaPhoneAppId", "9a978dee-9d32-4bb7-9c2a-da3549026306"},
                    {"wsaAppId", "edfe47c3-13e3-4bbd-bc2d-f3e01850acaa"},
                    {"iosId", "1223391730"},
                    {"iosScheme", "com.extremedevelopers.metalforce"},
                    {"macId", "1223395936"},
                    {
                        "webUrls", new Dictionary<string, string>
                        {
                            {"Facebook", "https://apps.facebook.com/metalforce"},
                            {"Vkontakte", "https://vk.com/app6014326"},
                            {"Odnoklassniki", "https://ok.ru/game/1250983168"},
                            {"Mail", "http://my.mail.ru/apps/753745"}
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
                    {"wsaPhoneAppId", "551ef081-4909-442c-a0ac-18180c43d8b6"},
                    {"wsaAppId", "f9fd0fef-88a9-4106-989d-01e7e8cd7bff"},
                    {"iosId", "1227399554"},
                    {"iosScheme", "com.extremedevelopers.grandtanks"},
                    {"macId", "1227401023"},
                    {
                        "webUrls", new Dictionary<string, string>
                        {
                            {"Facebook", "https://www.facebook.com/Grand-Tanks-Community-801754973309955"},
                            {"Vkontakte", "https://vk.com/app6014330_-145222597"},
                            {"Odnoklassniki", "https://ok.ru/group/53640484225119"},
                            {"Mail", "https://my.mail.ru/community/grandtanks"}
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
                    {"wsaPhoneAppId", "d2ca01e8-8663-4619-9b33-81ad149fc36c"},
                    {"wsaAppId", "8044b7ae-458f-4712-99b0-278af89e31e0"},
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
                    {"id", "IronTanks"},
                    {"texture", "IronTanksPromo"},
                    {"displayName", "IRON TANKS"},
                    {"googleBundleId", "com.extremedevelopers.irontanks"},
                    {"amazonBundleId", "com.extremedevelopers.irontanksam"},
                    {"wsaPdpId", "9nblggh0c2gx"},
                    {"wsaPhoneAppId", "1adee049-b6d7-4d12-967d-3774b810ab3f"},
                    {"wsaAppId", "c8136489-5bc4-4403-804c-c770cc518623"},
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
                    {"wsaPhoneAppId", "47539fca-0f7e-4ee8-aa16-ad7c8f30af12"},
                    {"wsaAppId", "b70de2a5-0385-4312-9dd6-6e3a5174457d"},
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
                    {"wsaPhoneAppId", "6c4764c9-3866-483e-93f6-4f40810b7433"},
                    {"wsaAppId", "47afcfeb-8abc-4673-abf0-85e25ac677e2"},
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
                    {"wsaPhoneAppId", "47c14c65-0198-4d18-bc84-1f8369226178"},
                    {"wsaAppId", "59957e25-ba67-4b88-b4f2-f2521cec8a84"},
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
                    {"id", "BattleOfWarplanes"},
                    {"texture", "BattleOfWarplanesPromo"},
                    {"googleBundleId", "com.extremedevelopers.battleofwarplanes"},
                    {"amazonBundleId", "com.extremedevelopers.battleofwarplanes"},
                    {"displayName", "BATTLE OF WARPLANES"},
                    {"wsaPdpId", "9nblggh6ggr3"},
                    {"wsaPhoneAppId", "2c6c0fc1-2355-40b3-bf85-ec84b85638f2"},
                    {"wsaAppId", "81604b53-4d05-4e41-a616-31c2978633f8"},
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
                    {"wsaPhoneAppId", "9a978dee-9d32-4bb7-9c2a-da3549026306"},
                    {"wsaAppId", "edfe47c3-13e3-4bbd-bc2d-f3e01850acaa"},
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
                /*new Dictionary<string, object>
                {
                    {"id", "MetalForce"},
                    {"displayName", "WORLD OF WALKING ROBOTS"},
                    {"wsaPdpId", "9nblggh52bmp"},
                    {"wsaPhoneAppId", ""},
                    {"wsaAppId", ""},
                    {"iosId", "1151825700"},
                    {"iosScheme", "com.extremedevelopers.metalforce"},
                    {"macId", ""},
                    {
                        "webUrls", new Dictionary<string, string>
                        {
                            {"Facebook", ""},
                            {"Vkontakte", ""},
                            {"Odnoklassniki", ""},
                            {"Mail", ""}
                        }
                    }
                },*/
            }
        }
    };
    void Awake () 
    {
#if UNITY_WSA && !UNITY_WSA_10_0
        if (button != null) {
            button.SetActive (false);
        }
        return;
#endif
#pragma warning disable 162
        if ((gamePromoPrefab == null) || (container == null))
        {
            Debug.LogError("MoreGamesController: missing required fields!");
            return;
        }

        foreach (var p in promos["promos"] as List<object>)
        {
            var promo = GamePromo.fromDictionary(p as Dictionary<string, object>);
            if(promo.GameId != GameData.ClearGameFlags(GameData.CurrentGame) 
                && !(RuntimePlatform.OSXPlayer == Application.platform && string.IsNullOrEmpty(promo.MacId))
                && !(RuntimePlatform.Android == Application.platform && GameData.IsGame(Game.AmazonBuild) && string.IsNullOrEmpty(promo.AmazonBundleId))
                && !(RuntimePlatform.Android == Application.platform && !GameData.IsGame(Game.AmazonBuild) && string.IsNullOrEmpty(promo.GoogleBundleId))
                && !(RuntimePlatform.IPhonePlayer == Application.platform && string.IsNullOrEmpty(promo.IosId))
                && !(RuntimePlatform.WebGLPlayer == Application.platform && (promo.webUrls.Count == 0)))
                gamesPromos.Add(promo);
        }
        Messenger.Subscribe(EventId.AfterHangarInit, StartInit);
#pragma warning restore 162
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.AfterHangarInit, StartInit);
    }

    private void StartInit(EventId id, EventInfo info)
    {
        XdevsSplashScreen.SetActiveWaitingIndicator(true);
        for (int index = 0; index < gamesPromos.Count; index++)
        {
            var promo = gamesPromos[index];
            var column = index % rowSize;
            var row = index / rowSize;
            var position = new Vector3(column*(gamePromoPrefab.HorizontalSize + horizontalSpaceBetweenItems),
                                       -row*(gamePromoPrefab.VerticalSize + verticalSpaceBetweenItems));
            var btn = (GamePromoButton)Instantiate(gamePromoPrefab, container.transform);
            btn.transform.localPosition = position;
            btn.SetPromo(promo);
        }
    }

    public void OnMoreGamesClick()
    {
        GUIPager.SetActivePage("MoreGames", true, true);
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
    public string WsaPhoneAppId { get; private set; }
    public string WsaAppId { get; private set; }
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
        promo.WsaPhoneAppId = data.ValueString("wsaPhoneAppId", "");
        promo.WsaAppId = data.ValueString("wsaAppId", "");
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
