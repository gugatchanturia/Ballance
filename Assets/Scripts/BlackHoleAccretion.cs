
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(BlackHole))]
public class BlackHoleAccretion : MonoBehaviour
{
    [Header("Appearance")]
    public Material accretionMaterial;
    public float diskWidth = 1.1f;
    public float surfaceOffset = 0.07f;

    [Header("Colors")]
    public Color hotColor = new Color(1f, 0.88f, 0.42f);
    public Color warmColor = new Color(1f, 0.43f, 0.08f);
    public Color dimColor = new Color(0.55f, 0.12f, 0.025f);
    [Header("Visual Intensity")]
    [Range(0f, 1f)]
    public float glowIntensity = 0.25f;

    [Header("Animation")]
    public float orbitSpeed = 1f;

    [Header("Geometry")]
    [Range(32, 256)]
    public int angularSegments = 128;

    [Range(2, 16)]
    public int radialSegments = 8;

    private readonly List<GameObject> objects =
        new List<GameObject>();

    private readonly List<Mesh> meshes =
        new List<Mesh>();

    private readonly List<Material> materials =
        new List<Material>();

    private readonly List<Texture2D> textures =
        new List<Texture2D>();

    private readonly float[] layerSpeeds =
    {
        0.12f, 0.075f, 0.03f
    };

    private GravityGrid grid;
    private Vector3 holeCenter;
    private Vector3[] surfaceVertices;

    private IEnumerator Start()
    {
        // Wait until the grid and physical surface exist.
        yield return null;
        yield return null;

        BlackHole hole = GetComponent<BlackHole>();

        grid = transform.parent.GetComponentInChildren<GravityGrid>();

        if (grid == null || accretionMaterial == null)
        {
            Debug.LogError(
                "BlackHoleAccretion: Missing grid or material.",
                this
            );
            yield break;
        }

        MeshFilter filter = grid.GetComponent<MeshFilter>();

        if (filter.sharedMesh == null)
        {
            Debug.LogError(
                "BlackHoleAccretion: Grid mesh not generated.",
                this
            );
            yield break;
        }

        surfaceVertices = filter.sharedMesh.vertices;

        int expectedVertices =
            (grid.meshColumns + 1) * (grid.meshRows + 1);

        if (surfaceVertices.Length != expectedVertices)
        {
            Debug.LogError(
                "BlackHoleAccretion: Unexpected grid resolution.",
                this
            );
            yield break;
        }

        // Work entirely in GravityGrid's local coordinates.
        holeCenter = grid.transform.InverseTransformPoint(
            transform.position
        );

        float innerRadius =
            Mathf.Max(0.01f, hole.captureRadius) + 0.05f;

        float width = Mathf.Max(0.2f, diskWidth);

        // Bright event-horizon rim.
        CreateBand(
            "Inner_Rim",
            innerRadius,
            innerRadius + width * 0.24f,
            warmColor,
            hotColor,
            0.95f * glowIntensity,
            0
        );

        // Orbiting luminous matter.
        CreateBand(
            "Orbiting_Disk",
            innerRadius + width * 0.10f,
            innerRadius + width * 0.73f,
            dimColor,
            warmColor,
            0.78f * glowIntensity,
            1
        );

        // Soft outer halo.
        CreateBand(
            "Outer_Halo",
            innerRadius + width * 0.50f,
            innerRadius + width,
            dimColor,
            warmColor,
            0.30f * glowIntensity,
            2
        );
    }

    private void CreateBand(
        string bandName,
        float innerRadius,
        float outerRadius,
        Color darkerColor,
        Color brighterColor,
        float opacity,
        int layer)
    {
        GameObject bandObject = new GameObject(bandName);

        // Parent to the GRID, not the scaled black-hole object.
        bandObject.transform.SetParent(grid.transform, false);

        MeshFilter filter =
            bandObject.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            bandObject.AddComponent<MeshRenderer>();

        Mesh mesh = BuildRingMesh(
            innerRadius,
            outerRadius,
            surfaceOffset + layer * 0.012f
        );

        Texture2D texture = BuildRingTexture(
            darkerColor,
            brighterColor,
            opacity,
            layer
        );

        Material material = new Material(accretionMaterial);
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);

        filter.sharedMesh = mesh;
        renderer.sharedMaterial = material;

        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        objects.Add(bandObject);
        meshes.Add(mesh);
        textures.Add(texture);
        materials.Add(material);
    }

    private Mesh BuildRingMesh(
        float innerRadius,
        float outerRadius,
        float offset)
    {
        int segmentCount = angularSegments;
        int ringCount = radialSegments;

        int vertexCount =
            (segmentCount + 1) * (ringCount + 1);

        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];

        int[] triangles =
            new int[segmentCount * ringCount * 6];

        for (int r = 0; r <= ringCount; r++)
        {
            float radialT = (float)r / ringCount;

            float radius = Mathf.Lerp(
                innerRadius,
                outerRadius,
                radialT
            );

            for (int s = 0; s <= segmentCount; s++)
            {
                float angularT = (float)s / segmentCount;

                float angle = angularT * Mathf.PI * 2f;

                float x = holeCenter.x + Mathf.Cos(angle) * radius;
                float z = holeCenter.z + Mathf.Sin(angle) * radius;

                float y = SampleSurfaceHeight(x, z) + offset;

                int index = r * (segmentCount + 1) + s;

                vertices[index] = new Vector3(x, y, z);
                uvs[index] = new Vector2(angularT, radialT);
            }
        }

        int triangleIndex = 0;

        for (int r = 0; r < ringCount; r++)
        {
            for (int s = 0; s < segmentCount; s++)
            {
                int a = r * (segmentCount + 1) + s;
                int b = a + 1;
                int c = a + segmentCount + 1;
                int d = c + 1;

                triangles[triangleIndex++] = a;
                triangles[triangleIndex++] = b;
                triangles[triangleIndex++] = c;

                triangles[triangleIndex++] = b;
                triangles[triangleIndex++] = d;
                triangles[triangleIndex++] = c;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "Accretion Ring Mesh";

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private float SampleSurfaceHeight(float x, float z)
    {
        // Interpolate heights from the existing curved mesh.
        // This avoids creating a second deformation formula.

        float u = Mathf.Clamp01(x / grid.width + 0.5f);
        float v = Mathf.Clamp01(z / grid.length + 0.5f);

        float gx = u * grid.meshColumns;
        float gz = v * grid.meshRows;

        int x0 = Mathf.Min(
            Mathf.FloorToInt(gx),
            grid.meshColumns - 1
        );

        int z0 = Mathf.Min(
            Mathf.FloorToInt(gz),
            grid.meshRows - 1
        );

        float tx = gx - x0;
        float tz = gz - z0;

        int stride = grid.meshColumns + 1;

        float h00 = surfaceVertices[z0 * stride + x0].y;
        float h10 = surfaceVertices[z0 * stride + x0 + 1].y;

        float h01 =
            surfaceVertices[(z0 + 1) * stride + x0].y;

        float h11 =
            surfaceVertices[(z0 + 1) * stride + x0 + 1].y;

        float bottom = Mathf.Lerp(h00, h10, tx);
        float top = Mathf.Lerp(h01, h11, tx);

        return Mathf.Lerp(bottom, top, tz);
    }

    private Texture2D BuildRingTexture(
        Color darkerColor,
        Color brighterColor,
        float opacity,
        int layer)
    {
        const int textureWidth = 512;
        const int textureHeight = 64;

        Texture2D texture = new Texture2D(
            textureWidth,
            textureHeight,
            TextureFormat.RGBA32,
            true
        );

        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        texture.anisoLevel = 8;

        Color[] pixels =
            new Color[textureWidth * textureHeight];

        for (int y = 0; y < textureHeight; y++)
        {
            float v = (float)y / (textureHeight - 1);

            // Fade toward both radial edges.
            float innerFade = Mathf.SmoothStep(
                0f, 1f, Mathf.Clamp01(v / 0.18f)
            );

            float outerFade = Mathf.SmoothStep(
                0f, 1f, Mathf.Clamp01((1f - v) / 0.22f)
            );

            float fade = innerFade * outerFade;

            for (int x = 0; x < textureWidth; x++)
            {
                float u = (float)x / textureWidth;
                float angle = u * Mathf.PI * 2f;

                // Periodic patterns create moving luminous streaks
                // when the texture scrolls around the ring.
                float wave1 = Mathf.Sin(
                    angle * 11f + layer * 0.7f
                );

                float wave2 = Mathf.Sin(
                    angle * 23f + layer * 1.4f
                );

                float highlight = Mathf.Pow(
                    Mathf.Max(
                        0f,
                        Mathf.Sin(angle * 7f + layer * 1.3f)
                    ),
                    10f
                );

                float brightness = Mathf.Clamp01(
                    0.58f +
                    0.22f * wave1 +
                    0.13f * wave2 +
                    0.36f * highlight
                );

                Color color = Color.Lerp(
                    darkerColor,
                    brighterColor,
                    brightness
                );

                color.a = fade * opacity *
                    Mathf.Lerp(0.5f, 1f, brightness);

                pixels[y * textureWidth + x] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(true, true);

        return texture;
    }

    private void Update()
    {
        // The geometry stays fixed on the funnel.
        // Only the luminous patterns orbit.
        for (int i = 0; i < materials.Count; i++)
        {
            float offset = Mathf.Repeat(
                Time.time * layerSpeeds[i] * orbitSpeed,
                1f
            );

            materials[i].SetTextureOffset(
                "_BaseMap",
                new Vector2(offset, 0f)
            );
        }
    }

    private void OnDestroy()
    {
        foreach (GameObject obj in objects)
            if (obj != null) Destroy(obj);

        foreach (Mesh mesh in meshes)
            if (mesh != null) Destroy(mesh);

        foreach (Material material in materials)
            if (material != null) Destroy(material);

        foreach (Texture2D texture in textures)
            if (texture != null) Destroy(texture);
    }
}
