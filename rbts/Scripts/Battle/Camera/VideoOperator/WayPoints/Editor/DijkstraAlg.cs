using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

//[Serializable]
public class DijkstraAlg
{
    public TrackData[]                      vertexTracksData = null;
    private static Vertex[]               vertices = null;
    private static List<DistanceVertex>[]   waysDistanceVertex = null;
    private bool                            onReInit = false;

    private float                           minDistanceVertex = 20;
    public List<Vertex>                   track = null;
    public bool                             noWays = true;
    public bool                             noHelicWays = true;
    public TrackName vehicleType;

    public DijkstraAlg(TrackName type, float minDistanceVertex)
    {
        if (WayPoints.instance == null)
        {
            return;
        }

        if (waysDistanceVertex == null)
        { 
            Init(); 
        }
        track = new List<Vertex>();
        int length = vertices.Length;
        vertexTracksData = new TrackData[length]; //create object calculate track
        this.minDistanceVertex = minDistanceVertex;
        
        if (vertices.Length == 0 || vertices[0].track.ContainsKey(vehicleType))
        {
            vehicleType = type;
        }
        else
        {
            vehicleType = TrackName.Common;
        }

        TrackData td;
        Vertex select;

        for (int i = 0; i < length; i++)
        {
            select = vertices[i];
            td = new TrackData(select);
            td.waysDistanceVertex = new List<DistanceVertex>();
            
            foreach (DistanceVertex distanceVertex in waysDistanceVertex[i])
            {
                if (distanceVertex.type == TrackName.Common || distanceVertex.type == type)
                {
                    td.waysDistanceVertex.Add(distanceVertex);
                    noWays = false;
                }
            }
            
            vertexTracksData[i] = td;
        }
    }

    public DijkstraAlg()
    {
        if (WayPoints.instance == null)
        {
            return;
        }

        if (waysDistanceVertex == null)
        {
            Init();
        }

        int length = vertices.Length;
        vertexTracksData = new TrackData[length]; //create object calculate track

        TrackData td;
        Vertex select;

        for (int i = 0; i < length; i++)
        {
            select = vertices[i];
            td = new TrackData(select);
            td.waysDistanceVertex = new List<DistanceVertex>();

            foreach (DistanceVertex distanceVertex in waysDistanceVertex[i])
            {
                td.waysDistanceVertex.Add(distanceVertex);
            }
            vertexTracksData[i] = td;
        }
        Debug.Log("waysDistanceVertex.Length = " + vertexTracksData.Length);
    }

    public class TrackData
    {
        public TrackData(Vertex vertex)
        {
            this.vertex = vertex;
        }

        public Vertex             vertex = null;
        public float                trackDistance = float.MaxValue;
        public bool                 attended = false;
        public List<Vertex>       trackVertex = new List<Vertex>();
        public List<DistanceVertex> waysDistanceVertex;
    }

    public class DistanceVertex
    {
        public DistanceVertex(Vertex select, Vertex target, TrackName type)
        {
            vertex = target;
            this.type = type;
            distance = Vector3.Distance(select.position, vertex.position);
        }

        public float            distance = 0;
        public Vertex         vertex = null;
        public TrackName    type = TrackName.Common;
    }

    public static void Init() //global measurement distance
    {
        vertices = WayPoints.instance.vertices;
        int length = vertices.Length;
        waysDistanceVertex = new List<DistanceVertex>[length];

        List<DistanceVertex> distanceVertices;
        Vertex select;

        bool warningVertex = false;

        for (int i = 0; i < length; i++)
        {
            select = vertices[i];
            distanceVertices = new List<DistanceVertex>();
            Vertex vertex;
            int lengthWaysVertex = select.waysVertex.Count;
            //foreach (Vertex vertex in select.waysVertex)
            for(int v = 0; v<lengthWaysVertex; v++)
            {
                vertex = select.waysVertex[v];
                if (vertex == null)
                {
                    Debug.Log("Check " + select.id + " way in vertex", select.gameObject);
                    if (!warningVertex) warningVertex = true;
                }
                else
                {
                    distanceVertices.Add(new DistanceVertex(select, vertex, select.waysVertexColors[v]));
                }
            }

            if (warningVertex)
            {
                int s = 0;
                while (s < select.waysVertex.Count)
                {
                    if (select.waysVertex[s] == null)
                    {
                        select.waysVertex.RemoveAt(s);
                        select.waysVertexColors.RemoveAt(s);
                    }
                    else
                    {
                        s++;
                    }
                }
                warningVertex = false;
            }

            distanceVertices.Sort(delegate(DistanceVertex us1, DistanceVertex us2) { return us1.distance.CompareTo(us2.distance); });

            waysDistanceVertex[i] = distanceVertices;
        }
    }

    public void ReInit()
    {
        foreach (TrackData trackData in vertexTracksData)
        {
            trackData.attended = false;
            trackData.trackDistance = float.MaxValue;
            trackData.trackVertex.Clear();
        }
        track = null;
    }

    public Vector3 NextTrack(Vector3 thisPoint, Vector3 targetDefault, ref float distanceTrack)
    {
        distanceTrack = distanceTrackOld;
        if (track == null || track.Count == 0) return targetDefault;
        return NextTrack(thisPoint, targetDefault);
    }

    private Vector3 NextTrack(Vector3 thisPoint, Vector3 targetDefault)
    {
        if (track.Count > 2)
        {
            Vertex vertex = track[0];
            float distance;
            for (int i = 0; i < track.Count; i++)
            {
                vertex = track[0];
                distance = Vector3.Distance(thisPoint, vertex.position);

                /*Vector3 derection = (track[1].position - vertex.position).normalized;
                if (Vector3.Angle(derection, (thisPoint - vertex.position).normalized) < 90)
                {
                    track.Remove(vertex);
                    //Debug.Log(this.GetType() + ": Remove " + Vector3.Angle(derection, (thisPoint - vertex.transform.position).normalized) + " " + derection + " " + (1 - (distance / minDistanceVertex)) + " " + distance);
                    continue;
                }*/

                if (distance < minDistanceVertex)
                {
                    track.Remove(vertex);
                    continue;

                    /*if (distance < minDistanceVertex/5)
                    {
                        track.Remove(vertex);
                        continue;
                    }

                    if (track.Count > 1)
                    {
                        //Debug.Log(this.GetType() + ": Angle " + Vector3.Angle(derection, (thisPoint - vertex.transform.position).normalized) + " " + derection + " " + (1 - (distance / minDistanceVertex)) + " " + distance);
                        return vertex.transform.position + (derection * ((1 - (distance / minDistanceVertex)) * Vector3.Distance(track[1].transform.position, vertex.transform.position)));
                    }
                    else
                    {
                        return vertex.transform.position;
                    }*/
                }
                else
                {
                    return vertex.position;
                }

                /*if (distance > minDistanceVertex)
                {
                    if (track.Count > 1)
                    {
                        //Debug.Log(this.GetType() + ": angle" + Vector3.Angle((track[1].transform.position - vertex.transform.position).normalized, (thisPoint - vertex.transform.position).normalized));
                        if (Vector3.Angle(derection, (thisPoint - vertex.transform.position).normalized) < 90) track.Remove(vertex);
                        //if (distance > Vector3.Distance(thisPoint, track[1].transform.position)) track.Remove(vertex);
                    }
                    else
                    {
                        return vertex.transform.position;
                    }
                }
                else
                {
                    track.Remove(vertex);
                }*/
            }
            return vertex.position;
        }
        if (track.Count > 0)
        {
            return track[0].position;
        }
        return targetDefault;
    }

    private Vector3 NextTrack_(Vector3 thisPoint, Vector3 targetDefault)
    {
        if (track.Count > 2)
        {
            Vertex vertex;// = track[0];
            float distance;
            int index = 0;
            for (int i = 0; i < track.Count; i++)
            {
                vertex = track[i];
                distance = Vector3.Distance(thisPoint, vertex.position);

                /*Vector3 derection = (track[1].position - vertex.position).normalized;
                if (Vector3.Angle(derection, (thisPoint - vertex.position).normalized) < 90)
                {
                    //track.Remove(vertex);
                    index++;
                    //Debug.Log(this.GetType() + ": Remove " + Vector3.Angle(derection, (thisPoint - vertex.transform.position).normalized) + " " + derection + " " + (1 - (distance / minDistanceVertex)) + " " + distance);
                    continue;
                }*/

                if (distance < minDistanceVertex)
                {
                    //track.Remove(vertex);
                    index++;
                    continue;
                }
                else
                {
                    return track[index].position;//vertex.position;
                }
            }
            if (track.Count > index) return track[index].position; //vertex.position;
        }
        if (track.Count > 0)
        {
            return track[0].position;
        }
        return targetDefault;
    }

    private float distanceTrackOld;
    private float dijkstraAlgTimer = 0;

    public Vector3 GetTrack(Vector3 start, Vector3 target, TrackName vehicleType, ref float distanceTrack)
    {
        if (WayPoints.instance.calcEditor)
        {
            Vertex startVertex_ = WayPoints.GetNear(start);
            Vertex endVertex_ = WayPoints.GetNear(target);
            
            if (startVertex_ == endVertex_ || Vector3.Distance(startVertex_.position, target) < 30 || Vector3.Distance(start, target) < 30) //startVtrtexToTarget > 30
            {
                distanceTrack = Vector3.Distance(start, target);
                return target;
            }
            else
            {
                TrackDict trackDict = startVertex_.track[vehicleType] ?? startVertex_.track[TrackName.Common];
                if (trackDict == null)
                {
                    startVertex_.track.Add(vehicleType, trackDict = new TrackDict());
                }
                Track track = trackDict[endVertex_];
                if (track == null)
                {
                    GetTrack(startVertex_, endVertex_, target, ref distanceTrack);
                    track = new Track(new List<Vertex>(this.track), distanceTrack);
                    trackDict.Add(endVertex_, track);
                }
                else
                {
                    distanceTrack = track.distance;
                    this.track = track.track;
                }
                return NextTrack_(start, target);

                /*if (track == null)
                {
                    GetTrack(startVertex_, endVertex_, target, ref distanceTrack);
                    return NextTrack(start, target);
                }
                else
                {
                    distanceTrack = track.distance;
                }
                this.track = track.track;
                return NextTrack_(start, target);*/

                /*distanceTrack = track.distance;
                this.track = track.track;
                return NextTrack_(start, target);*/
            }
        }
        else
        {
            if (WayPoints.instance != null && WayPoints.instance.vertices.Length > 1)
            {

                dijkstraAlgTimer -= Time.deltaTime;
                if (dijkstraAlgTimer <= 0)
                {
                    switch (vehicleType)
                    {
                        case TrackName.Common:

                            dijkstraAlgTimer = 1;
                            break;
                        default:
                            dijkstraAlgTimer = 0.03f;
                            break;
                    }
                    Vertex startVertex = WayPoints.GetNear(start);
                    Vertex endVertex = WayPoints.GetNear(target);

                    if (startVertex == endVertex || Vector3.Distance(startVertex.position, target) < 30 || Vector3.Distance(start, target) < 30) //startVtrtexToTarget > 30
                    {
                        distanceTrack = Vector3.Distance(start, target);
                        return target;
                    }
                    else
                    {
                        GetTrack(startVertex, endVertex, target, ref distanceTrack);
                        distanceTrackOld = distanceTrack;
                        return NextTrack(start, target);
                    }
                }
                else
                {
                    return NextTrack(start, target, ref distanceTrack);
                }
            }
            else
            {
                distanceTrack = 100;
                return target;
            }
        }
    }

    public List<Vertex> GetTrack(Vector3 start, Vector3 target) //public List<Vertex> GetTrack(Vertex start, Vertex target, Vector3 targetDefault, ref float distanceTrack)
    {
        if (WayPoints.instance != null && WayPoints.instance.vertices.Length > 1)
        {
            Vertex startVertex = WayPoints.GetNear(start);
            Vertex endVertex = WayPoints.GetNear(target);
            if (startVertex != endVertex)
            {
                float distanceTrack = 0;
                if (WayPoints.instance.calcEditor)
                {
                    TrackDict trackDict = startVertex.track[vehicleType] ?? startVertex.track[TrackName.Common];
                    if (trackDict == null) startVertex.track.Add(vehicleType, trackDict = new TrackDict());
                    Track track = trackDict[endVertex];
                    /*if (track == null)
                    {
                        GetTrack(startVertex, endVertex, target, ref distanceTrack);
                        return this.track;
                    }
                    else
                    {
                        distanceTrack = track.distance;
                    }
                    return track.track;*/

                    if (track == null)
                    {
                        GetTrack(startVertex, endVertex, target, ref distanceTrack);
                        track = new Track(new List<Vertex>(this.track), distanceTrack);
                        trackDict.Add(endVertex, track);
                    }
                    else
                    {
                        distanceTrack = track.distance;
                        this.track = track.track;
                    }
                    return track.track;

                    /*distanceTrack = track.distance;
                    return track.track;*/
                }
                else
                {
                    GetTrack(startVertex, endVertex, target, ref distanceTrack);
                    //Debug.Log(this.GetType() + " count " + track.Count);
                    return track;
                }
            }
        }
        return new List<Vertex>();
    }

    private List<TrackData> stack = new List<TrackData>();

    public Vector3 GetTrack(Vertex start, Vertex target, Vector3 targetDefault, ref float distanceTrack)
    {
        if (start == null)
        {
            Debug.Log("!!!Warning DijkstraAlg.GetTrack(Vertex start == null...");
            return targetDefault;
        }
        if (target == null)
        {
            Debug.Log("!!!Warning DijkstraAlg.GetTrack(...Vertex target == null...");
            return targetDefault;
        }

        if (onReInit)
        {
            ReInit();
        }
        else
        {
            onReInit = true;
        }

        TrackData select = vertexTracksData[start.id];
        select.trackDistance = 0;
        select.trackVertex.Clear();

        stack.Clear();
        stack.Add(select);

        while (stack.Count > 0)
        {
            select = stack[0];
            stack.RemoveAt(0);
            float distance;
            TrackData trackData;

            foreach (DistanceVertex dv in select.waysDistanceVertex)
            {
                trackData = vertexTracksData[dv.vertex.id];
                if (!trackData.attended)
                {
                    distance = dv.distance + select.trackDistance;
                    if (distance < trackData.trackDistance)
                    {
                        trackData.trackDistance = distance;
                        trackData.trackVertex.Clear();
                        trackData.trackVertex.AddRange(select.trackVertex);

                        trackData.trackVertex.Add(dv.vertex);
                        if (trackData.vertex != target && !stack.Contains(trackData))
                        {
                            stack.Add(trackData);
                        }
                    }
                }
            }
            select.attended = true;
        }
        
        track = vertexTracksData[target.id].trackVertex;
        if (track.Count == 0)
        {
            track = GetTrackNear(target.position, ref distanceTrack);
            //CL.Log("distanceTrack1 " + distanceTrack);
        }
        else
        {
            distanceTrack = vertexTracksData[target.id].trackDistance;
            //CL.Log("distanceTrack2 " + vertexTracksData[target.id].trackDistance);
        }

        if (track.Count > 1)
        {
            return track[1].position;
        }
        return target.position;
    }

    private List<Vertex> GetTrackNear(Vector3 target, ref float distanceTrack)
    {
        List<Vertex> result = new List<Vertex>();
        float distanceMin = float.MaxValue;
        float distance;
        foreach (TrackData vertexTD in vertexTracksData)
        {
            if (vertexTD.trackVertex.Count > 0)
            {

                distance = Vector3.Distance(vertexTD.vertex.position, target);
                if (distance < distanceMin)
                {
                    distanceMin = distance;
                    result = vertexTD.trackVertex;
                    distanceTrack = vertexTD.trackDistance;
                }
            }
        }
        return result;
    }

    /*public void OnDrawGizmos()
    {
        if (track == null || track.Count == 0) return;
        Vector3 start = track[0].GetComponent<Transform>().position;
        Gizmos.color = Color.red;

        Vertex ind;
        for(int i = 1; i<track.Count; i++)
        {
            ind = track[i];
            if (ind == null)
            {
                return;
            }
            Gizmos.DrawLine(start, ind.GetComponent<Transform>().position);
            start = ind.GetComponent<Transform>().position;
        }
    }*/
}
