using UnityEngine;
using System.Collections.Generic;

//[System.Serializable]
//public class TrackData : AEDictionary<JNVertex, JNTrackFeatureTypeDict> { }

public class WayPoints : MonoBehaviour
{
    public static WayPoints   instance;
    public Vertex[]           vertices;
    public bool                 calcEditor = false;
    //public TrackData trackData = new TrackData();

    public void Awake()
    {
        vertices = GetComponentsInChildren<Vertex>();
        int id = 0;
        foreach (Vertex vertice in vertices)
        {
            vertice.id = id++;
            //if (vertice.mapTarget) mapTargets.Add(vertice);
        }

        instance = this;
        //DijkstraAlg.Init();
    }

    /// <summary>
    /// Получение ближайшей вершины
    /// </summary>
    /// <param name="pos">Цель</param>
    /// <returns>Вершина</returns>
    public static Vertex GetNear(Vector3 pos)
    {
        Vertex target = null;
        if (instance.vertices.Length > 0)
        {
            float distance = float.MaxValue;//100
            float newDist;

            for (int i = 0; i < instance.vertices.Length; i++)
            {
                newDist = Vector3.Distance(instance.vertices[i].position, pos);
                if (newDist < distance)
                {
                    target = instance.vertices[i];
                    distance = newDist;
                }
            }
        }
        return target;
        //return GetNear(pos, 1)[0];
    }

    /// <summary>
    /// Получение нескольких ближайших вершин
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public static List<Vertex> GetNear(Vector3 pos, int count)
    {
        List<Vertex> res = new List<Vertex>();
        if (instance.vertices.Length > 0)
        {
            for (int n = 0; n < count; n++)
            {
                float distance = float.MaxValue;//100
                Vertex target = null;

                for (int i = 0; i < instance.vertices.Length; i++)
                {
                    //if (instance.vertices[i].unit == null && !res.Contains(instance.vertices[i]) && instance.vertices[i].unitWant == null)
                    if (!res.Contains(instance.vertices[i]))
                    {
                        float newDist = Vector3.Distance(instance.vertices[i].position, pos);
                        if (newDist < distance)
                        {
                            target = instance.vertices[i];
                            distance = newDist;
                        }
                    }
                }
                if (target!=null && !res.Contains(target))
                {
                    res.Add(target);
                }
            }            
        }
        return res;
    }

    public static List<Vertex> GetNear(List <Vertex> vertices, Vector3 position, int count)
    {
        List<Vertex> res = new List<Vertex>();
        float newDist;
        float distance;
        Vertex target;
        if (vertices.Count > 0)
        {
            for (int n = 0; n < count; n++)
            {
                distance = float.MaxValue;
                target = null;

                for (int i = 0; i < vertices.Count; i++)
                {
                    //if (!res.Contains(vertices[i]) && vertices[i].unit == null && vertices[i].unitWant == null)
                    if (!res.Contains(vertices[i]))
                    {
                        newDist = Vector3.Distance(vertices[i].position, position);
                        if (newDist < distance)
                        {
                            target = vertices[i];
                            distance = newDist;
                        }
                    }
                }
                if (target != null && !res.Contains(target))
                {
                    res.Add(target);
                }
            }
        }        
        res.Sort(delegate(Vertex us1, Vertex us2) { return Vector3.Distance(us1.position, position).CompareTo(Vector3.Distance(us2.position, position)); });
        return res;
    }

    /*public static List<JNVertex> GetNearWithWay(JNVertex target, JNVertex ofVertex)
    {
        List<JNVertex> res = new List<JNVertex>();
        List<JNVertex> nearVertexs = GetNear(target.transform.position, 5);
        nearVertexs.Add(target);
        foreach (JNVertex vertex in nearVertexs)
        {
            if (ofVertex.IsWayFrom(vertex)) res.Add(vertex);
        }
        return res;
    }*/

    /*public static List<JNVertex> GetNearWithWay(JNVehicleController target, JNVertex ofVertex)
    {
        List<JNVertex> res = new List<JNVertex>();
        if (ofVertex == null)
        {
            Debug.Log("!!! Warning GetNearWithWay(LHUnit target, LHVertex ofVertex == null)");
            return res;
        }
        List<JNVertex> nearVertexs = GetNear(target.transform.position, 5);
        foreach (JNVertex vertex in nearVertexs)
        {
            if (ofVertex.IsWayFrom(vertex)) res.Add(vertex);
        }
        return res;
    }*/
}
