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

	public tk2dTextMesh textMesh;
	public GameObject okButton;
    public GameObject question;
    public GameObject wrapper;//Добавил чтобы можно было отключить месседжбокс в редакторе, а то мешает

    private Data data;
	private List<Data> queue = new List<Data>();
    
    private int prevBlackAlfaLayerDepth = 0;//Костыль. Нужно делать полный рефактор чтобы управление BlackAlfaLayer-ом происходило через его добавление в объекты страницы в GuiPager
    private bool wasBlackAlphaLayerEnabled = false;

    public static bool duplicateToConsole;
    private static MessageBox Instance;

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
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

	public static void HideHardMessage()
	{
		if (Instance != null && Instance.data != null && Instance.data.type == Type.Hard)
			Instance.OnButtonPushed(null);
	}
	
    private void OnButtonPushed(tk2dUIItem item)
    {
        if (data.type == Type.Critical)
        {
            GameData.QuitGame ();
        }
        else
        {
            SetActive(false);

            if (data.callback != null)
                data.callback ();
            if (data.answerCallback != null && item != null)
                data.answerCallback (item.name == "btnYes" ? Answer.Yes : Answer.No);
            data.callback = null;
            data.answerCallback = null;

            queue.RemoveAt (0);
            if (queue.Count > 0)
                Setup (queue[0]);
        }
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
        Instance.textMesh.text = _data.text;

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

        Messenger.Send(EventId.MessageBoxChangeVisibility, new EventInfo_B(en));

        if (GUIPager.Instance == null || GUIPager.Instance.blackAlphaLayer == null)
            return;
        if (en)
        {
            prevBlackAlfaLayerDepth = GUIPager.Instance.blackAlphaLayer.GetComponent<tk2dBaseSprite>().SortingOrder;
            wasBlackAlphaLayerEnabled = GUIPager.Instance.blackAlphaLayer.activeSelf;
        }
        else
        {
            GUIPager.MoveBlackAlphaLayerToCustomOrder(prevBlackAlfaLayerDepth);
            GUIPager.Instance.blackAlphaLayer.SetActive(wasBlackAlphaLayerEnabled);
        }
    }
}
