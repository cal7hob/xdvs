using System;

namespace Newtonsoft.JsonEditor
{
    public static class JsonConvert
    {
        public delegate string SerializeObjectDelegate(object value);
        public static SerializeObjectDelegate serializeObjectHandler;

        public delegate object DeserializeObjectDelegate(string value, Type type);
        public static DeserializeObjectDelegate deserializeObjectHandler;

        public static T DeserializeObject<T>(string value)
        {
            return (deserializeObjectHandler == null) ? Activator.CreateInstance<T>() : (T) deserializeObjectHandler(value, typeof(T)); //return default(T);
        }

        public static string SerializeObject(object value)
        {
            return serializeObjectHandler == null ? value.ToString() : serializeObjectHandler(value);
        }
    }
}
