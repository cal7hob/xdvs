using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Facebook.MiniJSON;
using UnityEngine;

public class PushwooshRequestManager : MonoBehaviour
{
    public const string PUSHWOOSH_API_PATH = "https://cp.pushwoosh.com/json/1.3";
    public static PushwooshRequestManager Instance { get; private set; }
    public static bool TagsSetWP8 { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        Instance = null;
    }

    public void CreateMessage(List<object> notifications)
    {
        var data = new Dictionary<string, object>
        {
            {"application", Pushwoosh.ApplicationCode},
            //{"auth", GameData.pushwooshApiToken},
            {"notifications", notifications}
        };

        var json = Json.Serialize(new Dictionary<string, object> { { "request", data } });
        Debug.Log(string.Format("creating message, json: {0}", json));

        ApiRequest(json, string.Format("{0}/createMessage", PUSHWOOSH_API_PATH));
    }

    public void SetTags(string hwid)
    {
        var data = new Dictionary<string, object>
        {
            {"application", Pushwoosh.ApplicationCode},
            {"hwid", hwid},
            {
                "tags",
                new Dictionary<string, object>()
                {
                    {"playerId", ProfileInfo.playerId},
                    {"playerLevel", ProfileInfo.Level},
                    //{"region", Http.Manager.Instance().Region.ToString() },
                }
            }
        };

        var json = Json.Serialize(new Dictionary<string, object> { { "request", data } });

        Debug.Log(string.Format("Setting tags for {0}, {1}", Pushwoosh.ApplicationCode, hwid));
        Debug.Log(string.Format("Tags: {0}", json));

        ApiRequest(json, string.Format("{0}/setTags", PUSHWOOSH_API_PATH));
    }


    public void ApiRequest(string json, string url)
    {
        var headers = new WebHeaderCollection { { "Content-Type", "text/json; charset=utf-8" } };
        var request = (HttpWebRequest)WebRequest.Create(url);
        request.Headers = headers;
        request.Method = "POST";
        request.ContentType = "text/json";
        var byteArray = Encoding.UTF8.GetBytes(json);
        var dataStream = request.GetRequestStream();
        dataStream.Write(byteArray, 0, byteArray.Length);
        dataStream.Close();
        var response = request.GetResponse();
        dataStream = response.GetResponseStream();

        if (dataStream == null)
        {
            Debug.LogError("Pushwoosh: failed to get response stream");
            return;
        }

        var reader = new StreamReader(dataStream);
        var responseFromServer = reader.ReadToEnd();
        Debug.LogFormat("Pushwoosh: api request to {0} response: {1}", url, responseFromServer);
        reader.Close();
        dataStream.Close();
        response.Close();
    }
}
