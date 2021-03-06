using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

[Serializable]
public class MusicBox : MonoBehaviour
{
    //public List<SceneMusic> musicList; // Не используется, оставлено временно.
    public string allTracksJson = "";
    public Dictionary<int, Dictionary<int, List<string>>> allTracks = new Dictionary<int, Dictionary<int, List<string>>>();

    private const float MUSIC_VOLUME_RATIO_BOH = 0.35f;

    private static MusicBox instance;

    private AudioSource audioSource;
    private List<string> currentSceneMusic = null;

    public static MusicBox Instance { get { return instance; } }

    /* UNITY SECTION */
    void Awake()
	{
		//if (musicList == null || musicList.Count == 0)
		//{
		//	enabled = false;
		//	return;
		//}
		//DT.Log("MusicBox.Awake, Application.loadedLevelName = {0}", Application.loadedLevelName);
		if (instance != null)
		{
            DT.LogWarning("MusicBox.Awake, Second Instance. Deleting It!, Application.loadedLevelName = {0}", SceneManager.GetActiveScene().name);// Application.loadedLevelName);
			Destroy(gameObject);
			return;
		}
		instance = this;
		DontDestroyOnLoad(gameObject);
		audioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
		audioSource.bypassEffects = true;
		audioSource.bypassListenerEffects = true;
		audioSource.bypassReverbZones = true;
		audioSource.loop = true;
		DeserializeAllTracks();

        SceneManager.activeSceneChanged += OnActiveSceneChanged;
	}

	private void OnDestroy()
	{
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        instance = null;
	}

	public void DeserializeAllTracks()
	{
		try
		{
			/*******************Десериализация через JsonNET*******************   Только в редакторе   */ 
			//myTarget.allTracks = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, List<string>>>>(myTarget.allTracksJson.Length > 0 ? myTarget.allTracksJson : "{}");

			/*******************Десериализация через MiniJson*******************/
			Dictionary<string, object> fullDic = MiniJSON.Json.Deserialize(allTracksJson) as Dictionary<string, object>;
			allTracks = new Dictionary<int, Dictionary<int, List<string>>>();

			foreach (KeyValuePair<string, object> gamePair in fullDic)
			{
				Dictionary<int, List<string>> mapDic = new Dictionary<int, List<string>>();
				Dictionary<string, object> mapDicObj = gamePair.Value as Dictionary<string, object>;
				foreach (KeyValuePair<string, object> mapPair in mapDicObj)
				{
					List<object> listObj = mapPair.Value as List<object>;
					List<string> list = new List<string>();
					foreach (var v in listObj)
						list.Add(v.ToString());
					mapDic.Add(int.Parse(mapPair.Key), list);
				}
				allTracks.Add(int.Parse(gamePair.Key), mapDic);
			}
		}
		catch (Exception e)
		{
			DT.LogError("Cant Deserialize tracks string {0}! Error = {1}.", allTracksJson, e.Message);
			//myTarget.allTracks = new Dictionary<int, Dictionary<int, List<string>>>();
			//SaveChanges(force: true);
		}
	}
    private Coroutine musicRoutine = null;
    private void StartMusicRoutine() 
    {
        if (musicRoutine != null)
        {
            StopCoroutine(musicRoutine);
            musicRoutine = null;
        }
        if (musicRoutine == null)
        {
            audioSource.clip = GetRandomClip();
            length = audioSource.clip.length;
            musicRoutine = StartCoroutine(MusicRoutine());
        }
    }
    private void OnActiveSceneChanged(Scene previousScene, Scene newScene)
	{
		//DT.Log("<color=orange>MusicBox.OnLevelWasLoaded {0}, allTracks.Count = {1}</color>", Application.loadedLevelName, allTracks.Count);
        if (SceneManager.GetActiveScene().name == GameManager.LOADING_SCENE_NAME) // На сцене лоадинг нет музыки.
        {
            return;
        }

        /*if (GameData.IsHangarScene && ProfileInfo.IsBattleTutorial) // Не играть ангарную музыку перед боевым тутором.
        {
            return;
        }*/

        currentSceneMusic = LoadMusic();

        if (currentSceneMusic == null || currentSceneMusic.Count == 0)
        {
            return;
        }
        StartMusicRoutine();
	}
    private AudioClip clip = null;
    private float length = 0f;    
    
    private static bool isPlayed = false;

    private IEnumerator MusicRoutine()
    {
        while (true) //Надо еще учитывать паузу и разные там меню
        {
            clip = GetRandomClip();
            yield return new WaitForSeconds(length);
            audioSource.clip = clip;
            if (isPlayed) 
            {
                Play();
            }
            length = clip.length;
        }
    }

    private AudioClip GetRandomClip() 
    {
        if (currentSceneMusic == null) 
        {
            return null;
        }
        string currentTrackName = currentSceneMusic.GetRandomItem();
		if (currentTrackName.Length == 0)
		{
			DT.LogError("Empty track for scene {0}", GameManager.CurrentMap);
			return null;
		}
			
		AudioClip audioClip = (AudioClip)Resources.Load(
            string.Format(
                "{0}/Music/{1}",
                GameManager.CurrentResourcesFolder,
                currentTrackName),
            typeof(AudioClip));

		if (audioClip == null)
		{
			DT.LogError("Cant find track {0} in resources!",currentTrackName);
			return null;
		}
		return audioClip;
    }

	/* PUBLIC SECTION */
	public static float Volume
	{
		get { return instance.audioSource.volume; }
		set
		{
		    float musicVolume = value;

            if (HelpTools.Approximately(musicVolume, 0))
			{
                Stop();
			}
			else
			{
				Play();
				instance.audioSource.volume = musicVolume;
			}
		}
	}

	public static void Play()
	{
        if (!instance && ProfileInfo.IsBattleTutorial)
        {
            return;
        }
        if (!isPlayed)
        {
            instance.StartMusicRoutine();
            isPlayed = true;
        }
		if (!instance.audioSource.isPlaying)
		{
            instance.audioSource.volume = Settings.MusicVolume;
			instance.audioSource.Play();
		}
	}

	public static void Stop()
	{
        if (!instance)
        {
            return;
        }
        if (isPlayed) 
        {
            instance.StopCoroutine(instance.musicRoutine);
            isPlayed = false;
        }
        
		if (instance.audioSource.isPlaying)
		{
			instance.audioSource.Stop();
			instance.audioSource.clip = null;
		}
	}

	//[System.Serializable]
	//public class SceneMusic
	//{
	//	public string sceneName;
	//	public GameManager.MapId sceneId;
	//	public List<string> trackList;
	//}

	/* PRIVATE  SECTION */
    private Dictionary<int, List<string>> res;
    private List<string> res_;
	private List<string> LoadMusic()
	{
		GameManager.MapId currentSceneId = GameManager.CurrentMap;
        if (currentSceneId == GameManager.MapId.LoadingScene)//На сцене лоадинг нет музыки
        {
            return null;
        }

        if (!allTracks.TryGetValue((int)GameData.CurInterface,out res) || !res.TryGetValue((int)currentSceneId, out res_))
        {
            return null;
        }
		return res_;
		//foreach (var sm in musicList)
		//{
		//	if (sm.sceneId == currentSceneId)
		//		return sm.trackList;

		//}
		//return null;
	}
}



