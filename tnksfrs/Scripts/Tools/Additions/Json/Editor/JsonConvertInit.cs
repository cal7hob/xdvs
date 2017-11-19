using UnityEditor.Callbacks;

namespace Newtonsoft.JsonEditor
{
    public static class JsonConvertInit
    {
        [DidReloadScripts]
        static void InitJsonConvert()
        {
            JsonConvert.serializeObjectHandler = Json.JsonConvert.SerializeObject;
            JsonConvert.deserializeObjectHandler = Json.JsonConvert.DeserializeObject;
        }
    }
}
