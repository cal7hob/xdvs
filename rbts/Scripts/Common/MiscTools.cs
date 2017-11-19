using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.SceneManagement;
using XDevs.LiteralKeys;
using Random = System.Random;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public static class MiscTools
{
    public static double unixOrigin;
    public static Random random;

    static MiscTools()
    {
        unixOrigin = ConvertToUnixTimestamp(new DateTime(1971, 1, 1, 0, 0, 0, 0));
        random = new Random();
    }

    //***********
    public class OrderedLinkedList<T> : IEnumerable<T> where T : IComparable<T>
    {
        private LinkedList<T> list = new LinkedList<T>();
        public void Add(T val)
        {
            if (list.Count == 0)
            {
                list.AddFirst(val);
                return;
            }

            LinkedListNode<T> node = list.First;

            do
            {
                if (node.Value.CompareTo(val) > 0)
                {
                    list.AddBefore(node, val);
                    return;
                }
                node = node.Next;
            }
            while (node != null);
            list.AddLast(val);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (T val in list)
            {
                yield return val;
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            foreach (T val in list)
            {
                yield return val;
            }
        }

        public bool Contains(T val)
        {
            return list.Contains(val);
        }

        public void Clear()
        {
            list.Clear();
        }
    }
    //***********

    public static bool Try(float probability)
    {
        return random.NextDouble() <= probability;
    }

    public static int Round(int value, int round, bool alwaysDown = false)
    {
        if (alwaysDown)
            value -= round / 2;
        int frac = value % round;

        return (frac <= round / 2) ?
            value - frac : value - frac + round;
    }

    public static int GetRandomIndex(params int[] _probabilities)
    {
        int[] probabilities = new int[_probabilities.Length];

        _probabilities.CopyTo(probabilities, 0);

        int sum = 0;

        for (int i = 0; i < probabilities.Length; i++)
        {
            sum += probabilities[i];
            probabilities[i] = sum;
        }

        if (sum == 0)
            return probabilities.GetLowerBound(0);

        int rnd = random.Next(1, sum);

        for (int i = 0; i < probabilities.Length; i++)
            if (rnd <= probabilities[i])
                return i;

        return probabilities.GetUpperBound(0);
    }

    public static T[] GetRandomFromSeveral<T>(int[] _probabilities, T[] _values, int count = 1)
    {
        int sum = 0;

        int[] probabilities = new int[_probabilities.Length];

        _probabilities.CopyTo(probabilities, 0);

        T[] values = new T[_values.Length];

        _values.CopyTo(values, 0);

        for (int i = 0; i < probabilities.Length; i++)
        {
            sum += probabilities[i];
            probabilities[i] = sum;
        }

        T[] result = new T[count];

        for (int counter = 0; counter < count; counter++)
        {
            int rnd = random.Next(0, sum);

            int i;

            for (i = 0; i < probabilities.Length; i++)
            {
                if (rnd >= probabilities[i])
                    continue;

                result[counter] = values[i];

                break;
            }

            if (i == probabilities.Length)
            {
                int maxProbability = 0;

                for (int j = 0; j < _probabilities.Length; j++)
                    if (_probabilities[j] > maxProbability)
                        maxProbability = _probabilities[j];

                for (int j = 0; j < _probabilities.Length; j++)
                    if (_probabilities[j] == maxProbability)
                        result[counter] = values[j];
            }
        }

        return result;
    }

    public static void SetObjectsActivity(IEnumerable<GameObject> objects, bool active)
    {
        if (objects == null)
            return;
        foreach (var go in objects)
        {
            if (go != null && go.activeSelf != active)
                go.SetActive(active);
        }
    }

    public static void SetObjectsActivity(IEnumerable<MonoBehaviour> objects, bool active)
    {
        if (objects == null)
            return;
        foreach (var behaviour in objects)
        {
            if (behaviour != null && behaviour.gameObject.activeSelf != active)
                behaviour.gameObject.SetActive(active);
        }
    }

    public static void SetObjectsActivity(bool active, params GameObject[] objects)
    {
        SetObjectsActivity(objects, active);
    }

    public static void SetObjectsActivityByComponents(IEnumerable<Component> components, bool active)
    {
        foreach (var component in components)
        {
            if (component.gameObject.activeSelf != active)
                component.gameObject.SetActive(active);
        }
    }


    public static DateTime TimestampToDate(double timestamp, bool toUTC = false)
    {
        var dtDateTime = new DateTime(1971, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dtDateTime = dtDateTime.AddSeconds(timestamp);
        return toUTC ? dtDateTime : dtDateTime.ToLocalTime();
    }

    public static DateTime ConvertFromUnixTimestamp(double timestamp)
    {
        DateTime origin = new DateTime(1971, 1, 1, 0, 0, 0, 0);
        return origin.AddSeconds(timestamp);
    }

    public static double ConvertToUnixTimestamp(DateTime date)
    {
        DateTime origin = new DateTime(1971, 1, 1, 0, 0, 0, 0);
        TimeSpan diff = date.ToUniversalTime() - origin;
        return Math.Floor(diff.TotalSeconds);
    }

    public static string CheckIfNull(object obj, string name)
    {
        return string.Format("{0} {1}", name, obj == null ? "is NULL" : "is NOT NULL");
    }

    public static bool isErrorImage(Texture tex)
    {
        //The "?" image that Unity returns for an invalid www.texture has these consistent properties:
        //(we also reject null.)
        return (tex && tex.name == "" && tex.height == 8 && tex.width == 8 && tex.filterMode == FilterMode.Bilinear && tex.anisoLevel == 1 && tex.wrapMode == TextureWrapMode.Repeat && tex.mipMapBias == 0);
    }

    public static Texture2D GetScreenshot()
    {
        Texture2D texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        texture.Apply();
        return texture;
    }

    public static void SetScreenAutoOrientation(bool activate)
    {
        Screen.autorotateToLandscapeLeft = activate;
        Screen.autorotateToLandscapeRight = activate;
    }

    public static string GetGameObjectPath(GameObject obj)
    {
        StringBuilder path = new StringBuilder(64);
        path.Append("/").Append(obj.name);
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path.Append("/").Append(obj.name).Append(path);
        }
        return path.ToString();
    }

    #region fading and blinking routines
    public static IEnumerator FadingRoutine(tk2dTextMesh textMesh, float interval, float minValue)
    {
        var initialTime = Time.time;
        var color = textMesh.color;

        while (true)
        {
            color.a = GetFadingAlpha(interval, minValue, initialTime);
            textMesh.color = color;
            yield return null;
        }
    }

    /// <summary>
    /// Плавно меняет прозрачность спрайта от минимального до 1, за заданный интервал
    /// </summary>
    /// <param name="spr">сам спрайт</param>
    /// <param name="interval">период, сек</param>
    /// <param name="minValue">минимум прозрачности</param>
    /// <returns></returns>
    public static IEnumerator FadingRoutine(tk2dBaseSprite spr, float interval, float minValue)
    {
        var initialTime = Time.time;
        var color = spr.color;

        while (true)
        {
            color.a = GetFadingAlpha(interval, minValue, initialTime);
            spr.color = color;
            yield return null;
        }
    }

    public static IEnumerator BlinkingRoutine(tk2dTextMesh textMesh, float interval)
    {
        var color = textMesh.color;
        WaitForSeconds wait = new WaitForSeconds(interval);
        while (true)
        {
            color.a = 0;
            textMesh.color = color;
            yield return wait;
            color.a = 1;
            textMesh.color = color;
            yield return wait;
        }
    }

    public static IEnumerator BlinkingRoutine(tk2dBaseSprite spr, float interval)
    {
        var color = spr.color;
        WaitForSeconds wait = new WaitForSeconds(interval);

        while (true)
        {
            color.a = 0;
            spr.color = color;
            yield return wait;
            color.a = 1;
            spr.color = color;
            yield return wait;
        }
    }

    public static IEnumerator BlinkingRoutine(GameObject obj, float interval)
    {
        while (true)
        {
            obj.SetActive(false);
            yield return new WaitForSeconds(interval);
            obj.SetActive(true);
            yield return new WaitForSeconds(interval);
        }
    }

    public static void SetSpritesAlpha(IEnumerable<tk2dBaseSprite> sprites, float alpha)
    {
        foreach (var sprite in sprites)
        {
            var color = sprite.color;
            color.a = alpha;
            sprite.color = color;
        }
    }

    /// <summary>
    /// Получение текущего значения прозрачности от ф-ции,с заданным интервалом изменения от заданного минимального значения до 1
    /// </summary>
    /// <param name="minValue">должно быть от 0 до 1</param>
    /// <returns></returns>
    private static float GetFadingAlpha(float interval, float minValue, float initialTime)
    {
        return 0.5f * Mathf.Cos((Mathf.PI * ((Time.time / interval) - initialTime)) / (Mathf.PI * 0.5f) + 1) * (1 - minValue) + (1 - (1 - minValue) * 0.5f);
    }
    #endregion

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    /// <summary>
    /// Full transform name (with path)
    /// </summary>
    public static string GetFullTransformName(Transform tr)
    {
        if (tr == null)
            return "(null)";

        string trName = tr.name;
        Transform trParent = tr.parent;
        while (trParent != null)
        {
            trName = trParent.name + "/" + trName;
            trParent = trParent.parent;
        }
        return trName;
    }


    public static Transform GetTopParentTransform(Transform tr)
    {
        if (tr == null)
            return null;
        while (tr.parent != null)
            tr = tr.parent;
        return tr;
    }

    public static string Base64Encode(string plainText)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }

    public static void CheckBtnVipState(GameObject[] objectsActivatedIfVip, GameObject[] objectsDisactivatedIfVip)
    {
        if (objectsActivatedIfVip != null)
            foreach (var obj in objectsActivatedIfVip.Where(obj => obj != null))
                obj.SetActive(ProfileInfo.IsPlayerVip);
        if (objectsDisactivatedIfVip != null)
            foreach (GameObject obj in objectsDisactivatedIfVip.Where(obj => obj != null))
                obj.SetActive(!ProfileInfo.IsPlayerVip);
    }

    public static string GetCultureSpecificFormatOfNumber(int number)
    {
        return number.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
    }

    public static void ChangeLayersRecursively(Transform trans, string name)
    {
        trans.gameObject.layer = LayerMask.NameToLayer(name);
        foreach (Transform child in trans)
        {
            ChangeLayersRecursively(child.transform, name);
        }
    }

    public static int FindFirstInString(string str, Predicate<char> predicate, int index = 0)
    {
        for (int i = index; i < str.Length; i++)
        {
            if (predicate(str[i]))
                return i;
        }

        return -1;
    }

    public static string StringFromPhotonBytes(this byte[] bytes, ref int index)
    {
        int length = 0;
        Protocol.Deserialize(out length, bytes, ref index);
        byte[] stringBytes = new byte[length];
        Array.Copy(bytes, index, stringBytes, 0, length);
        index += length;
        return Encoding.UTF8.GetString(stringBytes, 0, stringBytes.Length);
    }

    #region Layer functions

    public static bool CheckIfLayerInMask(int mask, Layer.Key layerKey)
    {
        return mask == (mask | (1 << LayerMask.NameToLayer(Layer.Items[layerKey])));
    }

    public static bool CheckIfLayerInMask(int mask, int layer)
    {
        return mask == (mask | (1 << layer));
    }

    public static int GetLayerMask(params string[] layerNames)
    {
        int mask = 0;

        foreach (string layerName in layerNames)
            mask |= 1 << LayerMask.NameToLayer(layerName);

        return mask;
    }

    public static int GetLayerMask(params Layer.Key[] layerKeys)
    {
        int mask = 0;

        foreach (Layer.Key layerKey in layerKeys)
            mask |= 1 << LayerMask.NameToLayer(Layer.Items[layerKey]);

        return mask;
    }

    public static int ExcludeLayersFromMask(int layerMask, params string[] layersToExclude)
    {
        int excludeMask = GetLayerMask(layersToExclude);
        return layerMask & ~excludeMask;
    }

    public static int ExcludeLayersFromMask(int layerMask, params Layer.Key[] layersToExclude)
    {
        int excludeMask = GetLayerMask(layersToExclude);
        return layerMask & ~excludeMask;
    }

    public static int ExcludeLayerFromMask(int layerMask, int layer)
    {
        return layerMask & ~(1 << layer);
    }

    public static void ShowMaskLayers(string maskName, int mask)
    {
        StringBuilder str = new StringBuilder(128);
        bool firstRec = true;
        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                if (!firstRec)
                    str.Append(", ");
                str.AppendFormat("{0}({1})", LayerMask.LayerToName(i), i);
                firstRec = false;
            }
        }
        Debug.LogFormat("Mask '{0}': {1}", maskName, str);
    }
    #endregion

    /// <summary>
    /// Поиск по имени одного из объектов во вложенной иерархии
    /// </summary>
    public static Transform FindInHierarchy(this Transform transform, string name)
    {
        Transform t1 = transform.Find(name);
        if (t1 != null)
            return t1;

        foreach (Transform t in transform)
        {
            t1 = t.FindInHierarchy(name);
            if (t1 != null)
                return t1;
        }
        return null;
    }

    public static Bounds SumBounds(Bounds first, Bounds second)
    {
        Vector3 min = new Vector3(Mathf.Min(first.min.x, second.min.x), Mathf.Min(first.min.y, second.min.y),
            Mathf.Min(first.min.z, second.min.z));
        Vector3 max = new Vector3(Mathf.Max(first.max.x, second.max.x), Mathf.Max(first.max.y, second.max.y),
            Mathf.Max(first.max.z, second.max.z));
        Vector3 center = 0.5f * (min + max);
        Vector3 size = max - min;
        return new Bounds(center, size);
    }

    public static Vector3 EulerBetween(Vector3 current, Vector3 target)
    {
        return new Vector3(
            Mathf.DeltaAngle(current.x, target.x),
            Mathf.DeltaAngle(current.y, target.y),
            Mathf.DeltaAngle(current.z, target.z)
            );
    }



    #region Physics
    public static void ScaleLocalRotation(Rigidbody rigidBody, Vector3 axes)
    {
        Transform transform = rigidBody.transform;
        Vector3 angularVelocity = transform.InverseTransformDirection(rigidBody.angularVelocity);
        angularVelocity.Scale(axes);
        rigidBody.angularVelocity = transform.TransformDirection(angularVelocity);
    }
    #endregion

#if UNITY_EDITOR
    public static void MakeRealDirty(Object obj)
    {
        EditorUtility.SetDirty(obj);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
#endif
}
