using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum TrackName
{
    Common,
    Treck1,
    Treck2,
    Treck3,
    Treck4,
    Treck5,
    Treck6,
    Treck7,
    Treck8,
    Treck9,
}

public class EdgeColorsPrefab : MonoBehaviour {

    [System.Serializable]
    public class ColorDictionary : IEnumerable
    {
        public List<TrackName> keys = new List<TrackName>();
        public List<Color> values = new List<Color>();
        public Color this[TrackName key]
        {
            get
            {
                int index = keys.IndexOf(key);
                if (index == -1)
                {
                    return this[TrackName.Common];
                }
                else
                {
                    return values[index];
                }
            }
            set
            {
                int index = keys.IndexOf(key);
                if (index != -1 && values[index] != value)
                {
                    values[index] = value;
                    isChange = true;
                }
            }
        }

        public bool ContainsKey(TrackName key)
        {
            return keys.Contains(key);
        }

        public void Add(TrackName key, Color value)
        {
            if (keys.Contains(key)) return;
            keys.Add(key);
            values.Add(value);
            isChange = true;
        }

        public void Remove(TrackName key)
        {
            if (keys.Contains(key))
            {
                values.RemoveAt(keys.IndexOf(key));
                keys.Remove(key);
                isChange = true;
            }
        }

        public void RemoveAt(int index)
        {
            values.RemoveAt(index);
            keys.RemoveAt(index);
            isChange = true;
        }

        public int Count
        {
            get
            {
                return values.Count;
            }
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return values.GetEnumerator();
        }

        public KeyValuePair<TrackName, Color> this[int index]
        {
            get
            {
                return new KeyValuePair<TrackName, Color>(keys[index], values[index]); // values[index];
            }
            /*set
            {
                int index = keys.IndexOf(key);
                if (index != -1 && values[index] != value) values[index] = value;
            }*/
        }

        /*IEnumerable<KeyValuePair<FeatureType, Color>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<FeatureType, UnityEngine.Color>>.GetEnumerator()
        {
            return keys.GetEnumerator();
        }*/
    }

    public ColorDictionary color;
    public static bool isChange = false;

    /*public void PrefabApply()
    {
        if (!isChange) return;
        isChange = false;
        //Debug.Log(this.GetType() + ": EditorUtility.SetDirty(this)");
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }*/
}
