
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GravityGrid : MonoBehaviour
{
    [Header("Visible Grid")]
    public float width = 12f;
    public float length = 18f;
    [Range(1, 100)] public int columns = 16;
    [Range(1, 100)] public int rows = 24;

    [Header("Mesh Detail")]
    [Range(8, 200)] public int meshColumns = 64;
    [Range(8, 200)] public int meshRows = 96;

    [Header("Appearance")]
    public Material gridMaterial;
    public float height = 0.13f;
    public float lineWidth = 0.025f;

    public Color surfaceColor = new Color32(37, 56, 88, 255);
    public Color lineColor = new Color32(85, 185, 255, 255);

    private const int PixelsPerCell = 64;

    private Mesh generatedMesh;
    private Texture2D generatedTexture;
    private Material runtimeMaterial;

    private Vector3[] baseVertices;
    private Vector3[] deformedVertices;

    private BlackHole[] blackHoles;

    void Start()
    {
        if (gridMaterial == null)
        {
            Debug.LogError(
                "GravityGrid: Assign GravityGrid_Mat.",
                this
            );
            return;
        }

        blackHoles = transform.parent
            .GetComponentsInChildren<BlackHole>();

        GenerateSurface();
        GenerateGridTexture();

        runtimeMaterial = new Material(gridMaterial);
        runtimeMaterial.SetTexture("_BaseMap", generatedTexture);
        runtimeMaterial.SetColor("_BaseColor", Color.white);

        MeshRenderer gridRenderer = GetComponent<MeshRenderer>();

        gridRenderer.sharedMaterial = runtimeMaterial;
        gridRenderer.shadowCastingMode = ShadowCastingMode.Off;
        gridRenderer.receiveShadows = false;

        // Keep the physical surface but hide its solid appearance.
        MeshRenderer fieldRenderer =
            transform.parent.GetComponent<MeshRenderer>();

        if (fieldRenderer != null)
            fieldRenderer.enabled = false;

        UpdateDeformation();
    }

    void GenerateSurface()
    {
        int vertexColumns = meshColumns + 1;
        int vertexRows = meshRows + 1;

        baseVertices = new Vector3[
            vertexColumns * vertexRows
        ];

        deformedVertices = new Vector3[baseVertices.Length];
        Vector2[] uvs = new Vector2[baseVertices.Length];

        int[] triangles = new int[meshColumns * meshRows * 6];

        for (int z = 0; z < vertexRows; z++)
        {
            for (int x = 0; x < vertexColumns; x++)
            {
                int index = z * vertexColumns + x;

                float u = (float)x / meshColumns;
                float v = (float)z / meshRows;

                baseVertices[index] = new Vector3(
                    (u - 0.5f) * width,
                    height,
                    (v - 0.5f) * length
                );

                uvs[index] = new Vector2(u, v);
            }
        }

        int t = 0;

        for (int z = 0; z < meshRows; z++)
        {
            for (int x = 0; x < meshColumns; x++)
            {
                int a = z * vertexColumns + x;
                int b = a + 1;
                int c = a + vertexColumns;
                int d = c + 1;

                triangles[t++] = a;
                triangles[t++] = c;
                triangles[t++] = d;

                triangles[t++] = a;
                triangles[t++] = d;
                triangles[t++] = b;
            }
        }

        generatedMesh = new Mesh();
        generatedMesh.name = "Black Hole Gravity Grid";
        generatedMesh.MarkDynamic();

        generatedMesh.vertices = baseVertices;
        generatedMesh.uv = uvs;
        generatedMesh.triangles = triangles;

        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = generatedMesh;
    }

    void UpdateDeformation()
    {
        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 vertex = baseVertices[i];

            float totalDepth = 0f;

            foreach (BlackHole hole in blackHoles)
            {
                if (hole == null || !hole.isActiveAndEnabled)
                    continue;

                Vector3 holePosition =
                    transform.InverseTransformPoint(
                        hole.transform.position
                    );

                float dx = vertex.x - holePosition.x;
                float dz = vertex.z - holePosition.z;

                float distance = Mathf.Sqrt(
                    dx * dx + dz * dz
                );

                float radius = Mathf.Max(
                    0.01f,
                    hole.influenceRadius
                );

                if (distance >= radius)
                    continue;

                float proximity = 1f - distance / radius;

                float depth = Mathf.Clamp(
                    hole.gravityStrength * 0.12f,
                    0f,
                    4f
                );

                totalDepth += depth * proximity * proximity;
            }

            vertex.y -= totalDepth;
            deformedVertices[i] = vertex;
        }

        generatedMesh.vertices = deformedVertices;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();
    }

    void GenerateGridTexture()
    {
        int textureWidth = columns * PixelsPerCell + 1;
        int textureHeight = rows * PixelsPerCell + 1;

        generatedTexture = new Texture2D(
            textureWidth,
            textureHeight,
            TextureFormat.RGBA32,
            true
        );

        generatedTexture.name = "Gravity Grid With Openings";
        generatedTexture.wrapMode = TextureWrapMode.Clamp;
        generatedTexture.filterMode = FilterMode.Trilinear;
        generatedTexture.anisoLevel = 8;

        float cellWidth = width / columns;
        float cellLength = length / rows;

        int radiusX = Mathf.Max(
            1,
            Mathf.RoundToInt(
                lineWidth / cellWidth *
                PixelsPerCell * 0.5f
            )
        );

        int radiusZ = Mathf.Max(
            1,
            Mathf.RoundToInt(
                lineWidth / cellLength *
                PixelsPerCell * 0.5f
            )
        );

        Color32[] pixels = new Color32[
            textureWidth * textureHeight
        ];

        Color32 background = surfaceColor;
        Color32 foreground = lineColor;

        for (int y = 0; y < textureHeight; y++)
        {
            int cellY = y % PixelsPerCell;
            int distanceY = Mathf.Min(
                cellY,
                PixelsPerCell - cellY
            );

            bool horizontal = distanceY <= radiusZ;

            float v = (float)y / (textureHeight - 1);
            float localZ = (v - 0.5f) * length;

            for (int x = 0; x < textureWidth; x++)
            {
                int cellX = x % PixelsPerCell;
                int distanceX = Mathf.Min(
                    cellX,
                    PixelsPerCell - cellX
                );

                bool vertical = distanceX <= radiusX;

                float u = (float)x / (textureWidth - 1);
                float localX = (u - 0.5f) * width;

                Color32 pixel =
                    (horizontal || vertical)
                    ? foreground
                    : background;

                // Remove grid pixels inside black-hole openings.
                foreach (BlackHole hole in blackHoles)
                {
                    if (hole == null || !hole.isActiveAndEnabled)
                        continue;

                    Vector3 holePosition =
                        transform.InverseTransformPoint(
                            hole.transform.position
                        );

                    float dx = localX - holePosition.x;
                    float dz = localZ - holePosition.z;

                    float openingRadius = Mathf.Max(
                        0.01f,
                        hole.captureRadius
                    );

                    if (dx * dx + dz * dz <
                        openingRadius * openingRadius)
                    {
                        pixel.a = 0;
                        break;
                    }
                }

                pixels[y * textureWidth + x] = pixel;
            }
        }

        generatedTexture.SetPixels32(pixels);
        generatedTexture.Apply(true, true);
    }

    void OnDestroy()
    {
        if (generatedMesh != null)
            Destroy(generatedMesh);

        if (generatedTexture != null)
            Destroy(generatedTexture);

        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}
