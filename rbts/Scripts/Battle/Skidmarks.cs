using System;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Script used by the car Script to create skidmark meshes when cornering.
/// Just create an empty GameObject and attach this.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Skidmarks : MonoBehaviour
{
    /// <summary>
    /// Data structure describing a section of skidmarks.
    /// </summary>
    public class MarkSection
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector3 positionLeft;
        public Vector3 positionRight;
        public float intensity;
        public int lastIndex = DEFAULT_SKIDMARK_ID;
    }

    public const int DEFAULT_SKIDMARK_ID = -1;

    /// <summary>
    /// Maximal number of skidmarks.
    /// </summary>
    public int maxMarks = 512;

    /// <summary>
    /// Width of skid marks.
    /// </summary>
    public float markWidth = 0.225f;

    /// <summary>
    /// Time interval new mesh segments are generated
    /// in the lower this value, the smoother the generated tracks.
    /// </summary>
    public float updateRate = 0.1f;

    public float maxHeight = 10f;

    private MeshFilter meshFilter;
    private bool newTrackFlag = true;
    private bool updateMeshFlag = true;
    private int marksCount;
    private float updateTime;
    private MarkSection[] skidmarks;

    private List<Vector3> vertices = new List<Vector3>();
    private List<Vector3> normals = new List<Vector3>();
    private List<Color> colors = new List<Color>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<int> triangles = new List<int>();

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();

        if (meshFilter.mesh == null)
            meshFilter.mesh = new Mesh();

        skidmarks = new MarkSection[maxMarks];

        for (var i = 0; i < maxMarks; i++)
            skidmarks[i] = new MarkSection();
    }

    void Update()
    {
        // Update mesh if skidmarks have changed since last frame.
        if (updateMeshFlag)
            UpdateMesh();
    }

    void FixedUpdate()
    {
        // Set flag for creating new segments this frame if an update is pending.
        newTrackFlag = false;

        updateTime += Time.deltaTime;

        if (updateTime > updateRate)
        {
            newTrackFlag = true;
            updateTime -= updateRate;
        }
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
        intensity = Mathf.Clamp01(intensity);

        // Get index for new segment.
        int currentMarkIndex = marksCount;

        // Reuse lastIndex if we don't need to create a new one this frame.
        if ((lastIndex != DEFAULT_SKIDMARK_ID) && !newTrackFlag)
            currentMarkIndex = lastIndex;

        // Setup skidmark structure.
        MarkSection currentMark = skidmarks[currentMarkIndex % maxMarks];

        currentMark.position = position + normal * 0.05f + transform.position;
        currentMark.normal = normal;
        currentMark.intensity = intensity;

        if ((lastIndex == DEFAULT_SKIDMARK_ID) || newTrackFlag)
            currentMark.lastIndex = lastIndex;

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
            marksCount++;

        updateMeshFlag = true;

        return currentMarkIndex;
    }

    /// <summary>
    /// Regenerate the skidmarks mesh.
    /// </summary>
    private void UpdateMesh()
    {
        // Create skidmark mesh coordinates.
        vertices.Clear();
        normals.Clear();
        colors.Clear();
        uvs.Clear();
        triangles.Clear();

        int segmentCount = 0;
        int baseIndex = 0;
        Vector3 normal = Vector3.up;
        Color color = Color.white;
        for (int i = 0; i < marksCount && i < maxMarks; i++)
        {
            if (skidmarks[i].lastIndex != DEFAULT_SKIDMARK_ID &&
                skidmarks[i].lastIndex > marksCount - maxMarks)
            {
                MarkSection mark = skidmarks[i];
                MarkSection last = skidmarks[mark.lastIndex % maxMarks];

                vertices.Add(last.positionLeft);
                vertices.Add(last.positionRight);
                vertices.Add(mark.positionLeft);
                vertices.Add(mark.positionRight);

                normals.Add(normal); // last.normal;
                normals.Add(normal); // last.normal;
                normals.Add(normal); // mark.normal;
                normals.Add(normal); // mark.normal;

                color.a = last.intensity;
                colors.Add(color);
                colors.Add(color);
                color.a = mark.intensity;
                colors.Add(color);
                colors.Add(color);

                float distance = Vector3.Distance(mark.positionLeft, last.positionLeft);
                float maxUV = distance / maxHeight;

                uvs.Add(new Vector2(0, 0));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, maxUV));
                uvs.Add(new Vector2(1, maxUV));

                baseIndex = segmentCount * 4;

                triangles.Add(baseIndex + 0);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);

                segmentCount++;
            }
        }

        // Update mesh.
        Mesh mesh = meshFilter.mesh;

        mesh.Clear();

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0, false);
        mesh.SetNormals(normals);
        mesh.SetColors(colors);
        mesh.SetUVs(0, uvs);

        updateMeshFlag = false;
    }
}