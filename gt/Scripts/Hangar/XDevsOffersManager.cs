using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class XDevsOffersManager : MonoBehaviour {

    public XDevs.ButtonsPanel.VerticalPanel buttonsPanel;
    public GameObject buttonPrefab;
    public tk2dSpriteCollectionData sprCollectionData;

    Dictionary<string, XDevs.Offers.GameOfferButton> buttons = new Dictionary<string, XDevs.Offers.GameOfferButton>();

    bool clickSending = false;

    public static XDevsOffersManager Instance { get; private set; }

    public static Dictionary<string, XDevs.Offers.GameOfferButton> Buttons { get { return Instance.buttons; } }

    void Awake()
    {
        Instance = this;
    }

    // Use this for initialization
    void Start ()
    {
        if ( (buttonsPanel == null) || (buttonPrefab == null) ) {
            Debug.LogError("XDevsOffersManager: buttonsPanel and buttonPrefab are required!");
            return;
        }

        Dispatcher.Subscribe(EventId.AfterHangarInit, StartInit);
        Dispatcher.Subscribe(EventId.ProfileInfoLoadedFromServer, OnProfileLoaded);
    }

    void OnDestroy()
    {
        Instance = null;

        Dispatcher.Unsubscribe(EventId.AfterHangarInit, StartInit);
        Dispatcher.Unsubscribe(EventId.ProfileInfoLoadedFromServer, OnProfileLoaded);
    }

    void StartInit(EventId id, EventInfo info)
    {
        // Отключаем показ рекламных офферов не на телефонах
#if UNITY_WSA && !WINDOWS_PHONE_APP && !UNITY_EDITOR
        return;
#endif
        if (GameData.gamesOffers.Count <= 0)
        {
            return;
        }

        foreach (var offer in GameData.gamesOffers)
        {
            if (!offer.enabled)
            {
                continue;
            }
            try {
                if (sprCollectionData.GetSpriteIdByName(offer.id, -1) == -1)
                {
                    Debug.LogError("Sprite for game offer " + offer.id + " not found!");
                    continue;
                }
                //TODO: Временный костыль чтобы в тунварсе не было одновременно 5 кнопок в правой панели. Во всех остальных играх влазит 5 кнопок.
                
                var go = Instantiate(buttonPrefab) as GameObject;
                var btn = go.GetComponent<XDevs.Offers.GameOfferButton>();
                btn.SetGame(offer);
                btn.Clicked += OnButtonClicked;
                buttonsPanel.AddButton(go.GetComponent<XDevs.ButtonsPanel.PanelButton>(), buttonsPanel.buttons.Count - 1);
                buttons[offer.id] = btn;
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
        CheckButtonsStates();
    }

    void OnProfileLoaded (EventId id, EventInfo info)
    {
        CheckButtonsStates();
    }

    void OnButtonClicked(XDevs.Offers.GameOfferButton btn)
    {
        Debug.Log("Offer button clicked");
        if (clickSending)
        {
            return;
        }
        clickSending = true;
        OfferClick(btn.gameOffer.id, (bool result) =>
        {
            if (result)
            {
                Application.OpenURL(btn.gameOffer.url);
                AddToQueue(btn.gameOffer.id);
            }
            clickSending = false;
        });
    }


    void CheckButtonsStates ()
    {
        bool state = true;
        foreach (var btn in buttons)
        {
            state = true;
            if (ProfileInfo.xdevsOffers.ContainsKey (btn.Value.gameOffer.id))
            {
                state = ProfileInfo.xdevsOffers[btn.Value.gameOffer.id] != ProfileInfo.XDevsOffersStates.Installed;
                if (state)
                {
                    AddToQueue(btn.Value.gameOffer.id);
                }
            }
            btn.Value.gameObject.SetActive(state);
        }
    }

    /// <summary>
    /// Отправка на сервер клика по офферу нашей игры
    /// </summary>
    /// <param name="game"></param>
    /// <param name="result"></param>
    public static void OfferClick (string game, System.Action<bool> result = null)
    {
        var request = Http.Manager.Instance().CreateRequest("/xdevs/offers/click/" + game);
        Http.Manager.StartAsyncRequest(request,
            delegate (Http.Response r) {
                if (result != null)
                {
                    result(true);
                }
            },
            delegate (Http.Response r) {
                if (result != null)
                {
                    result(false);
                }
            }
        );
    }

    /// <summary>
    /// Отправка подтверждения установки игры по офферу
    /// </summary>
    /// <param name="game"></param>
    /// <param name="result"></param>
    public static void OfferInstalled(string game, System.Action<bool> result = null)
    {
        var request = Http.Manager.Instance().CreateRequest("/xdevs/offers/install/" + game);
        Http.Manager.StartAsyncRequest(request,
            delegate (Http.Response r) {
                if (result != null)
                {
                    result(true);
                }
            },
            delegate (Http.Response r) {
                if (result != null)
                {
                    result(false);
                }
            }
        );
    }


#region Install offer checker
    bool started = false;
    bool busy = false;
    Queue<string> queue = new Queue<string> ();

    void AddToQueue (string game)
    {
        if (queue.Contains (game))
        {
            return;
        }

        Debug.Log("XDevsOffersManager::AddToQueue('"+ game + "')");
        queue.Enqueue(game);
        if (!started)
        {
            started = true;
            Debug.Log("XDevsOffersManager::Check - STARTED");
            Check();
        }
    }

    void Check ()
    {
#if !UNITY_EDITOR
        Debug.Log("XDevsOffersManager::Check");
        if (queue.Count <= 0)
        {
            Debug.Log("XDevsOffersManager::Check - STOPPED");
            started = false;
            return;
        }

        var g = queue.Peek();
        if (!buttons.ContainsKey(g))
        {
            queue.Dequeue();
            this.Invoke(Check, 1);
            return;
        }
        if (GameData.IsPackageIntalled(buttons[g].gameOffer.bundleId))
        {
            Debug.Log("XDevsOffersManager::Check - Package installed, Send info to server");
            OfferInstalled(g, delegate(bool result) {
                Debug.Log("XDevsOffersManager::Check - Install request done, result = " + result);
                if (result)
                {
                    queue.Dequeue();
                }
                else
                {
                    queue.Enqueue(queue.Dequeue());
                }
                this.Invoke(Check, 1);
            });
            return;
        }

        Debug.Log(string.Format("XDevsOffersManager::Check - Package '{0}' not installed", buttons[g].gameOffer.bundleId));
        queue.Enqueue(queue.Dequeue());
        this.Invoke(Check, 3);

#endif
    }

#endregion

}
