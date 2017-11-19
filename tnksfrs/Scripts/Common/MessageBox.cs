using UnityEngine;
using System;
using System.Collections.Generic;

public class MessageBox : MonoBehaviour
{
	public enum Answer
	{
		Uncertain,
		No,
		Yes
	}

	public enum Type
	{
		Question,		//Yes,No
		Info,			//OK
		Hard,			//No buttons
		Critical		//automatic Application.Quit() on OK;
	}

	public class Data
	{
		public string text;
		public MessageBox.Type type;
		public int serverMessageId = -1;
		public Action<MessageBox.Answer> answerCallback;
		public Action callback;

		public Data(MessageBox.Type _type, string _text, Action<MessageBox.Answer> _callBack = null, int _serverMessageId = -1)
		{
			type = _type;
			text = _text;
			answerCallback = _callBack;
			serverMessageId = _serverMessageId;
		}

		public Data(MessageBox.Type _type, string _text, Action _callBack = null, int _serverMessageId = -1)
		{
			type = _type;
			text = _text;
			callback = _callBack;
			serverMessageId = _serverMessageId;
		}
	}

	public GameObject okButton;
    public GameObject question;
	public static bool duplicateToConsole;

    public static MessageBox Instance { get; private set; }
	private Data data;
	private List<Data> queue = new List<Data>();
    public GameObject wrapper;//Добавил чтобы можно было отключить месседжбокс в редакторе, а то мешает
    private int prevBlackAlfaLayerDepth = 0;//Костыль. Нужно делать полный рефактор чтобы управление BlackAlfaLayer-ом происходило через его добавление в объекты страницы в GuiPager
    private bool wasBlackAlphaLayerEnabled = false;

    public static bool IsShown
    {
        get
        {
            if (Instance == null)
                return false;
            if (Instance.wrapper == null)
                return Instance.gameObject.activeSelf;
            else
                return Instance.wrapper.activeSelf;
        }
    }

    void Awake()
	{
		Instance = this;
        SetActive(false);
    }

	void Start()
	{
		ShowHideGUIPage showHideGUIPage = transform.GetComponent<ShowHideGUIPage>();
		if (showHideGUIPage)
			showHideGUIPage.MoveToDefaultPosition();

	}

	void OnDestroy()
	{
		Instance = null;
	}

	public static void HideHardMessage()
	{
		
	}
	
	public static void Show(Data _data)
	{
		if(Instance == null || _data == null)
		{
			DT.LogError("Cant't show MessageBox! instance = {0}, _data = {1}", Instance == null ? "NULL" : "Exists", _data == null ? "NULL" : "Exists");
			return;
		}

		Instance.queue.Add(_data);
        
        if (!IsShown)
			Instance.Setup(_data);
	}

	private void Setup(Data _data)
	{
		Instance.data = _data;
		if (duplicateToConsole)
			Debug.Log("Message Box: " + _data.text);

        SetActive(true);

		switch (_data.type)
		{
			case Type.Critical:
			case Type.Info:
				Instance.okButton.SetActive(true);
				Instance.question.SetActive(false);
				break;
			case Type.Question:

				Instance.okButton.SetActive(false);
				Instance.question.SetActive(true);
				GUIPager.MoveBlackAlphaLayerOverTheInterface();
				break;
			case Type.Hard:
				Instance.okButton.SetActive(false);
				Instance.question.SetActive(false);
				break;
		}
        XdevsSplashScreen.SetActive(false);

    }

	public static void Show(MessageBox.Type _type, string _text, Action<MessageBox.Answer> _callBack = null, int _serverMessageId = -1)
	{
		Show(new Data(_type, _text, _callBack, _serverMessageId));
	}

    private void SetActive(bool en)
    {
        if (wrapper != null)
            wrapper.SetActive(en);
        else
            gameObject.SetActive(en);

        Dispatcher.Send(EventId.MessageBoxChangeVisibility, new EventInfo_B(en));

        if (GUIPager.Instance == null || GUIPager.Instance.blackAlphaLayer == null)
            return;
        if (en)
        {
            wasBlackAlphaLayerEnabled = GUIPager.Instance.blackAlphaLayer.activeSelf;
        }
        else
        {
            GUIPager.MoveBlackAlphaLayerToCustomOrder(prevBlackAlfaLayerDepth);
            GUIPager.Instance.blackAlphaLayer.SetActive(wasBlackAlphaLayerEnabled);
        }

    }
}
