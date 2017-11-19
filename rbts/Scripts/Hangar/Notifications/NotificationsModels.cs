using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JSONObject = System.Collections.Generic.Dictionary<string, object>;

namespace XDevs.Notifications.Models
{
    public enum Button
    {
        OK,
        Close,
        Acquire,
        Action
    }

    public enum Type
    {
        URL,
        Page,
    }

    public enum Page
    {
        Shop,
        Bank,
        Map,
    }

    public enum ShopTab
    {
        Tank,
        Camouflage,
        Decal
    }

    public enum AcquireType
    {
        Gold,
        Silver,
        Vip,
        Tank,
        Camouflage,
        Decal,
    }

    [Serializable]
    public class BankTabAction
    {
        [SerializeField] private Bank.Tab tab;
        [SerializeField] private string value;

        public Bank.Tab Tab { get { return tab; } }
        public string Value { get { return value; } }

        public BankTabAction(JSONObject jsonObject)
        {
            var prefs = new JsonPrefs(jsonObject);

            HelpTools.TryParseToEnum(prefs.ValueString("tab"), out tab, true);
            value = prefs.ValueString("value");
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class ShopTabAction
    {
        [SerializeField] private ShopTab tab;
        [SerializeField] private string value;

        public ShopTab Tab { get { return tab; } }
        public string Value { get { return value; } }

        public ShopTabAction(JSONObject jsonObject)
        {
            var prefs = new JsonPrefs(jsonObject);

            HelpTools.TryParseToEnum(prefs.ValueString("tab"), out tab, true);
            value = prefs.ValueString("value");
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class Action
    {
        [SerializeField] private Type type;
        [SerializeField] private string value;
        [SerializeField] private Page location;

        [SerializeField] private ShopTabAction shopTabAction;
        [SerializeField] private BankTabAction bankTabAction;

        public Type Type { get { return type; } }
        public string Value { get { return value; } }
        public Page Location { get { return location; } }

        public BankTabAction BankTabAction { get { return bankTabAction; } }
        public ShopTabAction ShopTabAction { get { return shopTabAction; } }

        public Action(JSONObject jsonObject)
        {
            var prefs = new JsonPrefs(jsonObject);

            HelpTools.TryParseToEnum(prefs.ValueString("type"), out type, true);

            switch (type)
            {
                case Type.Page:
                    HelpTools.TryParseToEnum(prefs.ValueString("location"), out location, true);

                    switch (location)
                    {
                        case Page.Bank:
                            bankTabAction =
                                new BankTabAction(jsonObject);
                            break;
                        case Page.Shop:
                            shopTabAction =
                                new ShopTabAction(jsonObject);
                            break;
                    }
                    break;
            }

            value = prefs.ValueString("value");
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class ActionButton
    {
        [SerializeField] private Button button;
        [SerializeField] private Action action;

        public Button Button { get { return button; } }
        public Action Action { get { return action; } }

        public ActionButton(JSONObject jsonObject)
        {
            var prefs = new JsonPrefs(jsonObject);

            HelpTools.TryParseToEnum(prefs.ValueString("button"), out button, true);

            action = new Action(prefs.ValueObjectDict("action"));
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class Notification
    {
        [SerializeField] private int id;
        [SerializeField] private string header;
        [SerializeField] private Uri imageUri;
        [SerializeField] private string text;
        [SerializeField] private List<ActionButton> buttons;

        [SerializeField] private Texture2D texture;

        public int Id { get { return id; } }
        public string Header { get { return header; } }
        public string Text { get { return text; } }
        public List<ActionButton> Buttons { get { return buttons; } }

        public Texture2D Texture { get { return texture; } }

        public bool loaded = false;

        public Notification(JSONObject jsonObject)
        {
            var notificationJSONPrefs = new JsonPrefs(jsonObject);

            id = notificationJSONPrefs.ValueInt("id");
            header = notificationJSONPrefs.ValueString("header");
            text = notificationJSONPrefs.ValueString("text");

            var imageUriString = notificationJSONPrefs.ValueString("imageUri");

            if (!string.IsNullOrEmpty(imageUriString)
                && Uri.IsWellFormedUriString(imageUriString, UriKind.Absolute))
            {
                try
                {
                    imageUri = new Uri(imageUriString);
                }
                catch (Exception ex)
                {
                    Debug.LogError("Notification ctor exception: " + ex);
                }
            }

            var buttonsJSONList = notificationJSONPrefs.ValueObjectList("buttons");

            if (buttonsJSONList.Count > 0)
                buttons = new List<ActionButton>();

            foreach (JSONObject button in buttonsJSONList)
            {
                Buttons.Add(new ActionButton(button));
            }

            NotificationsManager.Instance.StartCoroutine(Download());
        }

        public override bool Equals(object obj)
        {
            var other = obj as Notification;
            if (other == null)
                return false;

            return id == other.id;
        }

        public bool Equals(Notification other)
        {
            if (other == null)
                return false;

            return id == other.id;
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }

        private IEnumerator Download()
        {
            if (imageUri == null)
            {
                loaded = true;
                yield break;
            }

            WWW loader = new WWW(imageUri.ToString());
            yield return loader;

            DownloadFinished(loader);
        }

        private void DownloadFinished(WWW loader)
        {
            if (NotificationsManager.Dbg)
                Debug.LogError("Notification.DownloadFinished!");

            if (!string.IsNullOrEmpty(loader.error))
            {
                Debug.LogError("Notification.DownloadFinished error: " + loader.error);
                loaded = true;
                return;
            }

            texture = loader.texture;
            loaded = true;
        }
    }

    [Serializable]
    public class NotificationsModel
    {
        [SerializeField] private List<Notification> notifications;

        public List<Notification> Notifications { get { return notifications; } }

        public NotificationsModel(List<object> jsonObject)
        {
            notifications = new List<Notification>();

            foreach (JSONObject notification in jsonObject)
            {
                notifications.Add(new Notification(notification));
            }
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
}