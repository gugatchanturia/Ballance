
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GravityGrid))]
public class GravityGridEnergy : MonoBehaviour
{
    [Header("Particle Counts")]
    [Range(0, 150)]
    public int ambientSparks = 30;

    [Range(0, 50)]
    public int sparksPerBlackHole = 18;

    [Header("Normal Grid Movement")]
    public float ambientSpeed = 1.1f;

    [Header("Black Hole Attraction")]
    [Tooltip("Distance from a hole's center where normal sparks become trapped.")]
    public float pullRadius = 2.5f;

    [Tooltip("Sparks vanish when reaching this distance from the center.")]
    public float vanishRadius = 0.7f;

    [Tooltip("Speed of sparks entering a black hole.")]
    public float suctionSpeed = 1.35f;

    [Tooltip("How much faster sparks move near the opening.")]
    public float finalSpeedMultiplier = 2f;

    [Range(0f, 1.5f)]
    [Tooltip("Number of revolutions while spiraling into the hole.")]
    public float spiralTurns = 0.35f;

    [Header("Appearance")]
    public float sparkLength = 0.17f;
    public float sparkWidth = 0.055f;
    public float surfaceOffset = 0.075f;

    public Color ambientColor =
        new Color(0.48f, 0.95f, 1f);

    public Color suctionColor =
        new Color(1f, 0.90f, 0.40f);

    public Color dangerColor =
        new Color(1f, 0.38f, 0.25f);

    private class Spark
    {
        public Transform visual;
        public MeshRenderer renderer;

        public Vector2 position;
        public Vector2 direction;

        public float speed;
        public float age;
        public float respawnTimer;

        // -1 means an ordinary ambient particle.
        // Any other value is a dedicated black-hole stream.
        public int preferredHole;

        // A captured particle cannot return to ambient movement.
        public bool captured;
        public int targetHole = -1;

        public float startingRadius;
        public float rotationSign;
    }

    private class HoleInfo
    {
        public Vector2 center;
        public float openingRadius;
    }

    private readonly List<Spark> sparks =
        new List<Spark>();

    private readonly List<HoleInfo> holes =
        new List<HoleInfo>();

    private readonly List<Material> materials =
        new List<Material>();

    private GravityGrid grid;
    private Vector3[] surfaceVertices;

    private Mesh sparkMesh;

    private Material ambientMaterial;
    private Material suctionMaterial;
    private Material dangerMaterial;

    private IEnumerator Start()
    {
        // Let GravityGrid finish generating its surface.
        yield return null;
        yield return null;

        grid = GetComponent<GravityGrid>();

        MeshFilter gridFilter =
            GetComponent<MeshFilter>();

        if (gridFilter.sharedMesh == null)
        {
            Debug.LogError(
                "GravityGridEnergy: GravityGrid mesh missing.",
                this
            );

            yield break;
        }

        surfaceVertices = gridFilter.sharedMesh.vertices;

        // Cache every active black hole in GRID coordinates.
        BlackHole[] foundHoles = transform.parent
            .GetComponentsInChildren<BlackHole>();

        foreach (BlackHole hole in foundHoles)
        {
            if (hole == null || !hole.isActiveAndEnabled)
                continue;

            Vector3 local =
                transform.InverseTransformPoint(
                    hole.transform.position
                );

            holes.Add(new HoleInfo
            {
                center = new Vector2(local.x, local.z),
                openingRadius = Mathf.Max(
                    vanishRadius,
                    hole.captureRadius
                )
            });
        }

        Shader shader = Shader.Find(
            "Universal Render Pipeline/Unlit"
        );

        if (shader == null)
        {
            Debug.LogError(
                "GravityGridEnergy: URP Unlit shader missing.",
                this
            );

            yield break;
        }

        ambientMaterial =
            CreateMaterial(shader, ambientColor);

        suctionMaterial =
            CreateMaterial(shader, suctionColor);

        dangerMaterial =
            CreateMaterial(shader, dangerColor);

        // All sparks share one cube mesh.
        GameObject temp =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        sparkMesh =
            temp.GetComponent<MeshFilter>().sharedMesh;

        temp.SetActive(false);
        Destroy(temp);

        // Ordinary grid particles.
        for (int i = 0; i < ambientSparks; i++)
            CreateSpark(-1);

        // Constant inward-flowing particles for each hole.
        for (int h = 0; h < holes.Count; h++)
        {
            for (int i = 0; i < sparksPerBlackHole; i++)
                CreateSpark(h);
        }

        Debug.Log(
            "GravityGridEnergy: Created " +
            sparks.Count + " sparks for " +
            holes.Count + " black holes."
        );
    }

    private Material CreateMaterial(
        Shader shader,
        Color color)
    {
        Material material = new Material(shader);
        material.SetColor("_BaseColor", color);

        materials.Add(material);
        return material;
    }

    private void CreateSpark(int preferredHole)
    {
        GameObject obj =
            new GameObject("Grid Energy Spark");

        obj.transform.SetParent(transform, false);

        MeshFilter filter =
            obj.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            obj.AddComponent<MeshRenderer>();

        filter.sharedMesh = sparkMesh;
        renderer.sharedMaterial = ambientMaterial;

        Spark spark = new Spark
        {
            visual = obj.transform,
            renderer = renderer,
            preferredHole = preferredHole,
            speed = Random.Range(0.8f, 1.2f)
        };

        sparks.Add(spark);

        Respawn(spark);
    }

    private bool InsideGrid(Vector2 point)
    {
        return Mathf.Abs(point.x) <
                   grid.width * 0.49f &&
               Mathf.Abs(point.y) <
                   grid.length * 0.49f;
    }

    private float PullRadius(int index)
    {
        return Mathf.Max(
            pullRadius,
            holes[index].openingRadius + 0.4f
        );
    }

    private bool SafeAmbientSpawn(Vector2 point)
    {
        if (!InsideGrid(point))
            return false;

        foreach (HoleInfo hole in holes)
        {
            if (Vector2.Distance(point, hole.center) <
                Mathf.Max(
                    pullRadius,
                    hole.openingRadius + 0.4f
                ) + 0.1f)
            {
                return false;
            }
        }

        return true;
    }

    private bool SafeStreamSpawn(Vector2 point, int target)
    {
        if (!InsideGrid(point))
            return false;

        for (int i = 0; i < holes.Count; i++)
        {
            if (i == target)
                continue;

            if (Vector2.Distance(point, holes[i].center) <
                holes[i].openingRadius + 0.3f)
            {
                return false;
            }
        }

        return true;
    }

    private void Respawn(Spark spark)
    {
        spark.captured = false;
        spark.targetHole = -1;
        spark.age = 0f;

        spark.renderer.sharedMaterial = ambientMaterial;

        bool spawned = spark.preferredHole < 0
            ? SpawnAmbient(spark)
            : SpawnNearHole(spark, spark.preferredHole);

        if (!spawned)
        {
            spark.visual.gameObject.SetActive(false);
            spark.respawnTimer = 0.25f;
            return;
        }

        spark.visual.gameObject.SetActive(true);

        // Begin invisible, then fade in by growing.
        spark.visual.localScale = Vector3.zero;
    }

    private bool SpawnAmbient(Spark spark)
    {
        for (int attempt = 0; attempt < 120; attempt++)
        {
            Vector2 position = new Vector2(
                Random.Range(
                    -grid.width * 0.48f,
                    grid.width * 0.48f
                ),
                Random.Range(
                    -grid.length * 0.48f,
                    grid.length * 0.48f
                )
            );

            bool horizontal = Random.value < 0.5f;

            float sign = Random.value < 0.5f
                ? -1f
                : 1f;

            if (horizontal)
            {
                float spacing =
                    grid.length / grid.rows;

                float origin = -grid.length * 0.5f;

                position.y = origin +
                    Mathf.Round(
                        (position.y - origin) / spacing
                    ) * spacing;

                spark.direction =
                    new Vector2(sign, 0f);
            }
            else
            {
                float spacing =
                    grid.width / grid.columns;

                float origin = -grid.width * 0.5f;

                position.x = origin +
                    Mathf.Round(
                        (position.x - origin) / spacing
                    ) * spacing;

                spark.direction =
                    new Vector2(0f, sign);
            }

            if (!SafeAmbientSpawn(position))
                continue;

            spark.position = position;
            spark.speed = Random.Range(0.8f, 1.2f);

            PlaceSpark(spark, spark.direction);
            return true;
        }

        return false;
    }

    private bool SpawnNearHole(Spark spark, int index)
    {
        if (index < 0 || index >= holes.Count)
            return false;

        HoleInfo hole = holes[index];

        float outerRadius = PullRadius(index);

        // Always spawn in the OUTER funnel.
        // Never inside the black opening.
        float minimumRadius = Mathf.Max(
            hole.openingRadius + 0.65f,
            outerRadius * 0.65f
        );

        float maximumRadius = Mathf.Max(
            minimumRadius + 0.2f,
            outerRadius * 0.95f
        );

        for (int attempt = 0; attempt < 100; attempt++)
        {
            float angle =
                Random.Range(0f, Mathf.PI * 2f);

            float radius = Random.Range(
                minimumRadius,
                maximumRadius
            );

            Vector2 offset = new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            ) * radius;

            Vector2 position = hole.center + offset;

            if (!SafeStreamSpawn(position, index))
                continue;

            spark.position = position;

            // Dedicated streams start captured.
            // They can ONLY move toward the center.
            BeginCapture(spark, index);

            spark.renderer.sharedMaterial =
                suctionMaterial;

            Vector2 inward =
                (hole.center - position).normalized;

            PlaceSpark(spark, inward);

            return true;
        }

        return false;
    }

    private void BeginCapture(Spark spark, int index)
    {
        spark.captured = true;
        spark.targetHole = index;

        spark.startingRadius = Mathf.Max(
            Vector2.Distance(
                spark.position,
                holes[index].center
            ),
            holes[index].openingRadius + 0.1f
        );

        spark.rotationSign = Random.value < 0.5f
            ? -1f
            : 1f;
    }

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

        int i = z0 * stride + x0;

        float bottom = Mathf.Lerp(
            surfaceVertices[i].y,
            surfaceVertices[i + 1].y,
            tx
        );

        float top = Mathf.Lerp(
            surfaceVertices[i + stride].y,
            surfaceVertices[i + stride + 1].y,
            tx
        );

        return Mathf.Lerp(bottom, top, tz);
    }

    private void PlaceSpark(
        Spark spark,
        Vector2 direction)
    {
        float y = SurfaceHeight(
            spark.position.x,
            spark.position.y
        );

        spark.visual.localPosition = new Vector3(
            spark.position.x,
            y + surfaceOffset,
            spark.position.y
        );

        Vector3 forward = new Vector3(
            direction.x,
            0f,
            direction.y
        );

        if (forward.sqrMagnitude > 0.00001f)
        {
            spark.visual.localRotation =
                Quaternion.LookRotation(
                    forward.normalized,
                    Vector3.up
                );
        }

        // New sparks gradually appear.
        // Captured sparks shrink near the horizon.
        float fadeIn = Mathf.Clamp01(
            spark.age / 0.15f
        );

        float shrink = 1f;

        if (spark.captured &&
            spark.targetHole >= 0)
        {
            HoleInfo hole = holes[spark.targetHole];

            float distance = Vector2.Distance(
                spark.position,
                hole.center
            );

            float progress = Mathf.InverseLerp(
                spark.startingRadius,
                hole.openingRadius,
                distance
            );

            shrink = Mathf.Lerp(
                1.15f,
                0.3f,
                progress
            );
        }

        float scale = fadeIn * shrink;

        spark.visual.localScale = new Vector3(
            sparkWidth * scale,
            0.022f * scale,
            sparkLength * scale
        );
    }

    private void Update()
    {
        if (grid == null || surfaceVertices == null)
            return;

        float dt = Time.deltaTime;

        foreach (Spark spark in sparks)
        {
            if (!spark.visual.gameObject.activeSelf)
            {
                spark.respawnTimer -= dt;

                if (spark.respawnTimer <= 0f)
                    Respawn(spark);

                continue;
            }

            spark.age += dt;

            if (spark.captured)
                UpdateCaptured(spark, dt);
            else
                UpdateAmbient(spark, dt);
        }
    }

    private void UpdateAmbient(Spark spark, float dt)
    {
        Vector2 previous = spark.position;

        Vector2 next = previous +
            spark.direction *
            ambientSpeed * spark.speed * dt;

        if (!InsideGrid(next))
        {
            Disappear(spark);
            return;
        }

        // Test the whole movement segment against
        // every black hole's attraction zone.
        // This also prevents high-speed tunneling.
        for (int i = 0; i < holes.Count; i++)
        {
            Vector2 center = holes[i].center;

            Vector2 delta = next - previous;

            float t = delta.sqrMagnitude > 0.000001f
                ? Mathf.Clamp01(
                    Vector2.Dot(
                        center - previous,
                        delta
                    ) / delta.sqrMagnitude
                )
                : 0f;

            Vector2 nearestPoint =
                previous + delta * t;

            float distance = Vector2.Distance(
                nearestPoint,
                center
            );

            if (distance <= PullRadius(i))
            {
                spark.position = nearestPoint;

                BeginCapture(spark, i);

                UpdateCaptured(spark, dt);
                return;
            }
        }

        // Outside every capture zone:
        // preserve the original straight-line movement.
        spark.position = next;

        spark.renderer.sharedMaterial =
            ambientMaterial;

        PlaceSpark(spark, spark.direction);
    }

    private void UpdateCaptured(Spark spark, float dt)
    {
        int index = spark.targetHole;

        if (index < 0 || index >= holes.Count)
        {
            Disappear(spark);
            return;
        }

        HoleInfo hole = holes[index];

        Vector2 before = spark.position;
        Vector2 offset = before - hole.center;

        float radius = offset.magnitude;

        // This is the actual disappearance boundary.
        float horizon = hole.openingRadius;

        if (radius <= horizon + 0.025f)
        {
            Disappear(spark);
            return;
        }

        float totalDistance = Mathf.Max(
            0.1f,
            spark.startingRadius - horizon
        );

        float progress = Mathf.Clamp01(
            (spark.startingRadius - radius) /
            totalDistance
        );

        // Speed increases as the particle descends.
        float speed = suctionSpeed *
            spark.speed *
            Mathf.Lerp(
                1f,
                finalSpeedMultiplier,
                progress
            );

        // Radial distance MUST decrease every frame.
        float nextRadius = Mathf.Max(
            0f,
            radius - speed * dt
        );

        if (nextRadius <= horizon + 0.025f)
        {
            Disappear(spark);
            return;
        }

        // Add a visible spiral to the inward motion.
        // This cannot make the particle escape.
        float fraction =
            (radius - nextRadius) / totalDistance;

        float angle = Mathf.Atan2(
            offset.y,
            offset.x
        );

        angle +=
            fraction *
            spiralTurns *
            Mathf.PI * 2f *
            spark.rotationSign;

        Vector2 next = hole.center +
            new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            ) * nextRadius;

        spark.position = next;

        Vector2 movement = next - before;

        float remainingProgress =
            Mathf.Clamp01(
                (spark.startingRadius - nextRadius) /
                totalDistance
            );

        spark.renderer.sharedMaterial =
            remainingProgress > 0.6f
                ? dangerMaterial
                : suctionMaterial;

        PlaceSpark(spark, movement);
    }

    private void Disappear(Spark spark)
    {
        // Hide immediately. Never show the particle
        // traveling OUT of the black hole.
        spark.visual.gameObject.SetActive(false);

        // A new particle appears elsewhere shortly afterward.
        spark.respawnTimer =
            Random.Range(0.1f, 0.45f);
    }

    private void OnDestroy()
    {
        foreach (Material material in materials)
        {
            if (material != null)
                Destroy(material);
        }
    }
}
