using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Http
{
    public class Response
    {
        Request m_request;
        Dictionary<string, object> m_data = null;
        JsonPrefs m_prefs;

        bool m_haveErrors = true;
        string m_error = "";
        long m_status = -1;
        int m_httpStatusCode = 0;
        string m_httpStatusMessage = "";

        int m_serverErrorCode = 0;
        Error m_serverError = Error.NoError;

        public bool HaveErrors {
            get { return m_haveErrors; }
        }

        public bool IsNetworkError {
            get {
                if (!m_haveErrors) {
                    return false;
                }

                return m_request.Result.responseHeaders.Count == 0;
            }
        }

        public string text {
            get { return m_request.Result.text; }
        }

        public string error {
            get { return m_error; }
        }

        public long status {
            get { return m_status; }
        }

        public Error ServerError
        {
            get { return m_serverError; }
        }

        public Dictionary<string, string> Headers {
            get { return m_request.Result.responseHeaders; }
        }

        public Dictionary<string, object> Data
        {
            get { return m_data; }
        }

        public JsonPrefs Prefs
        {
            get { return m_prefs; }
        }

        public int HttpStatusCode
        {
            get { return m_httpStatusCode; }
        }

        public string HttpStatusMessage {
            get { return m_httpStatusMessage; }
        }

        public Response (Request request, bool doDecodeNow = true) {
#if UNITY_EDITOR
            // Поиск заголовка профайлера симфони
            if (request.Result.responseHeaders.ContainsKey("X-DEBUG-TOKEN-LINK"))
            {
                string path = request.Result.responseHeaders["X-DEBUG-TOKEN-LINK"];
                var uri = new Uri(request.Result.url);
                path = uri.Scheme + "://" + uri.Host + ":" + uri.Port + path;
                Debug.LogFormat("<color='#337ab7'>Profiler: {0} </color>", path);
            }
#endif

            m_request = request;
            ParseStatus ();

            if (!string.IsNullOrEmpty (m_request.Result.error)) {
                m_error = m_request.Result.error;
                m_serverError = Error.InternalServerError;
                Debug.LogWarningFormat ("Request '{0}' error: {1}", m_request.Url, m_error);
                if (HttpStatusCode == 503) {
                    var p = new JsonPrefs (text);
                    int status = p.ValueInt ("status", -1);
                    int error = p.ValueInt ("error", -1);
                    string msg = p.ValueString ("message");
                    if (status == 0 && error > 0 && !string.IsNullOrEmpty (msg)) {
                        m_serverError = (Error)error;
                        Debug.LogError (msg);
                    }
                }
                return;
            }
            if (!m_request.Result.responseHeaders.ContainsKey (Manager.signatureHeader)) {
                m_error = "Request doesn't contain header '" + Manager.signatureHeader + "'";
                m_serverError = Error.InternalServerError;
                Debug.LogWarning (m_error);
                return;
            }

            string incomingSig = m_request.Result.responseHeaders[Manager.signatureHeader];
            string dataSig = Manager.computeHash(GameData.instance.AuthenticationKey + m_request.Headers[Manager.signatureHeader] + m_request.Result.text);

            if (dataSig != incomingSig) {
                m_error = "Wrong check answer signature. May be HACK!";
                m_serverError = Error.InternalServerError;
                Debug.LogWarning (m_error);
                Debug.Log ("Signatures: in='" + incomingSig + "', computed='" + dataSig + "'");
                Manager.dbg ("Response body: " + m_request.Result.text);
                return;
            }

            if (doDecodeNow) {
                try {
                    m_data = MiniJSON.Json.Deserialize (m_request.Result.text) as Dictionary<string, object>;
                    m_prefs = new JsonPrefs (m_data);
                }
                catch (Exception e) {
                    m_error = "JSON parse error: " + e.Message + ", text: " + m_request.Result.text;
                    m_serverError = Error.InternalServerError;
                    Debug.LogWarning (m_error);
                    return;
                }
                AnalyzeResponse ();
            }
            else {
                m_haveErrors = false;
            }
        }

#if !UNITY_WSA
        public IEnumerator DecodeResponse (Action<bool> result)
        {
            m_haveErrors = true;
            string error = null;
            bool isDecoded = false;
            ThreadedJsonDecoder.Decode (m_request.Result.text, delegate(object d, string err){
                m_data = d as Dictionary<string, object>;
                error = err;
                isDecoded = true;
            });
            while (!isDecoded) {
                yield return new WaitForSeconds(0.1f);
            }
            if (!string.IsNullOrEmpty (error)) {
                m_error = "JSON parse error: " + error + ", text: " + m_request.Result.text;
                Debug.LogWarning (m_error);
                result (m_haveErrors);
                yield break;
            }
            AnalyzeResponse ();
            result (m_haveErrors);
        }
#endif

        void AnalyzeResponse ()
        {
            if (m_data == null) {
                m_error = "JSON parse result is NULL, text: " + m_request.Result.text;
                m_serverError = Error.InternalServerError;
                Debug.LogWarning (m_error);
                return;
            }

            object o;
            if (!m_data.TryGetValue ("status", out o)) {
                m_error = "JSON result doesn't contain key 'status'";
                m_serverError = Error.InternalServerError;
                Debug.LogWarning (m_error);
                return;
            }

            try {
                m_status = System.Convert.ToInt64 (o);
            }
            catch (Exception e) {
                Debug.LogError ("Can't convert 'status' value to int64 value, error: " + e.Message);
                m_serverError = Error.InternalServerError;
                return;
            }
            if (m_status == 0) {
                m_error = "JSON result 'status' equals to 0";
                if (m_data.ContainsKey ("error")) {
                    m_serverErrorCode = System.Convert.ToInt32 (m_data["error"]);
                    m_serverError = (Error)m_serverErrorCode;
                    m_error += ", error: " + m_serverErrorCode + " (" + m_serverError.ToString () + ")";

                    if (m_serverError == Error.PlayerUnknownToken) {
                        GameData.QuitGame ();
                        return;
                    }
                }
                Debug.LogWarning (m_error);
#if UNITY_EDITOR
                Debug.LogWarning (m_request.Result.text);
#endif
                if (m_data.ContainsKey ("banned")) {
                    GameData.QuitGame ();
                }
                return;
            }
            Manager.dbg ("Response body: " + m_request.Result.text);
            m_haveErrors = false;
            if (m_data.ContainsKey ("device")) {
                string devId = m_data["device"] as string;
                if (!string.IsNullOrEmpty (devId)) {
                    ProfileInfo.AppGuid = devId;
                }
            }

            if (m_data.ContainsKey ("profileChanges")) {
                var profile = m_data["profileChanges"] as Dictionary<string, object>;
                if (profile == null) {
                    Debug.LogWarning ("Can't read profile changes from server answer");
                    return;
                }
                ProfileInfo.ApplyProfileChanges (profile);
            }
        }

        void ParseStatus ()
        {
            string l_status = "";
            if (m_request.Result.responseHeaders.ContainsKey ("NULL")) {
                l_status = m_request.Result.responseHeaders["NULL"];
            }
            if (m_request.Result.responseHeaders.ContainsKey ("STATUS")) {
                l_status = m_request.Result.responseHeaders["STATUS"];
            }
            if (!string.IsNullOrEmpty (l_status)) {
                var parts = l_status.Split (new char[]{' '}, 3);
                if (parts.Length > 2) {
                    m_httpStatusCode = Convert.ToInt32 (parts[1]);
                    m_httpStatusMessage = parts[2];
                }
            }
            else if (m_request.Result.responseHeaders.Count > 0) {
                //Manager.dbg ("Status header not found! Response headers: " + MiniJSON.Json.Serialize (m_request.Result.responseHeaders));
            }
        }
    }
}
