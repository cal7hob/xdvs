using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(VertexEditorScript))]
public class VertexEditor : Editor
{
    private enum State
    {
        NONE = -1,
        RMV_VERTEX = 0,
        SET_VERTEX = 1,
        SET_WAY = 2,
        RMV_WAY = 3,
        AGG_VERTEX = 4,
    }

    private class CollectionsFeatureType
    {
        public CollectionsFeatureType()
        {
            foreach (TrackName featureType in Enum.GetValues(typeof(TrackName))) if (colorList.ContainsKey(featureType)) this[featureType] = true;
        }

        private List<TrackName> values = new List<TrackName>();
        private bool _isModify = false;

        public bool this[TrackName key]
        {
            get
            {
                return values.Contains(key);
            }

            set
            {
                if (values.Contains(key) != value)
                {
                    _isModify = true;
                    if (value)
                    {
                        values.Add(key);
                    }
                    else
                    {
                        values.Remove(key);
                    }
                }
            }
        }

        public bool isModify
        {
            get
            {
                if (_isModify)
                {
                    _isModify = false;
                    return true;
                }

                return false;
            }
        }

        public void Update()
        {
            _isModify = true;
        }
    }

    class PaintVertex //VertexWays
    {
        public PaintVertex() { }
        public PaintVertex(Vertex vertex)
        {
            this.vertex = vertex;
        }

        public Vertex vertex;
        public Dictionary<Vertex, TrackName> waysVertex = new Dictionary<Vertex, TrackName>();
    }

    public  GameObject vertex;
    private static GameObject from;
    private static EdgeColorsPrefab prefab;
    private static EdgeColorsPrefab.ColorDictionary colorList;
    private static Color defaultColor = Color.blue;
    private static Vertex[] allVertices;
    public List<GameObject> selectedVertices = new List<GameObject>();
    public TrackName addVehicle = TrackName.Common;
    private static CollectionsFeatureType selectPaintVehicle; //= new CollectionsFeatureType();
    private Dictionary<Vertex, PaintVertex> paintVertices = new Dictionary<Vertex, PaintVertex>();

    private static State state = State.NONE;
    private static TrackName colorIndex = 0;

    private Vertex oldVertex;
    private bool isSetWay = false;

    public float heightOffset = 10f;

    public void Load()
    {
        CheckResources(ref prefab, "Prefabs/EdgeColorsPrefab.prefab");//Assets/.prefab
        colorList = prefab.color;//(prefab = AEEditorTools.LoadResources<EdgeColorsPrefab>("Prefabs/EdgeColorsPrefab")).color;
        selectPaintVehicle = new CollectionsFeatureType();
    }

    public void OnEnable()
    {
        if (colorList == null) Load();
        selectPaintVehicle.Update();
    }

    public override void OnInspectorGUI()
    {
        CheckResources(ref vertex, "Prefabs/VertexPoint.prefab");//Assets/.prefab
        vertex = (GameObject)EditorGUILayout.ObjectField("Vertex prefab", vertex, typeof(GameObject), false);
        heightOffset = EditorGUILayout.FloatField("Height Offset", heightOffset);

        EditorGUILayout.BeginHorizontal();
        /*if (GUILayout.Button("Generate IDs"))
        {
            allVertices = GameObject.FindObjectsOfType<Vertex>();
            if (allVertices != null && allVertices.Length > 0)
            {
                Vertex.lastIdForEditor = 0;
                foreach (Vertex vrt in allVertices)
                {
                    vrt.idForEditor = ++Vertex.lastIdForEditor;
                    EditorUtility.SetDirty(vrt);
                    Debug.Log(vrt.idForEditor);
                }
            }
            Debug.Log("Last ID for vertex is: " + Vertex.lastIdForEditor);
        }*/

        if (GUILayout.Button("Calc ways"))
        {
            CalcWays();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("Full calc ways");
        }

        if (GUILayout.Button("FastCW"))
        {
            FastCalcWays();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("Full calc ways");
        }

        EditorGUILayout.EndHorizontal();

        if (vertex == null)
        {
            EditorGUILayout.Space();
            ColorLabel("Vertex prefab is NULL", Color.red);
        }

        EditorGUILayout.Space();
        KeyValuePair<TrackName, Color> typeColorPair;
        for (int i = 0; i < colorList.Count; i++)
        {
            typeColorPair = colorList[i];
            EditorGUILayout.BeginHorizontal();
            selectPaintVehicle[typeColorPair.Key] = (GUILayout.Toggle(selectPaintVehicle[typeColorPair.Key], GUIContent.none) || typeColorPair.Key == colorIndex);
            if (ColorButton(typeColorPair.Key + "", typeColorPair.Key == colorIndex, typeColorPair.Value, GUILayout.Width(156)))
            {
                colorIndex = typeColorPair.Key;
                selectPaintVehicle[typeColorPair.Key] = true;
            }
            colorList[typeColorPair.Key] = EditorGUILayout.ColorField(typeColorPair.Value); //colorList.values[i]
            if (GUILayout.Button("-")) colorList.Remove(typeColorPair.Key); //.RemoveAt(i);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        addVehicle = (TrackName)EditorGUILayout.EnumPopup(addVehicle);
        if (GUILayout.Button("Add a path"))
        {
            colorList.Add(addVehicle, defaultColor);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (vertex != null)
        {
            //SetVertex
            if (ColorButton("Set Vertex", state == State.SET_VERTEX, Color.green, GUILayout.Height(20)))
            {
                from = null;
                state = state == State.SET_VERTEX ? State.NONE : State.SET_VERTEX;
            }

            //RemoveVertex
            if (ColorButton("Remove Vertex", state == State.RMV_VERTEX, Color.red, GUILayout.Height(20)))
            {
                from = null;
                state = state == State.RMV_VERTEX ? State.NONE : State.RMV_VERTEX;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            //Set or remove way
            if (state == State.SET_WAY || state == State.RMV_WAY || state == State.SET_VERTEX)
            {
                EditorGUILayout.BeginHorizontal();

                //SetWay
                if (ColorButton("Set Way", state == State.SET_WAY, Color.green, GUILayout.Height(20)))
                {
                    from = null;
                    state = state == State.SET_WAY ? State.SET_VERTEX : State.SET_WAY;
                }
                
                //RemoveWay
                if (ColorButton("Remove Way", state == State.RMV_WAY, Color.red, GUILayout.Height(20)))
                {
                    from = null;
                    state = state == State.RMV_WAY ? State.SET_VERTEX : State.RMV_WAY;
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        if (ColorButton("Select vertices", state == State.AGG_VERTEX, Color.green, GUILayout.Height(20)))
        {
            selectedVertices.Clear();
            state = state == State.AGG_VERTEX ? State.NONE : State.AGG_VERTEX;
        }

        if (selectedVertices.Count > 0) 
        {
            if (GUILayout.Button("Aggregate", GUILayout.Height(20)))
            {
                vertex = null;
                AggregateVertices();
                for (int i = 0; i < selectedVertices.Count; i++)
                {
                    Transform child = selectedVertices[i].transform.GetChild(0); //Debug.Log(child.name);
                    if (child != null)
                    {
                        child.gameObject.SetActive(false);
                    }
                }
                selectedVertices.Clear();
                state = State.NONE;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear null waysVertex", GUILayout.Height(20)))
        {
            allVertices = GameObject.FindObjectsOfType<Vertex>();
            if (allVertices != null && allVertices.Length > 0)
            {
                int selectIndex;
                bool set = false;
                foreach (Vertex vrt in allVertices)
                {
                    selectIndex = 0;
                    while(vrt.waysVertex.Count > selectIndex)
                    {
                        if (vrt.waysVertex[selectIndex] == null)
                        {
                            vrt.waysVertex.RemoveAt(selectIndex);
                            vrt.waysVertexColors.RemoveAt(selectIndex);
                            set = true;
                            Debug.Log("Remove waysVertex", vrt);
                        }
                        else
                        {
                            selectIndex++;
                        }
                    }
                    if (set)
                    {
                        EditorUtility.SetDirty(vrt);
                        set = false;
                    }
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        
        if (GUI.changed)
        {
            //Debug.Log(this.GetType() + ": OnInspectorGUI() GUI.changed");
            //prefab.PrefabApply();
            if (EdgeColorsPrefab.isChange)
            {
                EdgeColorsPrefab.isChange = false;
                //Debug.Log(this.GetType() + ": EditorUtility.SetDirty(this)");
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
            }
        }
    }
    
    void OnSceneGUI()
    {
        if (Application.isPlaying) return;

        if (selectPaintVehicle.isModify)
        {
            allVertices = GameObject.FindObjectsOfType<Vertex>();
            PaintVertex paintVertex = new PaintVertex();
            TrackName featureType;

            if (allVertices != null && allVertices.Length > 0) 
            {
                paintVertices.Clear();
                foreach (Vertex vrt in allVertices) 
                {
                    if (vrt == null) continue;
                    paintVertex.vertex = vrt;

                    foreach (Vertex neighbourVertex in vrt.waysVertex)
                    {
                        if (neighbourVertex == null) continue;
                        featureType = vrt.waysVertexColors[vrt.waysVertex.IndexOf(neighbourVertex)];
                        if (!selectPaintVehicle[featureType]) continue;
                        paintVertex.waysVertex.Add(neighbourVertex, featureType);
                    }

                    if (paintVertex.waysVertex.Count > 0)
                    {
                        paintVertices.Add(paintVertex.vertex, paintVertex);
                        paintVertex = new PaintVertex();
                    }
                }
            }
        }
        
        if (Camera.current == null) return;
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.current);

        if (paintVertices != null && paintVertices.Count > 0) 
        {
            foreach (KeyValuePair<Vertex, PaintVertex> vrt in paintVertices) 
            {
                if (!GeometryUtility.TestPlanesAABB(planes, vrt.Value.vertex.GetComponent<Renderer>().bounds)) continue;
                foreach (KeyValuePair<Vertex, TrackName> neighbourVertex in vrt.Value.waysVertex)
                {
                    Handles.color = colorList[neighbourVertex.Value];
                    Handles.DrawLine(vrt.Value.vertex.GetComponent<Transform>().position, neighbourVertex.Key.GetComponent<Transform>().position);
                }
            }
        }

        Event current = Event.current;
        //int controlID = GUIUtility.GetControlID(FocusType.Passive);
        GUIUtility.GetControlID(FocusType.Passive);

        switch (current.type)
        {
            case EventType.MouseDown:
                //Debug.Log(this.GetType() + ": EventType.MouseDown " + current.button);
                if (current.button == 0 && state != State.NONE)//(state > -1)
                {
                    Paint(current);
                }
                break;

            case EventType.keyDown:
                if (current.keyCode == KeyCode.C) isSetWay = true;
                break;
            case EventType.keyUp:
                if (current.keyCode == KeyCode.C)
                {
                    isSetWay = false;
                    oldVertex = null;
                }
                break;
        }
    }

    public static void CalcWays()
    {
        FindObjectOfType<WayPoints>().Awake();
        if (WayPoints.instance.vertices != null && WayPoints.instance.vertices.Length > 0)
        {
            foreach (Vertex vertex_ in WayPoints.instance.vertices)
            {
                foreach (TrackDict track_ in vertex_.track)
                {
                    foreach (Track track__ in track_)
                    {
                        track__.track.Clear();
                    }
                    track_.Clear();
                    track_.Clear();
                }
                vertex_.track.Clear();
            }
            WayPoints.instance.calcEditor = true;
            EditorUtility.SetDirty(WayPoints.instance);

            float distanceTrack = 0;
            DijkstraAlg dijkstra;
            TrackDict trackDict;
            
            foreach (TrackName featureType in colorList.keys) //colorList.keys
            {
                dijkstra = new DijkstraAlg(featureType, 0);
                foreach (Vertex start in WayPoints.instance.vertices)
                {
                    if ((trackDict = start.track[featureType]) == null) start.track.Add(featureType, trackDict = new TrackDict());

                    foreach (Vertex target in WayPoints.instance.vertices)
                    {
                        if (start == target) continue;
                        if (!trackDict.ContainsKey(target))
                        {
                            dijkstra.track = null;
                            dijkstra.GetTrack(start, target, target.position, ref distanceTrack); //Vector3.zero
                            //CL.Log("distanceTrack " + distanceTrack);
                            trackDict.Add(target, new Track(new List<Vertex>(dijkstra.track), distanceTrack));
                        }
                    }
                    EditorUtility.SetDirty(start);
                }
            }
        }
    }

    public static void SetCalcEditor(bool on)
    {
        if (WayPoints.instance == null) WayPoints.instance = FindObjectOfType<WayPoints>();
        WayPoints.instance.calcEditor = on;
        EditorUtility.SetDirty(WayPoints.instance);
    }

    private static List<TrackName> threads = new List<TrackName>();
    private static List<Vertex> setVertices = new List<Vertex>();

    public static void FastCalcWays()
    {
        FindObjectOfType<WayPoints>().Awake();
        if (WayPoints.instance.vertices != null && WayPoints.instance.vertices.Length > 0)
        {
            foreach (Vertex vertex_ in WayPoints.instance.vertices)
            {
                foreach (TrackDict track_ in vertex_.track)
                {
                    foreach (Track track__ in track_)
                    {
                        track__.track.Clear();
                    }
                    track_.Clear();
                    track_.Clear();
                }
                vertex_.track.Clear();
            }
            WayPoints.instance.calcEditor = true;
            EditorUtility.SetDirty(WayPoints.instance);
            
            int id = 0;
            foreach (TrackName featureType in colorList.keys) //colorList.keys //AEEditorTools.LoadResources<EdgeColorsPrefab>("Prefabs/LHEdgeColorsPrefab").color.keys
            {
                Thread t = new Thread(new ParameterizedThreadStart(ThreadCalcWays));
                t.Name = "CalcWays_" + id;
                threads.Add(featureType);
                t.Start(featureType);
            }

            while(setVertices.Count > 0 || threads.Count > 0)
            {
                if (setVertices.Count > 0)
                {
                    EditorUtility.SetDirty(setVertices[0]);
                    setVertices.RemoveAt(0);
                }
                else
                {
                    Thread.Sleep(10);
                }
                Thread.Sleep(10);
            }
        }
    }
    
    public static void ThreadCalcWays(object param)
    {
        float distanceTrack = 0;
        DijkstraAlg dijkstra;
        TrackDict trackDict;
        TrackName featureType = (TrackName)param;

        dijkstra = new DijkstraAlg(featureType, 0);
        foreach (Vertex start in WayPoints.instance.vertices)
        {
            if ((trackDict = start.track[featureType]) == null) start.track.Add(featureType, trackDict = new TrackDict());

            foreach (Vertex target in WayPoints.instance.vertices)
            {
                if (start == target) continue;
                if (!trackDict.ContainsKey(target))
                {
                    dijkstra.GetTrack(start, target, Vector3.zero, ref distanceTrack);
                    //CL.Log("distanceTrack " + distanceTrack);
                    trackDict.Add(target, new Track(new List<Vertex>(dijkstra.track), distanceTrack));
                }
            }
            setVertices.Add(start);
            //EditorUtility.SetDirty(start);
        }
        threads.Remove(featureType);
    }

    PaintVertex CreateVertex(Vector3 point)
    {
        GameObject vrt = (GameObject)PrefabUtility.InstantiatePrefab(vertex);
        Vertex newVertex = vrt.GetComponent<Vertex>();
        newVertex.waysVertex.Clear();
        newVertex.waysVertexColors.Clear();
        //newVertex.idForEditor = ++Vertex.lastIdForEditor;
        vrt.transform.position = point + new Vector3(0, heightOffset, 0);
        vrt.transform.parent = GameObject.Find("Verticles").transform;

        PaintVertex result = new PaintVertex(newVertex);
        paintVertices.Add(newVertex, result);
        return result;
    }

    void RemoveVertex(Vertex removeVertex)
    {
        if (removeVertex == null) return;
        Vertex wayVertex;
        int index;
        for (int i = 0; i < removeVertex.waysVertex.Count; i++) //remove ways
        {
            wayVertex = removeVertex.waysVertex[i];
            index = wayVertex.waysVertex.IndexOf(removeVertex);
            if (index != -1)
            {
                wayVertex.waysVertexColors.RemoveAt(index); //remove color
                wayVertex.waysVertex.Remove(removeVertex); //remove vertex
            }

            if (paintVertices.ContainsKey(wayVertex)) paintVertices[wayVertex].waysVertex.Remove(removeVertex);
            EditorUtility.SetDirty(wayVertex);
        }
        paintVertices.Remove(removeVertex);
        
        removeVertex.waysVertex.Clear();
        removeVertex.waysVertexColors.Clear();
        DestroyImmediate(removeVertex.GetComponent<Transform>().gameObject);
    }

    void AddWays(Vertex toVertex, Vertex fromVertex)
    {
        if (fromVertex != null && toVertex != null && fromVertex != toVertex && !fromVertex.waysVertex.Contains(toVertex) && !toVertex.waysVertex.Contains(fromVertex))
        {
            toVertex.waysVertexColors.Add(colorIndex);
            toVertex.waysVertex.Add(fromVertex);
            fromVertex.waysVertexColors.Add(colorIndex);
            fromVertex.waysVertex.Add(toVertex);
            EditorUtility.SetDirty(fromVertex);
            EditorUtility.SetDirty(toVertex);

            if (!paintVertices.ContainsKey(toVertex)) paintVertices.Add(toVertex, new PaintVertex(toVertex));
            if (!paintVertices.ContainsKey(fromVertex)) paintVertices.Add(fromVertex, new PaintVertex(fromVertex));
            paintVertices[toVertex].waysVertex.Add(fromVertex, colorIndex);
            paintVertices[fromVertex].waysVertex.Add(toVertex, colorIndex);
        }
    }

    bool RemoveWay(Vertex toVertex, Vertex fromVertex)
    {
        if (fromVertex != null && toVertex != null && fromVertex != toVertex && fromVertex.waysVertex.Contains(toVertex) && toVertex.waysVertex.Contains(fromVertex))
        {
            fromVertex.waysVertexColors.RemoveAt(fromVertex.waysVertex.IndexOf(toVertex));
            fromVertex.waysVertex.Remove(toVertex);
            toVertex.waysVertexColors.RemoveAt(toVertex.waysVertex.IndexOf(fromVertex));
            toVertex.waysVertex.Remove(fromVertex);

            paintVertices[toVertex].waysVertex.Remove(fromVertex);
            paintVertices[fromVertex].waysVertex.Remove(toVertex);

            EditorUtility.SetDirty(fromVertex);
            EditorUtility.SetDirty(toVertex);
            return true;
        }
        return false;
    }

    void Paint(Event current) 
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
		RaycastHit hit;

        switch (state)
        {
            case State.SET_VERTEX:
                if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                {
                    PaintVertex paintVertex = null;
                    if (hit.collider.tag == "Vertex")
                    {
                        Vertex lhvrt = hit.collider.gameObject.GetComponent<Vertex>();
                        if (lhvrt != null) paintVertex = paintVertices[lhvrt];
                    }
                    if (paintVertex == null) paintVertex = CreateVertex(hit.point);

                    if (isSetWay && oldVertex != null) AddWays(paintVertex.vertex, oldVertex);//GetButton("c")
                    oldVertex = paintVertex.vertex;
                }
                break;

            case State.RMV_VERTEX:
                if (Physics.Raycast(ray, out hit, Mathf.Infinity) && hit.transform.tag == "Vertex") RemoveVertex(hit.transform.GetComponent<Vertex>());
                break;

            case State.SET_WAY:
                if (Physics.Raycast(ray, out hit, Mathf.Infinity) && hit.transform.tag == "Vertex")
                {
                    if (from == null)
                    {
                        from = hit.transform.gameObject;
                    }
                    else
                    {
                        AddWays(hit.transform.gameObject.GetComponent<Vertex>(), from.GetComponent<Vertex>());
                        from = null;
                    }
                }
                break;

            case State.RMV_WAY:
                if (Physics.Raycast(ray, out hit, Mathf.Infinity) && hit.transform.tag == "Vertex")
                {
                    if (from == null)
                    {
                        from = hit.transform.gameObject;
                    }
                    else
                    {
                        RemoveWay(hit.transform.gameObject.GetComponent<Vertex>(), from.GetComponent<Vertex>());
                        from = null;
                    }
                }
                break;

            case State.AGG_VERTEX:
                if (Physics.Raycast(ray, out hit, Mathf.Infinity) && hit.transform.tag == "Vertex")
                {
                    GameObject go = hit.transform.gameObject;
                    if (selectedVertices.Contains(go)) break;
                    selectedVertices.Add(go);

                    Transform child = go.transform.GetChild(0);
                    if (child == null) break;
                    child.gameObject.SetActive(true);
                }
                break;

            default:
                break;
        }
    }

    private void AggregateVertices() 
    {
        List<Vertex> vertexList = new List<Vertex>();
        foreach (GameObject go in selectedVertices) vertexList.Add(go.GetComponent<Vertex>());
        foreach (Vertex from in vertexList) foreach (Vertex to in vertexList) AddWays(to, from);
    }

    public static void CheckResources<T>(ref T object_, string path) where T : UnityEngine.Object
    {
        if (object_ == null) object_ = AssetDatabase.LoadAssetAtPath<T>(GetDirectory() + path); //Resources.Load<GameObject>(path);
    }

    public static string GetDirectory()
    {
        string resuslt = AEEditorTools.GetDirectoryCurretScript(2);
        int index = resuslt.IndexOf("Assets");
        return resuslt.Substring(index, resuslt.LastIndexOf("Editor") - index);
    }

    bool ColorButton(string name, bool isSelect, Color selectColor, params GUILayoutOption[] options)
    {
        if (isSelect)
        {
            bool result;
            Color colorDefault = GUI.color;
            GUI.color = selectColor;
            result = GUILayout.Button(name, options);
            GUI.color = colorDefault;
            return result;
        }
        else
        {
            return GUILayout.Button(name, options);
        }
    }

    void ColorLabel(string name, Color color)
    {
        Color colorDefault = GUI.color;
        GUI.color = color;
        EditorGUILayout.LabelField(name);
        GUI.color = colorDefault;
    }
}