using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Facebook.MiniJSON;
using UnityEngine;

public class PushwooshRequestManager : MonoBehaviour
{
    private static readonly Dictionary<string, string> headers = new Dictionary<string, string>()
    {
        { "Content-Type", "text/json; charset=utf-8" }
    };

    public const string PUSHWOOSH_API_PATH = "https://cp.pushwoosh.com/json/1.3";
    public static PushwooshRequestManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        Instance = null;
    }

    public IEnumerator CreateMessage(List<object> notifications)
    {
        var data = new Dictionary<string, object>
        { 
            {"application", Pushwoosh.ApplicationCode},
            {"auth", GameData.pushwooshApiToken},
            {"notifications", notifications}
        };

        string json = Json.Serialize(new Dictionary<string, object> { { "request", data } });
        Debug.Log(string.Format("creating message, json: {0}", json));

        yield return CreateMessageApiRequest(json);
    }

    public IEnumerator SetTags(string hwid)
    {
        var data = new Dictionary<string, object>
        {
            {"application", Pushwoosh.ApplicationCode},
            {"hwid", hwid},
            {
                "tags", 
                new Dictionary<string, object>()
                {
                    {"playerId", ProfileInfo.profileId},
                    {"playerLevel", ProfileInfo.Level},
                    {"region", Http.Manager.Instance().Region.ToString() },
                }
            }
        };

        var json = Json.Serialize(new Dictionary<string, object> { { "request", data } });

        Debug.LogFormat("Setting tags for {0}, {1}.\nTags: {2}", Pushwoosh.ApplicationCode, hwid, json);

        var request = new WWW(PUSHWOOSH_API_PATH + "/setTags", Encoding.UTF8.GetBytes(json), headers);

        while (!request.isDone)
        {
            yield return null;
        }

        Debug.Log("pushwoosh setTags response: " + request.text);
        if (!string.IsNullOrEmpty(request.error))
            Debug.LogError("pushwoosh setTags error: " + request.error);
    }

    private static IEnumerator CreateMessageApiRequest(string json)
    {
        var request = new WWW(PUSHWOOSH_API_PATH + "/createMessage", Encoding.UTF8.GetBytes(json), headers);

        while (!request.isDone)
        {
            yield return null;
        }

        Debug.Log("pushwoosh creating message, response: " + request.text);
        if (!string.IsNullOrEmpty(request.error))
            Debug.Log("pushwoosh creating message, error: " + request.error);

        PlayerPrefs.SetString("prevPushwooshResponse", request.text);
        Debug.LogFormat("saving pushwoosh response {0}", request.text);
    }

    public IEnumerator DeleteMessageApiRequest(string message)
    {
        var data = new Dictionary<string, object>
        { 
            {"auth", GameData.pushwooshApiToken},
            {"message", message}
        };

        Debug.LogFormat("pushwoosh messageCode to delete: {0}", message);

        var json = Json.Serialize(new Dictionary<string, object> { { "request", data } });      
        var request = new WWW(PUSHWOOSH_API_PATH + "/deleteMessage", Encoding.UTF8.GetBytes(json), headers);
        Debug.LogFormat("pushwoosh deleting message, json: {0}", json);

        while (!request.isDone)
        {
            yield return null;
        }

        Debug.Log(string.Format("pushwoosh deleting message, response: {0}", request.text));
        if (!string.IsNullOrEmpty(request.error))
            Debug.Log(string.Format("pushwoosh deleting message, error: {0}", request.error));
    }
}
