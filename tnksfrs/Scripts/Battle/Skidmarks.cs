using UnityEngine;
using XD;
/// <summary>
/// Script used by the car Script to create skidmark meshes when cornering.
/// Just create an empty GameObject and attach this.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Skidmarks : MonoBehaviour, ISubscriber
{
    /// <summary>
    /// Data structure describing a section of skidmarks.
    /// </summary>
    public class MarkSection
    {
        public Vector3  position;
        public Vector3  normal;
        public Vector3  positionLeft;
        public Vector3  positionRight;
        public float    intensity;
        public int      lastIndex = DEFAULT_SKIDMARK_ID;
    }

    public const int        DEFAULT_SKIDMARK_ID = -1;

    /// <summary>
    /// Maximal number of skidmarks.
    /// </summary>
    public int              maxMarks = 512;

    /// <summary>
    /// Width of skid marks.
    /// </summary>
    public float            markWidth = 0.225f;

    /// <summary>
    /// Time interval new mesh segments are generated
    /// in the lower this value, the smoother the generated tracks.
    /// </summary>
    public float            updateRate = 0.1f;

    public float            maxHeight = 10f;

    private MeshFilter      meshFilter = null;
    private bool            newTrackFlag = true;
    private bool            updateMeshFlag = true;
    private int             marksCount = 0;
    private float           updateTime = 0;
    private MarkSection[]   skidmarks = null;
    private bool            active = true;

    #region ISubscriber    
    public string Description
    {
        get
        {
            return "[Skidmarks] " + name;
        }

        set
        {
            name = value;
        }
    }

    public void Reaction(Message message, params object[] parameters)
    {
        switch (message)
        {
            case Message.SettingsChanged:
                active = QualitySettings.GetQualityLevel() > 0;
                break;
        }
    }
    #endregion

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        active = QualitySettings.GetQualityLevel() > 0;
        //Debug.LogErrorFormat("Graphics: {0}", QualitySettings.GetQualityLevel());

        if (meshFilter.mesh == null)
        {
            meshFilter.mesh = new Mesh();
        }

        skidmarks = new MarkSection[maxMarks];

        for (var i = 0; i < maxMarks; i++)
        {
            skidmarks[i] = new MarkSection();
        }

        StaticType.Options.Instance().AddSubscriber(this);
    }

    private void OnDestroy()
    {
        StaticType.Options.Instance().RemoveSubscriber(this);
    }

    public MarkSection GetSkidMark(int index)
    {
        return skidmarks[index];
    }

    /// <summary>
    /// Called by the car script to add a skid mark.
    /// </summary>
    /// <param name="position">Position.</param>
    /// <param name="normal">Normal.</param>
    /// <param name="intensity">Transparency.</param>
    /// <param name="lastIndex">Connects to the track segment (or it won't display if lastIndex is -1).</param>
    /// <returns>An index value which can be passed as lastIndex to the next AddSkidMark call.</returns>
    public int AddSkidMark(Vector3 position, Vector3 normal, float intensity, int lastIndex)
    {
        if (!active)
        {
            return lastIndex;
        }

        intensity = Mathf.Clamp01(intensity);

        // Get index for new segment.
        int currentMarkIndex = marksCount;

        // Reuse lastIndex if we don't need to create a new one this frame.
        if ((lastIndex != DEFAULT_SKIDMARK_ID) && !newTrackFlag)
        {
            currentMarkIndex = lastIndex;
        }

        // Setup skidmark structure.
        MarkSection currentMark = skidmarks[currentMarkIndex % maxMarks];

        currentMark.position = position + normal * 0.05f + transform.position;
        currentMark.normal = normal;
        currentMark.intensity = intensity;

        if ((lastIndex == DEFAULT_SKIDMARK_ID) || newTrackFlag)
        {
            currentMark.lastIndex = lastIndex;
        }

        // If we have a valid lastIndex, get positions for marks.
        if (currentMark.lastIndex != DEFAULT_SKIDMARK_ID)
        {
            MarkSection lastMark = skidmarks[currentMark.lastIndex % maxMarks];
            Vector3 direction = (currentMark.position - lastMark.position);
            Vector3 xDirection = Vector3.Cross(direction, normal).normalized;

            currentMark.positionLeft = currentMark.position + xDirection * markWidth * 0.5f;
            currentMark.positionRight = currentMark.position - xDirection * markWidth * 0.5f;

            if (lastMark.lastIndex == DEFAULT_SKIDMARK_ID)
            {
                lastMark.positionLeft = currentMark.position + xDirection * markWidth * 0.5f;
                lastMark.positionRight = currentMark.position - xDirection * markWidth * 0.5f;
            }
        }

        if ((lastIndex == DEFAULT_SKIDMARK_ID) || newTrackFlag)
        {
            marksCount++;
        }

        updateMeshFlag = true;

        return currentMarkIndex;
    }

    /// <summary>
    /// Regenerate the skidmarks mesh.
    /// </summary>
    private void UpdateMesh()
    {
        // Count visible segments.
        int segmentCount = 0;

        for (int i = 0; i < marksCount && i < maxMarks; i++)
        {
            if (skidmarks[i].lastIndex != DEFAULT_SKIDMARK_ID &&
                skidmarks[i].lastIndex > marksCount - maxMarks)
            {
                segmentCount++;
            }
        }

        // Create skidmark mesh coordinates.
        Vector3[] vertices = new Vector3[segmentCount * 4];
        Vector3[] normals = new Vector3[segmentCount * 4];
        Color[] colors = new Color[segmentCount * 4];
        Vector2[] uvs = new Vector2[segmentCount * 4];
        int[] triangles = new int[segmentCount * 6];

        segmentCount = 0;

        for (int i = 0; i < marksCount && i < maxMarks; i++)
        {
            if (skidmarks[i].lastIndex != DEFAULT_SKIDMARK_ID &&
                skidmarks[i].lastIndex > marksCount - maxMarks)
            {
                MarkSection mark = skidmarks[i];
                MarkSection last = skidmarks[mark.lastIndex % maxMarks];

                vertices[segmentCount * 4 + 0] = last.positionLeft;
                vertices[segmentCount * 4 + 1] = last.positionRight;
                vertices[segmentCount * 4 + 2] = mark.positionLeft;
                vertices[segmentCount * 4 + 3] = mark.positionRight;

                normals[segmentCount * 4 + 0] = new Vector3(0, 1, 0); // last.normal;
                normals[segmentCount * 4 + 1] = new Vector3(0, 1, 0); // last.normal;
                normals[segmentCount * 4 + 2] = new Vector3(0, 1, 0); // mark.normal;
                normals[segmentCount * 4 + 3] = new Vector3(0, 1, 0); // mark.normal;

                colors[segmentCount * 4 + 0] = new Color(1, 1, 1, last.intensity);
                colors[segmentCount * 4 + 1] = new Color(1, 1, 1, last.intensity);
                colors[segmentCount * 4 + 2] = new Color(1, 1, 1, mark.intensity);
                colors[segmentCount * 4 + 3] = new Color(1, 1, 1, mark.intensity);

                float distance = Vector3.Distance(mark.positionLeft, last.positionLeft);
                float maxUV = distance / maxHeight;

                uvs[segmentCount * 4 + 0] = new Vector2(0, 0);
                uvs[segmentCount * 4 + 1] = new Vector2(1, 0);
                uvs[segmentCount * 4 + 2] = new Vector2(0, maxUV);
                uvs[segmentCount * 4 + 3] = new Vector2(1, maxUV);

                triangles[segmentCount * 6 + 0] = segmentCount * 4 + 0;
                triangles[segmentCount * 6 + 1] = segmentCount * 4 + 2;
                triangles[segmentCount * 6 + 2] = segmentCount * 4 + 1;

                triangles[segmentCount * 6 + 3] = segmentCount * 4 + 1;
                triangles[segmentCount * 6 + 4] = segmentCount * 4 + 2;
                triangles[segmentCount * 6 + 5] = segmentCount * 4 + 3;

                segmentCount++;
            }
        }

        // Update mesh.
        Mesh mesh = meshFilter.mesh;

        mesh.Clear();

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.uv = uvs;

        updateMeshFlag = false;
    }

    private void Update()
    {
        if (!active)
        {
            return;
        }

        // Update mesh if skidmarks have changed since last frame.
        if (updateMeshFlag)
        {
            UpdateMesh();
        }
    }

    private void FixedUpdate()
    {
        if (!active)
        {
            return;
        }

        // Set flag for creating new segments this frame if an update is pending.
        newTrackFlag = false;

        updateTime += Time.deltaTime;

        if (updateTime > updateRate)
        {
            newTrackFlag = true;
            updateTime -= updateRate;
        }
    }
}