using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public static class AETools
{
    public class ListInt
    {
        /// <summary>
        /// Serialize list in one int32 (32bit)
        /// </summary>
        /// <param name="size">count bit on one item max all item 32 bit</param>
        public ListInt(int size) { Deserialize(size, ClearValue); }
        public ListInt(int size, int value) { Deserialize(size, value); }

        protected List<int> values;
        private int capacity = 0;
        private int size = 1;
        private int maxValue = 1;

        public void Deserialize(int size, int value)
        {
            if (size > 32 || size < 1)
            {
                return;
            }

            this.size = size;
            capacity = 32 / size;
            values = new List<int>(capacity);
            maxValue = (int)Math.Pow(2, size) - 1;
            //CL.Log("size: " + size + ", capacity: " + capacity + ", maxValue: " + maxValue);
            BitArray source = new BitArray(new int[] { value });
            int val;
            int index = 0;
            BitArray target;
            values.Clear();
            for (int i = 0; i < capacity; i++)
            {
                target = new BitArray(size, false);
                Copy(ref source, ref target, index, 0, size);
                index += size;
                if ((val = target.ToInt()) != maxValue)
                {
                    values.Add(val);
                }
            }
        }

        public int Serialize()
        {
            BitArray result = new BitArray(32, true);
            BitArray source;
            int index = 0;
            for (int i = 0; i < values.Count; i++)
            {
                source = new BitArray(new int[] { values[i] });
                Copy(ref source, ref result, 0, index, size);
                index += size;
            }

            return result.ToInt();
        }

        private void Copy(ref BitArray source, ref BitArray target, int indexSource, int indexTarget, int count)
        {
            for (int i = 0; i < count; i++)
            {
                target[indexTarget] = source[indexSource];
                indexTarget++;
                indexSource++;
            }
        }

        public static int ClearValue { get { return -1; } } //-1 int.MaxValue

        public virtual void Add(int value)
        {
            if (values.Count >= capacity) return;
            values.Add(value);
        }

        public int Capacity { get { return capacity; } }
        public int Size { get { return size; } }
        public int MaxValue { get { return maxValue; } }

        public int this[int index] { get { return values[index]; } set { values[index] = value; } }
        public int Count { get { return values.Count; } }
        public void Clear() { values.Clear(); }
        public IEnumerator GetEnumerator() { return values.GetEnumerator(); }
    }

    /*public abstract class Setting<T> : ScriptableObject where T : Setting<T>
    {
        [NonSerialized]
        private const string pathSetting = "Assets/Resources/"; //"Library" + Path.DirectorySeparatorChar + "EditorSetting" + Path.DirectorySeparatorChar;
        protected static string fileSetting;
        [NonSerialized]
        protected static T single;

        public virtual void Init() {  } //Set();

        public virtual void Clear()
        {
            (single = CreateInstance<T>()).Init(); //Activator.CreateInstance<T>()
        }

        public static T Get()
        {
            if (single == null)
            {
                typeof(T)
#if NETFX_CORE
                    .GetTypeInfo()
#endif
                    .GetMethod("InitPath").Invoke(null, new object[0]);
                

                single = Resources.Load<T>(fileSetting); //AETools.LoadObjectJson<T>(pathSetting);
                if (single == null)
                {
                    //Debug.Log("Create");
                    single = CreateInstance<T>(); //Activator.CreateInstance<T>();
                    single.Init();
                }
            }
            return single;
        }

        public static void Set()
        {
#if UNITY_EDITOR //AETools.SetJson(pathSetting, single);
            if (File.Exists(pathSetting + fileSetting + ".asset"))//Resources.Load<T>(fileSetting) == null
            {
                //Debug.Log("SetAsset");
                UnityEditor.EditorUtility.SetDirty(single);
            }
            else
            {
                //Debug.Log("CreateAsset");
                UnityEditor.AssetDatabase.CreateAsset(single, pathSetting + fileSetting + ".asset");
            }
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }*/

    /*public abstract class Setting<T> : ScriptableObject where T : Setting<T>
    {
        [NonSerialized]
        protected static string pathSetting
#if UNITY_EDITOR
            = "EditorSetting/";//"Library/EditorSetting/" //"Assets/"
        protected static string pathSettingResource
//#else
//            , pathSettingResource
#endif
            = "Setting/";

        [NonSerialized]
        protected static T single;

        public virtual void Init()
        {
            //Set();
        }

        public virtual void Clear()
        {
            (single = Activator.CreateInstance<T>()).Init();
        }

        public static T Get()
        {
            if (single == null)
            {
                typeof(T)
#if NETFX_CORE
                    .GetTypeInfo()
#endif
                    .GetMethod("InitPath").Invoke(null, new object[0]);
#if UNITY_EDITOR
                single = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(pathSetting);  //AETools.LoadObjectJson<T>(pathSetting);
#else
                single = Resources.Load<T>(pathSetting);
#endif
                if (single == null)
                {
                    (single = Activator.CreateInstance<T>()).Init();
#if UNITY_EDITOR
                    UnityEditor.AssetDatabase.CreateAsset(single, pathSetting);
#endif
                }
            }
            return single;
        }

        public static void Set()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty((UnityEngine.Object)single);
            UnityEditor.AssetDatabase.SaveAssets();
#endif

            //AETools.SetJson(pathSetting, single);
        }

        public static void SetResource()
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.CreateAsset(single, pathSettingResource);
#endif
        }

        public static void DeleteResource()
        {
#if UNITY_EDITOR
            File.Delete(pathSettingResource);
#endif
        }

        //public abstract void InitPath();
    }*/

    /// <summary>
    /// Get Param
    /// </summary>
    /// <param name="source"></param>
    /// <param name="output"></param>
    /// <param name="startIndex"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public static bool GetParam(this string source, out string output, ref int startIndex, string start, string end)
    {
        startIndex = source.IndexOf(start, startIndex);
        if (startIndex != -1)
        {
            startIndex = startIndex + start.Length;
            int startEnd = source.IndexOf(end, startIndex);
            if (startEnd != -1)
            {
                output = source.Substring(startIndex, startEnd - startIndex);
                startIndex = startEnd + end.Length;
                return true;
            }
        }
        output = null;
        return false;
    }

    /// <summary>
    /// Get Param 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="output"></param>
    /// <param name="startIndex"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public static bool GetParam(this string source, out string output, ref int startIndex, string end)
    {
        int startEnd = source.IndexOf(end, startIndex);
        if (startEnd != -1)
        {
            output = source.Substring(startIndex, startEnd - startIndex);
            startIndex = startEnd + end.Length;
            return true;
        }
        output = null;
        return false;
    }

    /*
    /// <summary>
    /// Copy object in object other type
    /// </summary>
    /// <param name="source"></param>
    /// <param name="result"></param>
    public static void Copy(object source, ref object result)
    {
        //CL.Log();
        Type type = source.GetType();

        if (type.IsClass)
        {
            if (source == null) return;
            if (type.Equals(typeof(string)))
            {
                result = source;
                return;
            }
            //if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return source;

            object item;
            if (type.IsArray)
            {
                Array sourceI = source as Array;
                int sourceLength = sourceI.Length;
                Array resultI = result as Array; //Array.CreateInstance(result.GetType().GetElementType(), sourceLength);

                for (int i = 0; i < sourceLength; i++)
                {
                    item = resultI.GetValue(i);
                    Copy(sourceI.GetValue(i), ref item);
                    resultI.SetValue(item, i);
                }
                return;
            }

            if (typeof(IList).IsAssignableFrom(type))
            {
                IList resultI = result as IList;
                IList sourceI = source as IList;

                for (int i = 0; i < sourceI.Count; i++)
                {
                    item = resultI[i];
                    Copy(sourceI[i], ref item);
                }
                return;
            }

            FieldInfo fieldInfoResult;
            foreach (FieldInfo fieldInfo in type.GetFields())
            {
                fieldInfoResult = result.GetType().GetField(fieldInfo.Name);
                item = fieldInfoResult.GetValue(result);
                Copy(fieldInfo.GetValue(source), ref item);
                fieldInfoResult.SetValue(result, item);
                //result.GetType().GetField(fieldInfo.Name).SetValue(result, fieldInfo.GetValue(source));
            }
        }
        result = source;
    }

    /// <summary>
    /// Copy object in object other type, for each property result and copy in source
    /// </summary>
    /// <param name="source"></param>
    /// <param name="result"></param>
    public static void CopyR(object source, ref object result)
    {
        //CL.Log();
        Type type = source.GetType();

        if (type.IsClass)
        {
            if (source == null) return;
            if (type.Equals(typeof(string)))
            {
                result = source;
                return;
            }

            object item;
            if (type.IsArray)
            {
                Array sourceI = source as Array;
                Array resultI = result as Array; //Array.CreateInstance(result.GetType().GetElementType(), sourceLength);
                int resultLength = resultI.Length;


                for (int i = 0; i < resultLength; i++)
                {
                    item = resultI.GetValue(i);
                    Copy(sourceI.GetValue(i), ref item);
                    resultI.SetValue(item, i);
                }
                return;
            }

            if (typeof(IList).IsAssignableFrom(type))
            {
                IList resultI = result as IList;
                IList sourceI = source as IList;

                for (int i = 0; i < sourceI.Count; i++)
                {
                    if (i < resultI.Count)
                    {
                        item = resultI[i];
                    }
                    else
                    {
                        item = Activator.CreateInstance(result.GetType().GetGenericArguments()[0]);
                        resultI.Add(item);
                    }
                    CopyR(sourceI[i], ref item);
                }
                return;
            }

            FieldInfo fieldInfoSource;
            foreach (FieldInfo fieldInfo in result.GetType().GetFields()) //result.GetType().GetFields()
            {
                fieldInfoSource = source.GetType().GetField(fieldInfo.Name);
                item = fieldInfo.GetValue(result);
                CopyR(fieldInfoSource.GetValue(source), ref item);
                fieldInfo.SetValue(result, item);
                //fieldInfo.SetValue(result, source.GetType().GetField(fieldInfo.Name).GetValue(source));
            }
        }
        result = source;
    }
    */

    /*
//=============================================================================================================================
//Player prefs get and set class
//=============================================================================================================================
    delegate object GetValue(string name);
    private static Dictionary<Type, GetValue> getValue = new Dictionary<Type, GetValue>()
        {
            { typeof(int),          delegate(string name) { if (!PlayerPrefs.HasKey(name)) return null; return PlayerPrefs.GetInt(name);        }        },
            { typeof(float),        delegate(string name) { if (!PlayerPrefs.HasKey(name)) return null; return PlayerPrefs.GetFloat(name);      }        },
            { typeof(bool),         delegate(string name) { if (!PlayerPrefs.HasKey(name)) return null; return PlayerPrefs.GetInt(name) != 0;   }        },
            { typeof(string),       delegate(string name) { if (!PlayerPrefs.HasKey(name)) return null; return PlayerPrefs.GetString(name);     }        },
            { typeof(Vector2),      delegate(string name) { if (!PlayerPrefs.HasKey(name)) return null; string[] xy = PlayerPrefs.GetString(name).Split(','); return new Vector2(float.Parse(xy[0]), float.Parse(xy[1]));                           }        },
            { typeof(Vector3),      delegate(string name) { if (!PlayerPrefs.HasKey(name)) return null; string[] xyz = PlayerPrefs.GetString(name).Split(','); return new Vector3(float.Parse(xyz[0]), float.Parse(xyz[1]), float.Parse(xyz[2]));   }        },
        };

    delegate void SetValue(string name, object value);
    private static Dictionary<Type, SetValue> setValue = new Dictionary<Type, SetValue>()
        {
            { typeof(int),          delegate(string name, object value) { PlayerPrefs.SetInt(name, (int)value);             }        },
            { typeof(float),        delegate(string name, object value) { PlayerPrefs.SetFloat(name, (float)value);         }        },
            { typeof(bool),         delegate(string name, object value) { PlayerPrefs.SetInt(name, ((bool)value) ? 1 : 0);  }        },
            { typeof(string),       delegate(string name, object value) { PlayerPrefs.SetString(name, (string)value);       }        },
            { typeof(Vector2),      delegate(string name, object value) { Vector3 xy = (Vector2)value; PlayerPrefs.SetString(name, xy[0] + "," + xy[1]);                                }        },
            { typeof(Vector3),      delegate(string name, object value) { Vector3 xyz = (Vector3)value; PlayerPrefs.SetString(name, xyz[0] + "," + xyz[1] + "," + xyz[2] + ",");        }        },
        };

    /// <summary>
    /// Get object from PlayerPrefs
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name">name param</param>
    /// <returns>object</returns>
    public static T LoadPrefs<T>(string name)
    {
        return (T)LoadPrefs(name, typeof(T));
    }

    /// <summary>
    /// Get object from PlayerPrefs
    /// </summary>
    /// <param name="name">name get param</param>
    /// <param name="type">type</param>
    /// <returns>object</returns>
    public static object LoadPrefs(string name, Type type)
    {
        //CL.Log();
        if (type.IsClass)
        {
            if (type.IsArray)
            {
                Type itemType = type.GetElementType();
                IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));

                object item;
                for (int i = 0; i < 100; i++) // max 100 item
                {
                    item = LoadPrefs(name + "_" + i, itemType);
                    if (item == null) break;
                    list.Add(item);
                }

                int listLength = list.Count;
                Array resultI = Array.CreateInstance(itemType, listLength);

                for (int i = 0; i < listLength; i++)
                {
                    resultI.SetValue(list[i], i);
                }
                return resultI;
            }

            object result = Activator.CreateInstance(type);

            if (typeof(IList).IsAssignableFrom(type))
            {
                IList resultI = result as IList;
                object item;
                Type itemType = type.GetGenericArguments()[0];

                for (int i = 0; i < 100; i++) // max 100 item
                {
                    item = LoadPrefs(name + "_" + i, itemType);
                    if (item == null) return resultI;
                    //if (item.ToString() == "-1") return resultI;
                    resultI.Add(item);
                }
                return resultI;
            }

            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                IDictionary resultI = result as IDictionary;
                object item;
                object itemKey;
                //Type itemKeyType = type.GetGenericArguments()[0];
                Type itemType = type.GetGenericArguments()[1];

                for (int i = 0; i < 100; i++) // max 100 item
                {
                    itemKey = LoadPrefs(name + "_key" + i, itemType);
                    if (itemKey == null) return resultI;
                    item = LoadPrefs(name + "_" + i, itemType);
                    if (item == null) return resultI;
                    resultI.Add(itemKey, item);
                }
                return result;
            }

            object property;
            foreach (FieldInfo fieldInfo in type.GetFields())
            {
                property = LoadPrefs(name + "_" + fieldInfo.Name, fieldInfo.FieldType);
                if (property == null) return null;
                fieldInfo.SetValue(result, LoadPrefs(name + "_" + fieldInfo.Name, fieldInfo.FieldType));
            }
            return result;
        }

        if (type.IsEnum) return Enum.ToObject(type, PlayerPrefs.GetInt(name));

        if (getValue.ContainsKey(type)) return getValue[type](name);

        Debug.Log("not getter value");
        return null;
    }

    /// <summary>
    /// Set object in PlayerPrefs
    /// </summary>
    /// <param name="name">name set param</param>
    /// <param name="object_">set object</param>
    public static void SetPrefs(string name, object object_)
    {
        //CL.Log();
        Type type = object_.GetType();
        if (type.IsClass)
        {
            if (type.IsArray)
            {
                Array objectI = object_ as Array;
                int length = objectI.Length;

                for (int i = 0; i < length; i++)
                {
                    SetPrefs(name + "_" + i, objectI.GetValue(i));
                }
                return;
            }

            if (typeof(IList).IsAssignableFrom(type))
            {
                IList objectI = object_ as IList;
                for (int i = 0; i < objectI.Count; i++)
                {
                    SetPrefs(name + "_" + i, objectI[i]);
                }
                return;
            }

            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                IDictionary objectI = object_ as IDictionary;
                //Type itemKeyType = type.GetGenericArguments()[0];
                //Type itemType = type.GetGenericArguments()[1];
                //int count = objectI.Count;

                int id = 0;
                foreach (KeyValuePair<object, object> item in objectI)
                {
                    SetPrefs(name + "_key" + id, item.Key);
                    SetPrefs(name + "_" + id, item.Value);
                    id++;
                }
                return;
            }

            foreach (FieldInfo fieldInfo in type.GetFields())
            {
                SetPrefs(name + "_" + fieldInfo.Name, fieldInfo.GetValue(object_));
            }
            return;
        }

        if (type.IsEnum)
        {
            PlayerPrefs.SetInt(name, (int)object_);
            return;
        }

        if (setValue.ContainsKey(type))
        {
            setValue[type](name, object_);
            return;
        }

        Debug.Log("not setter value " + type);
    }
    */

    public static int ToInt(this BitArray binary)
    {
        int result = 0;
        int length = binary.Length;
        for (int i = 0; i < length; i++)
        {
            if (binary[i]) result = result | (1 << i);
        }
        return result;
    }
}
