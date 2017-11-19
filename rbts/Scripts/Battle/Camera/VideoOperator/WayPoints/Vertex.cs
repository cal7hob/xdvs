using UnityEngine;
using System.Collections.Generic;

public class Vertex : MonoBehaviour
{
    //public static int               lastIdForEditor;
    //[SerializeField]
    //public int                      idForEditor;
    public List<Vertex>           waysVertex = new List<Vertex>();
    public List<TrackName>      waysVertexColors = new List<TrackName>();
    public TrackFeatureTypeDict   track = new TrackFeatureTypeDict();

    public int                      id;

    public Vector3 position
    {
        get
        {
            return transform.position;
        }
    }

    public void Awake()
    {
        //calculatePosition = false;
    }

    /*private void OnDrawGizmosSelected()
    {
        Color color =  Color.white;
        for (int i = 0; i < track_.Count; i++)
        {
            int res = i % 5;
            switch (res)
            {
                case 0:
                    color = Color.yellow;
                    break;
                case 1:
                    color = Color.white;
                    break;
                case 2:
                    color = Color.green;
                    break;
                case 3:
                    color = Color.magenta;
                    break;
                case 4:
                    color = Color.red;
                    break;

            }
            
            for(int j = 1; j < track_[i].track.Count; j++)
            {
                Debug.DrawLine(track_[i].track[j - 1].GetComponent<Transform>().position + Vector3.up * i, track_[i].track[j].GetComponent<Transform>().position + Vector3.up * i, color);
            }
        }
    }*/
}

[System.Serializable]
public class Track
{
    public Track(List<Vertex> track, float distance)
    {
        this.track = track;
        this.distance = distance;
    }

    public List<Vertex> track = new List<Vertex>();
    public float distance;
}

[System.Serializable]
public class TrackDict : AEDictionary<Vertex, Track> { }

[System.Serializable]
public class TrackFeatureTypeDict : AEDictionary<TrackName, TrackDict> { }
