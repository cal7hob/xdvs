using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using XDevs;
using System.Linq;

public enum GameLocation
{
    Hangar,
    Battle,
    Loading,
    Common
}

public enum AtlasId //индексы не менять
{
    //Hangar 0-99    Поидее на эти диапазоны ничего не завязано.
    Bank                = 1,
    ClansImages         = 2,
    DecalIcons          = 3,
    HangarGui           = 4,
    PatternIcons        = 5,
    SplashScreens       = 6,
    XDevsAdsOffers      = 7,
    
    //Battle 100-199
    BattleGui           = 100,

    //Hangar and Battle 200-299
    Consumables         = 200,
    CountryFlags        = 201,
    Universal           = 202,

    Common              = 300,//project specific high quality atlas ( only 2x)
}

public  class AtlasesManager: MonoBehaviour
{
    [Serializable]
    public class Atlas
    {
        public AtlasId id;
        public List<GameLocation> locations;
        public tk2dSpriteCollectionData spriteCollectionData;
        public bool IsPermanent{ get; set;}//atlas that never unloads
        public List<string> SpriteList { get { return spriteCollectionData.inst.spriteDefinitions.Select(sprite => sprite.name).Cast<string>().ToList(); } }//for CyclicActions
    }

    public static AtlasesManager Instance { get; private set; }
    [SerializeField] private List<Atlas> atlasesList;
    public List<Atlas> AtlasesList { get { return atlasesList; } }

    private Dictionary<AtlasId, Atlas> atlasesDic = new Dictionary<AtlasId, Atlas>();
    private Dictionary<GameLocation, Dictionary<AtlasId, Atlas>> atlasesByLocationDic = new Dictionary<GameLocation, Dictionary<AtlasId, Atlas>>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        gameObject.name = gameObject.name.Replace("(Clone)", "");

        FillDictionaries(atlasesList, atlasesDic, atlasesByLocationDic);
        
        SceneManager.sceneLoaded += OnLoadScene;
        SceneManager.sceneUnloaded += OnUnloadScene;
    }

    /// <summary>
    /// This method is used also in editor scripts, like CyclicActions
    /// </summary>
    public static void FillDictionaries(List<Atlas> _atlasesList, Dictionary<AtlasId, Atlas> _atlasesDic, Dictionary<GameLocation, Dictionary<AtlasId, Atlas>> _atlasesByLocationDic)
    {
        _atlasesDic.Clear();
        _atlasesByLocationDic.Clear();

        foreach (var loc in Enum.GetValues(typeof(GameLocation)))
            _atlasesByLocationDic[(GameLocation)loc] = new Dictionary<AtlasId, Atlas>();

        for (int i = 0; i < _atlasesList.Count; i++)
        {
            _atlasesList[i].IsPermanent = _atlasesList[i].locations.Contains(GameLocation.Hangar) && _atlasesList[i].locations.Contains(GameLocation.Battle);//cashing IsPermanent property
            _atlasesDic[_atlasesList[i].id] = _atlasesList[i];
            for (int j = 0; j < _atlasesList[i].locations.Count; j++)
                _atlasesByLocationDic[_atlasesList[i].locations[j]].Add(_atlasesList[i].id, _atlasesList[i]);
        }
    }

    public static List<Atlas> GetGameAtlasesList(Interface iface)
    {
        GameObject atlasManagerGo = (GameObject)Resources.Load(string.Format("{0}/{1}/AtlasesManager{2}", iface, "Other", GameData.GetInterfaceShortName(iface).ToUpper()));
        if (!atlasManagerGo)
        {
            Debug.LogErrorFormat("Cant load AtlasManager for project {0}", iface);
            return new List<Atlas>();
        }
            
        AtlasesManager atlasesManager = atlasManagerGo.GetComponent<AtlasesManager>();
        return atlasesManager.AtlasesList;
    }

    private void OnDestroy()
    {
        Instance = null;
        SceneManager.sceneLoaded -= OnLoadScene;
        SceneManager.sceneUnloaded -= OnUnloadScene;
#if UNITY_EDITOR
        LoadAllLocationsAtlases();// Загружаем текстуры всех атласов при выходе из плей мода
#endif
    }

    private void OnLoadScene(Scene scene, LoadSceneMode mode)
    {
        //Debug.LogErrorFormat("Loaded scene {0}, Mode = {1}", scene.name, mode);
        LoadAtlasTexturesForLocation(GetSceneLocation(scene.name));
    }

    private void OnUnloadScene(Scene scene)
    {
        //Debug.LogErrorFormat("Unloaded scene {0}", scene.name);
        UnloadAtlasTexturesForLocation(GetSceneLocation(scene.name));
    }

    public void LoadAtlasTexturesForLocation(GameLocation loc)
    {
        foreach (var pair in atlasesByLocationDic[loc])
        {
            if (pair.Value.IsPermanent)
                continue;
            //Debug.LogErrorFormat("Loaded Atlas {0}", pair.Value.id);
            pair.Value.spriteCollectionData.ReloadTextures();
        }
    }

    public void UnloadAtlasTexturesForLocation(GameLocation loc)
    {
        foreach (var pair in atlasesByLocationDic[loc])
        {
            if (pair.Value.IsPermanent)
                continue;
            //Debug.LogErrorFormat("Unloaded Atlas {0}", pair.Value.id);
            pair.Value.spriteCollectionData.UnloadTextures();
        }
            
    }

    private void LoadAllLocationsAtlases()
    {
        foreach (var loc in Enum.GetValues(typeof(GameLocation)))
            LoadAtlasTexturesForLocation((GameLocation)loc);
    }

    /// <summary>
    /// Вынести в какой-нить Манагер сцен
    /// </summary>
    public static GameLocation GetSceneLocation(string sceneName)
    {
        if (sceneName == GameManager.LOADING_SCENE_NAME)
            return GameLocation.Loading;
        else if (sceneName.StartsWith("Hangar_") || sceneName.StartsWith("scnh_"))
            return GameLocation.Hangar;
        else if (sceneName.StartsWith("scnb_") || sceneName.StartsWith("Battle_"))
            return GameLocation.Battle;
        else
        {
            Debug.LogErrorFormat("Unknown structure of scene name {0}", sceneName);
            return GameLocation.Loading;
        }
    }

    public static tk2dSpriteCollectionData GetAtlasDataById(AtlasId id)
    {
        if (Instance == null || !Instance.atlasesDic.ContainsKey(id))
            return null;

        return Instance.atlasesDic[id].spriteCollectionData;
    }

    public static tk2dSpriteCollectionData GetAtlasDataByEntity(EntityTypes entity)
    {
        if (Instance == null)
            return null;
        return GetAtlasDataById(GetAtlasIdForEntity(entity));
    }

    public static Atlas GetAtlasById(AtlasId id)
    {
        if (Instance == null || !Instance.atlasesDic.ContainsKey(id))
            return null;

        return Instance.atlasesDic[id];
    }

    public static AtlasId GetAtlasIdForEntity(EntityTypes entity)
    {
        switch (entity)
        {
            case EntityTypes.money: return AtlasId.Bank;
            case EntityTypes.consumable: return AtlasId.Consumables;
            case EntityTypes.cam: return AtlasId.PatternIcons;
            case EntityTypes.decal: return AtlasId.DecalIcons;
            default: return AtlasId.HangarGui;
        }
    }

    //#if UNITY_EDITOR
    //    [UnityEditor.MenuItem("HelpTools/Reload Textures in scene %#r")]
    //    public static void ReloadTexturesEditor()
    //    {
    //        //LoadAllLocationsAtlases();
    //    }
    //#endif
}
