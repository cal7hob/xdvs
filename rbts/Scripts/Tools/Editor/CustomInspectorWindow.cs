using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class CustomInspectorWindow : EditorWindowScroll //EditorWindow
{
    public class ComponentInfo
    {
        public delegate void CustomInspectorParse(AESerializedObject SOcomponent);
        public static event CustomInspectorParse customParser;
        public ComponentInfo(Component component)
        {
            name = component.GetType().Name;//.ToString();
            SOcomponent = new AESerializedObject(component, false);
            if (customParser != null) customParser(SOcomponent); //SOcomponent.SubscribeParseHandler(typeof(SerializedSoundEvent), typeof(JNSoundEvent));
            SOcomponent.Parse();
        }

        public AESerializedObject SOcomponent;

        public string name;
        public bool foldout = false;
    }

    protected static CustomInspectorWindow window;
    private int height;
    private GameObject selectObject;
    private List<ComponentInfo> components = new List<ComponentInfo>();

    private static GUIStyle styleBarButton;
    
    [MenuItem("Tools/Custom Inspector")]
    public static void Init()
    {
        window = GetWindow<CustomInspectorWindow>("Custom Inspector");
        window.minSize = new Vector2(310, 100); //width height
        window.Load();
    }

    public void Load() { }

    private void Apply()
    {
        //CL.Log(DebugSource.Editor, selectObject.scene.isLoaded); //PrefabUtility.GetPrefabObject(selectObject) != null //selectObject.activeInHierarchy
        if (selectObject == null) return;
        if (selectObject.scene.isLoaded)
        {
            //CL.Log(DebugSource.Editor, "set scene");
            foreach (ComponentInfo component in components) component.SOcomponent.SceneApply();
        }
        else
        {
            //CL.Log(DebugSource.Editor, "set prefab");
            foreach (ComponentInfo component in components) component.SOcomponent.PrefabApply();
        }
    }

    private static void InitStyles()
    {
        styleBarButton = GUI.skin.GetStyle("toolbarbutton");
    }

    void OnDestroy()
    {
        if (selectObject != null)
        {
            Apply();
        }
    }

    public void Update()
    {
        //thisObject = Selection.activeGameObject; //Selection.gameObjects[0]; //Selection.transforms[0].gameObject;
        if (Selection.activeGameObject != selectObject && Selection.activeGameObject != null)
        {
            if (selectObject != null) Apply();
            selectObject = Selection.activeGameObject;
            components.Clear();
            foreach (Component component in selectObject.GetComponents<MonoBehaviour>())
            {
                components.Add(new ComponentInfo(component));
            }
            Repaint();
        }
    }
    
    public virtual void OnGUI()
    {
        if (window == null) Init();
        if (styleBarButton == null) InitStyles();

        int windowWidth = (int)window.position.width - 4;
        AERectPosition rect = new AERectPosition(windowWidth); //(int)window.position.widt
        rect.width = windowWidth - 24; //24 14
        AERectPosition rectTop = rect.Clone();

        int barHeight = 18;
        EditorGUI.LabelField(rectTop.NextLineNotSet(windowWidth - 4, barHeight), "", styleBarButton); //bar

        //SOSettings.foldout = EditorGUI.Foldout(rectTop.NextLine(10, barHeight, rect.separator), SOSettings.foldout, "", true); //if (SOSettings.foldout)
        if (GUI.Button(rectTop.NextLine(45, barHeight), "Update", styleBarButton))
        {
            foreach (ComponentInfo component in components)
            {
                component.SOcomponent.Update();
            }
        }

        if (GUI.Button(rectTop.NextLine(45, barHeight), "Apply", styleBarButton)) Apply();

        //GUI.Box(rectTop.Next((int)window.position.width, (int)(window.position.height - (rectTop.GetRect().yMax))), "");
        rectTop.Next((int)window.position.width, (int)(window.position.height - (rectTop.GetRect().yMax + rectTop.separator)));
        scrollBoxAll = GUI.BeginScrollView(rectTop.GetRect(), scrollBoxAll, new Rect(0, 0, windowWidth, height)); //rect.NextNotSet().yMax - 42
        //AESerializedProperty s;
        foreach (ComponentInfo component in components)
        {
            if (component.foldout = EditorGUI.Foldout(rect.GetRect(), component.foldout, component.name, true))
            {
                rect.Tab();
                component.SOcomponent.ReInit(rect);
                component.SOcomponent.PropertyFieldChilds(true, true, false);
                //s = component.SOcomponent.GetProperty("services", true);
                //if (s != null) ((AESerializedIList)s).ListButtons(Color.green, s.serializedObject.rectPosition.windowWidth - s.serializedObject.rectPosition.separator, 50, @"Assets/Scripts/Tools/Editor", true); //, 50
                rect.TabEnd();
            }
            rect.Next(200);
        }

        height = rect.startY;
        GUI.EndScrollView();
    }
}