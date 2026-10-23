
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

    public Color surfaceColor =
        new Color32(37, 56, 88, 255);

    [Header("Gravity Grid Colors")]
    public bool useGravityColors = true;

    public Color flatColor =
        new Color32(55, 195, 170, 255);

    public Color shallowColor =
        new Color32(144, 210, 105, 255);

    public Color mediumColor =
        new Color32(240, 209, 86, 255);

    public Color deepColor =
        new Color32(255, 132, 55, 255);

    public Color dangerColor =
        new Color32(220, 55, 75, 255);

    [Header("Depth Thresholds")]
    [Tooltip("Depth at which lines become yellow.")]
    public float mediumDepth = 0.45f;

    [Tooltip("Depth at which lines become orange.")]
    public float deepDepth = 0.90f;

    [Tooltip("Depth at which lines become red.")]
    public float dangerDepth = 1.50f;

    [Header("Original Grid Color")]
    public Color lineColor =
        new Color32(85, 185, 255, 255);

    private const int PixelsPerCell = 64;

    private Mesh generatedMesh;
    private Texture2D generatedTexture;
    private Material runtimeMaterial;

    private Vector3[] baseVertices;
    private Vector3[] deformedVertices;

    private BlackHole[] blackHoles;
    private SolarSystem3D[] solarSystems;

    private struct Opening
    {
        public Vector2 position;
        public float radiusSquared;
    }

    private void Start()
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

        solarSystems = transform.parent
            .GetComponentsInChildren<SolarSystem3D>();

        // Build exactly the same physical mesh as before.
        GenerateSurface();
        UpdateDeformation();

        // The texture now samples the deformed mesh.
        GenerateGridTexture();

        runtimeMaterial = new Material(gridMaterial);
        runtimeMaterial.SetTexture(
            "_BaseMap", generatedTexture
        );
        runtimeMaterial.SetColor(
            "_BaseColor", Color.white
        );

        MeshRenderer gridRenderer =
            GetComponent<MeshRenderer>();

        gridRenderer.sharedMaterial = runtimeMaterial;
        gridRenderer.shadowCastingMode =
            ShadowCastingMode.Off;
        gridRenderer.receiveShadows = false;

        MeshRenderer fieldRenderer =
            transform.parent.GetComponent<MeshRenderer>();

        if (fieldRenderer != null)
            fieldRenderer.enabled = false;
    }

    private void GenerateSurface()
    {
        int vertexColumns = meshColumns + 1;
        int vertexRows = meshRows + 1;

        baseVertices = new Vector3[
            vertexColumns * vertexRows
        ];

        deformedVertices =
            new Vector3[baseVertices.Length];

        Vector2[] uvs =
            new Vector2[baseVertices.Length];

        int[] triangles =
            new int[meshColumns * meshRows * 6];

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

        GetComponent<MeshFilter>().sharedMesh =
            generatedMesh;
    }

    private void UpdateDeformation()
    {
        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 vertex = baseVertices[i];
            float totalDepth = 0f;

            foreach (BlackHole hole in blackHoles)
            {
                if (hole == null ||
                    !hole.isActiveAndEnabled)
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

                float proximity =
                    1f - distance / radius;

                float depth = Mathf.Clamp(
                    hole.gravityStrength * 0.12f,
                    0f,
                    4f
                );

                totalDepth +=
                    depth * proximity * proximity;
            }

            // Original solar deformation, unchanged.
            foreach (SolarSystem3D solar in solarSystems)
            {
                if (solar == null ||
                    !solar.isActiveAndEnabled)
                    continue;

                Vector3 sunPosition =
                    transform.InverseTransformPoint(
                        solar.transform.position
                    );

                float dx = vertex.x - sunPosition.x;
                float dz = vertex.z - sunPosition.z;

                float radius = Mathf.Max(
                    0.01f,
                    solar.meshGravityRadius
                );

                float distance = Mathf.Sqrt(
                    dx * dx + dz * dz
                );

                if (distance >= radius)
                    continue;

                float proximity =
                    1f - distance / radius;

                float smooth =
                    proximity * proximity *
                    (3f - 2f * proximity);

                totalDepth +=
                    Mathf.Max(
                        0f,
                        solar.meshGravityDepth
                    ) * smooth;
            }

            vertex.y -= totalDepth;
            deformedVertices[i] = vertex;
        }

        generatedMesh.vertices = deformedVertices;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();
    }

    // Read surface depth from the SAME vertices used
    // to create the physical curved grid.
    private float SampleMeshDepth(float u, float v)
    {
        float gx = Mathf.Clamp01(u) * meshColumns;
        float gz = Mathf.Clamp01(v) * meshRows;

        int x0 = Mathf.Min(
            Mathf.FloorToInt(gx),
            meshColumns - 1
        );

        int z0 = Mathf.Min(
            Mathf.FloorToInt(gz),
            meshRows - 1
        );

        float tx = gx - x0;
        float tz = gz - z0;

        int stride = meshColumns + 1;

        int i00 = z0 * stride + x0;
        int i10 = i00 + 1;
        int i01 = i00 + stride;
        int i11 = i01 + 1;

        float h0 = Mathf.Lerp(
            deformedVertices[i00].y,
            deformedVertices[i10].y,
            tx
        );

        float h1 = Mathf.Lerp(
            deformedVertices[i01].y,
            deformedVertices[i11].y,
            tx
        );

        float surfaceHeight = Mathf.Lerp(
            h0,
            h1,
            tz
        );

        return Mathf.Max(
            0f,
            height - surfaceHeight
        );
    }

    private Color GetDepthColor(float depth)
    {
        if (!useGravityColors)
            return lineColor;

        float shallowDepth =
            Mathf.Max(0.001f, mediumDepth * 0.35f);

        float medium =
            Mathf.Max(shallowDepth + 0.001f, mediumDepth);

        float deep =
            Mathf.Max(medium + 0.001f, deepDepth);

        float danger =
            Mathf.Max(deep + 0.001f, dangerDepth);

        if (depth <= shallowDepth)
        {
            return Color.Lerp(
                flatColor,
                shallowColor,
                Mathf.Clamp01(depth / shallowDepth)
            );
        }

        if (depth <= medium)
        {
            return Color.Lerp(
                shallowColor,
                mediumColor,
                Mathf.InverseLerp(
                    shallowDepth,
                    medium,
                    depth
                )
            );
        }

        if (depth <= deep)
        {
            return Color.Lerp(
                mediumColor,
                deepColor,
                Mathf.InverseLerp(
                    medium,
                    deep,
                    depth
                )
            );
        }

        return Color.Lerp(
            deepColor,
            dangerColor,
            Mathf.InverseLerp(
                deep,
                danger,
                depth
            )
        );
    }

    private void GenerateGridTexture()
    {
        int textureWidth =
            columns * PixelsPerCell + 1;

        int textureHeight =
            rows * PixelsPerCell + 1;

        generatedTexture = new Texture2D(
            textureWidth,
            textureHeight,
            TextureFormat.RGBA32,
            true
        );

        generatedTexture.name =
            "Gravity Grid Depth Colors";

        generatedTexture.wrapMode =
            TextureWrapMode.Clamp;

        generatedTexture.filterMode =
            FilterMode.Trilinear;

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

        // Cache openings once instead of calculating
        // their transforms millions of times.
        System.Collections.Generic.List<Opening>
            openings =
                new System.Collections.Generic.List<Opening>();

        foreach (BlackHole hole in blackHoles)
        {
            if (hole == null ||
                !hole.isActiveAndEnabled)
                continue;

            Vector3 position =
                transform.InverseTransformPoint(
                    hole.transform.position
                );

            float radius = Mathf.Max(
                0.01f,
                hole.captureRadius
            );

            openings.Add(new Opening
            {
                position = new Vector2(
                    position.x,
                    position.z
                ),
                radiusSquared = radius * radius
            });
        }

        Color32[] pixels = new Color32[
            textureWidth * textureHeight
        ];

        Color32 background = surfaceColor;

        for (int y = 0; y < textureHeight; y++)
        {
            int cellY = y % PixelsPerCell;

            int distanceY = Mathf.Min(
                cellY,
                PixelsPerCell - cellY
            );

            bool horizontal =
                distanceY <= radiusZ;

            float v =
                (float)y / (textureHeight - 1);

            float localZ =
                (v - 0.5f) * length;

            for (int x = 0; x < textureWidth; x++)
            {
                int cellX = x % PixelsPerCell;

                int distanceX = Mathf.Min(
                    cellX,
                    PixelsPerCell - cellX
                );

                bool vertical =
                    distanceX <= radiusX;

                float u =
                    (float)x / (textureWidth - 1);

                float localX =
                    (u - 0.5f) * width;

                Color32 pixel = background;

                // Color only the grid lines.
                if (horizontal || vertical)
                {
                    float depth = SampleMeshDepth(u, v);
                    pixel = GetDepthColor(depth);
                }

                // Preserve the original black-hole
                // transparency/opening behavior.
                foreach (Opening opening in openings)
                {
                    float dx =
                        localX - opening.position.x;

                    float dz =
                        localZ - opening.position.y;

                    if (dx * dx + dz * dz <
                        opening.radiusSquared)
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

    private void OnDestroy()
    {
        if (generatedMesh != null)
            Destroy(generatedMesh);

        if (generatedTexture != null)
            Destroy(generatedTexture);

        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}
