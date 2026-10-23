
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(GravityGrid))]
public class BlackHoleHorizon : MonoBehaviour
{
    [Header("Event Horizon")]
    [Tooltip("Visual radius of the black opening. Does not affect physics.")]
    public float mouthRadius = 0.76f;

    [Tooltip("Keep the visuals slightly above the gravity mesh.")]
    public float surfaceOffset = 0.075f;

    public Color darknessColor =
        new Color(0.015f, 0.025f, 0.055f);

    [Header("Thin Warning Rim")]
    public float rimWidth = 0.035f;

    public Color rimColor =
        new Color(0.72f, 0.20f, 0.24f);

    [Header("Orbiting Highlights")]
    public Color highlightColor =
        new Color(1f, 0.65f, 0.30f);

    public float highlightWidth = 0.055f;
    public float highlightSpeed = 35f;

    [Range(5f, 60f)]
    public float highlightArc = 24f;

    private class Horizon
    {
        public Vector2 center;
        public float angle;
        public LineRenderer firstHighlight;
        public LineRenderer secondHighlight;
    }

    private readonly List<Horizon> horizons =
        new List<Horizon>();

    private readonly List<Mesh> generatedMeshes =
        new List<Mesh>();

    private readonly List<Material> generatedMaterials =
        new List<Material>();

    private GravityGrid grid;
    private Vector3[] surfaceVertices;

    private Material darknessMaterial;
    private Material rimMaterial;
    private Material highlightMaterial;

    private IEnumerator Start()
    {
        // Wait for GravityGrid to generate its curved mesh.
        yield return null;
        yield return null;

        grid = GetComponent<GravityGrid>();

        MeshFilter gridFilter = GetComponent<MeshFilter>();

        if (gridFilter.sharedMesh == null)
        {
            Debug.LogError(
                "BlackHoleHorizon: Gravity grid mesh missing.",
                this
            );

            yield break;
        }

        surfaceVertices = gridFilter.sharedMesh.vertices;

        Shader shader = Shader.Find(
            "Universal Render Pipeline/Unlit"
        );

        if (shader == null)
        {
            Debug.LogError(
                "BlackHoleHorizon: URP Unlit shader missing.",
                this
            );

            yield break;
        }

        darknessMaterial =
            CreateMaterial(shader, darknessColor);

        rimMaterial =
            CreateMaterial(shader, rimColor);

        highlightMaterial =
            CreateMaterial(shader, highlightColor);

        // Find even disabled BlackHole scripts,
        // since their scripted gravitational forces
        // are disabled in this game.
        BlackHole[] holes = FindObjectsByType<BlackHole>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        int count = 0;

        foreach (BlackHole hole in holes)
        {
            if (hole == null ||
                !hole.gameObject.activeInHierarchy ||
                hole.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            Vector3 localPosition =
                transform.InverseTransformPoint(
                    hole.transform.position
                );

            Vector2 center = new Vector2(
                localPosition.x,
                localPosition.z
            );

            CreateHorizon(center);
            count++;
        }

        Debug.Log(
            "BlackHoleHorizon: Created " +
            count + " event horizons.",
            this
        );
    }

    private Material CreateMaterial(
        Shader shader,
        Color color)
    {
        Material material = new Material(shader);
        material.SetColor("_BaseColor", color);

        generatedMaterials.Add(material);
        return material;
    }

    // Sample the triangles of the existing
    // gravity surface. Nothing here modifies the mesh.
    private float SurfaceHeight(float x, float z)
    {
        float u = Mathf.Clamp01(
            x / grid.width + 0.5f
        );

        float v = Mathf.Clamp01(
            z / grid.length + 0.5f
        );

        float gx = u * grid.meshColumns;
        float gz = v * grid.meshRows;

        int ix = Mathf.Min(
            Mathf.FloorToInt(gx),
            grid.meshColumns - 1
        );

        int iz = Mathf.Min(
            Mathf.FloorToInt(gz),
            grid.meshRows - 1
        );

        float tx = gx - ix;
        float tz = gz - iz;

        int stride = grid.meshColumns + 1;

        int a = iz * stride + ix;
        int b = a + 1;
        int c = a + stride;
        int d = c + 1;

        float ha = surfaceVertices[a].y;
        float hb = surfaceVertices[b].y;
        float hc = surfaceVertices[c].y;
        float hd = surfaceVertices[d].y;

        if (tz >= tx)
        {
            return
                (1f - tz) * ha +
                (tz - tx) * hc +
                tx * hd;
        }

        return
            (1f - tx) * ha +
            (tx - tz) * hb +
            tz * hd;
    }

    private Vector3 CirclePoint(
        Vector2 center,
        float radius,
        float angle,
        float extraHeight = 0f)
    {
        float radians = angle * Mathf.Deg2Rad;

        float x = center.x +
            Mathf.Cos(radians) * radius;

        float z = center.y +
            Mathf.Sin(radians) * radius;

        float y = SurfaceHeight(x, z) +
            surfaceOffset + extraHeight;

        return new Vector3(x, y, z);
    }

    private void CreateHorizon(Vector2 center)
    {
        // The dark surface hides the jagged visual
        // edges of the existing opening.
        CreateDarkMouth(center);

        float radius = Mathf.Max(0.1f, mouthRadius);

        // A continuous, very thin rim.
        LineRenderer rim = CreateLine(
            "Event Horizon Rim",
            rimMaterial,
            rimWidth,
            128,
            true
        );

        for (int i = 0; i < 128; i++)
        {
            float angle = i * 360f / 128f;

            rim.SetPosition(
                i,
                CirclePoint(center, radius, angle, 0.025f)
            );
        }

        // Two short highlights rotating along the rim.
        LineRenderer first = CreateLine(
            "Horizon Highlight A",
            highlightMaterial,
            highlightWidth,
            12,
            false
        );

        LineRenderer second = CreateLine(
            "Horizon Highlight B",
            highlightMaterial,
            highlightWidth,
            12,
            false
        );

        horizons.Add(new Horizon
        {
            center = center,
            angle = Random.Range(0f, 360f),
            firstHighlight = first,
            secondHighlight = second
        });
    }

    // Build an opaque, smooth circular visual surface.
    // It follows the curved mesh and has NO collider.
    private void CreateDarkMouth(Vector2 center)
    {
        const int segments = 128;
        const int radialRings = 8;

        int stride = segments + 1;

        Vector3[] vertices = new Vector3[
            (radialRings + 1) * stride
        ];

        int[] triangles = new int[
            radialRings * segments * 6
        ];

        float radius =
            Mathf.Max(0.1f, mouthRadius);

        for (int r = 0; r <= radialRings; r++)
        {
            float t = (float)r / radialRings;
            float currentRadius = radius * t;

            for (int s = 0; s <= segments; s++)
            {
                float angle =
                    (float)s / segments * 360f;

                vertices[r * stride + s] =
                    CirclePoint(
                        center,
                        currentRadius,
                        angle
                    );
            }
        }

        int index = 0;

        for (int r = 0; r < radialRings; r++)
        {
            for (int s = 0; s < segments; s++)
            {
                int a = r * stride + s;
                int b = a + 1;
                int c = a + stride;
                int d = c + 1;

                // Upward-facing triangles.
                triangles[index++] = a;
                triangles[index++] = d;
                triangles[index++] = c;

                triangles[index++] = a;
                triangles[index++] = b;
                triangles[index++] = d;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "Smooth Event Horizon";

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        generatedMeshes.Add(mesh);

        GameObject obj = new GameObject(
            "Dark Event Horizon"
        );

        obj.transform.SetParent(transform, false);

        MeshFilter filter =
            obj.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            obj.AddComponent<MeshRenderer>();

        filter.sharedMesh = mesh;
        renderer.sharedMaterial = darknessMaterial;

        renderer.shadowCastingMode =
            ShadowCastingMode.Off;

        renderer.receiveShadows = false;
    }

    private LineRenderer CreateLine(
        string objectName,
        Material material,
        float width,
        int points,
        bool closed)
    {
        GameObject obj = new GameObject(objectName);

        obj.transform.SetParent(transform, false);

        LineRenderer line =
            obj.AddComponent<LineRenderer>();

        line.useWorldSpace = false;
        line.positionCount = points;
        line.loop = closed;

        line.widthMultiplier = width;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;

        line.sharedMaterial = material;

        line.shadowCastingMode =
            ShadowCastingMode.Off;

        line.receiveShadows = false;

        return line;
    }

    private void DrawHighlight(
        LineRenderer line,
        Vector2 center,
        float startAngle)
    {
        int points = line.positionCount;

        for (int i = 0; i < points; i++)
        {
            float t = (float)i / (points - 1);

            float angle =
                startAngle + t * highlightArc;

            line.SetPosition(
                i,
                CirclePoint(
                    center,
                    mouthRadius,
                    angle,
                    0.045f
                )
            );
        }
    }

    private void Update()
    {
        if (surfaceVertices == null)
            return;

        foreach (Horizon horizon in horizons)
        {
            horizon.angle +=
                highlightSpeed * Time.deltaTime;

            DrawHighlight(
                horizon.firstHighlight,
                horizon.center,
                horizon.angle
            );

            DrawHighlight(
                horizon.secondHighlight,
                horizon.center,
                horizon.angle + 180f
            );
        }
    }

    private void OnDestroy()
    {
        foreach (Mesh mesh in generatedMeshes)
        {
            if (mesh != null)
                Destroy(mesh);
        }

        foreach (Material material in generatedMaterials)
        {
            if (material != null)
                Destroy(material);
        }
    }
}
