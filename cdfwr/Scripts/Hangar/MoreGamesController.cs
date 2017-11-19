using UnityEngine;
using System;
using System.Collections.Generic;
#if !UNITY_WSA
using System.Runtime.Remoting.Messaging;
#endif

public class MoreGamesController : MonoBehaviour
{
    public GamePromoButton gamePromoPrefab;
    public GameObject container;
    public float horizontalSpaceBetweenItems = 10;
    public float verticalSpaceBetweenItems = 10;
    public int rowSize = 4;
    List<GamePromo> gamesPromos = new List<GamePromo>();
    public GameObject button;

    [Header("Параметры для плавного выплывания окошек в MoreGames")]
    public bool activateOnEnable = true;
    public Vector3 deltaStartpos = new Vector3(-2000, 0, 0);
    public float smoothSpeed = 2.7f;
    public float delayBefore = 0.5f;

    Dictionary<string, object> promos = new Dictionary<string, object>
    {
        {"promos", new List<object>
        {
            new Dictionary<string, object>
            {
                {"id", "IronTanks"},
                {"displayName", "IRON TANKS"},
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
               new Dictionary<string, object>
            {
              {"id", "MetalForce"},
                {"displayName", "METAL FORCE"},
                {"wsaPdpId", "9nt2lhrr5g56"},
                {"wsaPhoneAppId", "860a4f35-f146-4aa7-b080-78b44e179133"},
                {"wsaAppId", "c5af05a6-f28c-4a7d-94bd-84409eee33a5"},
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
           {"id", "TanksVsRobots"},
                {"displayName", "TANKS VS ROBOTS"},
                {"wsaPdpId", "9pb3502r2jqg"},
                {"wsaPhoneAppId", "53786edd-af65-4b9d-b0b2-1971fea1c0d7"},
                {"wsaAppId", "53786edd-af65-4b9d-b0b2-1971fea1c0d7"},
                {"iosId", "1236426650"},
                {"iosScheme", "com.extremedevelopers.tanksvsrobots"},
                {"macId", "1236426799"},
                {
                    "webUrls", new Dictionary<string, string>
                    {
                        {"Facebook", "https://apps.facebook.com/tanksvsrobots"},
                        {"Vkontakte", "https://vk.com/app6035662"},
                        {"Odnoklassniki", "https://ok.ru/app/1251136512"},
                        {"Mail", "https://my.mail.ru/apps/754019"}
                    }
                }
            },
            new Dictionary<string, object>
            {
                {"id", "Armada"},
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
        }}
    };
    void Awake()
    {
#if UNITY_WSA && !UNITY_WSA_10_0
        if(button != null)button.SetActive(false);
	        return;
#endif
        if ((gamePromoPrefab == null) || (container == null))
        {
            Debug.LogError("MoreGamesController: missing required fields!");
            return;
        }

        foreach (var p in promos["promos"] as List<object>)
        {
            var promo = GamePromo.fromDictionary(p as Dictionary<string, object>);
            if (promo.id != GameData.ClearGameFlags(GameData.CurrentGame) && !(RuntimePlatform.OSXPlayer == Application.platform && string.IsNullOrEmpty(promo.macId)))
                gamesPromos.Add(promo);
        }
        Dispatcher.Subscribe(EventId.AfterHangarInit, StartInit);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, StartInit);
    }

    private void StartInit(EventId id, EventInfo info)
    {
        XdevsSplashScreen.SetActiveWaitingIndicator(true);
        for (int index = 0; index < gamesPromos.Count; index++)
        {
            var promo = gamesPromos[index];
            var column = index % rowSize;
            var row = index / rowSize;
            var position = new Vector3(column * (gamePromoPrefab.HorizontalSize + horizontalSpaceBetweenItems),
                                       -row * (gamePromoPrefab.VerticalSize + verticalSpaceBetweenItems));
            var btn = (GamePromoButton)Instantiate(gamePromoPrefab, container.transform);
            btn.transform.localPosition = position;
            btn.SetPromo(promo);
            btn.gameObject.AddComponent<SmoothTransformPosition>();
            var script = btn.GetComponent<SmoothTransformPosition>();
            script.deltaStartpos = deltaStartpos;
            script.activateOnEnable = activateOnEnable;
            script.delayBefore = delayBefore + index * 0.1f;
        }
    }

    public void OnMoreGamesClick()
    {
        GUIPager.SetActivePage("MoreGames");
    }
}

public class GamePromo
{
    public Game id = Game.Undefined;
    public string name = "";
    public string wsaPdpId = "";
    public string wsaPhoneAppId = "";
    public string wsaAppId = "";
    public string iosId = "";
    public string iosScheme = "";
    public string macId = "";
    public Dictionary<SocialPlatform, string> webUrls = new Dictionary<SocialPlatform, string>();

    public static GamePromo fromDictionary(Dictionary<string, object> dict)
    {
        var promo = new GamePromo();

        var data = new JsonPrefs(dict);
        promo.id = (Game)Enum.Parse(typeof(Game), data.ValueString("id"));
        promo.name = data.ValueString("displayName", "");
        promo.wsaPdpId = data.ValueString("wsaPdpId", "");
        promo.wsaPhoneAppId = data.ValueString("wsaPhoneAppId", "");
        promo.wsaAppId = data.ValueString("wsaAppId", "");
        promo.iosId = data.ValueString("iosId", "");
        promo.iosScheme = data.ValueString("iosScheme", "");
        promo.macId = data.ValueString("macId", "");
        var weburls = (Dictionary<string, string>)dict["webUrls"];
        foreach (var weburl in weburls)
        {
            var platform = (SocialPlatform)Enum.Parse(typeof(SocialPlatform), weburl.Key);
            promo.webUrls[platform] = (string)weburl.Value;
        }

        return promo;
    }
}
