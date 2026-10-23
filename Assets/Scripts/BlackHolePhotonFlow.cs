
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(GravityGrid))]
[RequireComponent(typeof(MeshFilter))]
public class BlackHolePhotonFlow : MonoBehaviour
{
    [Header("Particle Counts")]
    [Range(0, 100)]
    public int backgroundPhotons = 24;

    [Range(0, 60)]
    public int photonsPerHole = 26;

    [Header("Normal Grid Movement")]
    public float backgroundSpeed = 1.1f;

    [Header("Black Hole Absorption")]
    [Tooltip("Normal photons become trapped inside this radius.")]
    public float attractionRadius = 2.5f;

    [Tooltip("Where dedicated photon streams begin.")]
    public float streamStartRadius = 2.3f;

    [Tooltip("Where photons disappear. Match the visible opening.")]
    public float eventHorizonRadius = 0.72f;

    [Tooltip("Approximate duration of one inward spiral.")]
    public float absorptionSeconds = 1.8f;

    [Tooltip("Full revolutions during absorption.")]
    [Range(0f, 2f)]
    public float spiralRevolutions = 0.65f;

    [Header("Particle Appearance")]
    public float photonWidth = 0.075f;
    public float photonLength = 0.22f;

    [Tooltip("Height above the actual curved mesh.")]
    public float heightAboveGrid = 0.12f;

    public Color backgroundColor =
        new Color(0.45f, 1f, 1f);

    public Color outerFlowColor =
        new Color(1f, 0.92f, 0.52f);

    public Color innerFlowColor =
        new Color(1f, 0.38f, 0.20f);

    private enum PhotonMode
    {
        Background,
        Absorbing,
        Hidden
    }

    private class Photon
    {
        public Transform transform;
        public MeshRenderer renderer;

        // -1: ordinary grid particle.
        // 0 or greater: dedicated to a black hole.
        public int homeHole;

        public PhotonMode mode;

        public Vector2 position;
        public Vector2 direction;

        public int targetHole;

        public float startingRadius;
        public float startingAngle;
        public float rotationSign;

        public float elapsed;
        public float duration;
        public float speed;

        public float hiddenTime;
    }

    private GravityGrid grid;
    private Vector3[] meshVertices;
    private Mesh photonMesh;

    private Material backgroundMaterial;
    private Material outerMaterial;
    private Material innerMaterial;

    private readonly List<Vector2> holeCenters =
        new List<Vector2>();

    private readonly List<Photon> photons =
        new List<Photon>();

    private IEnumerator Start()
    {
        // GravityGrid builds its visual mesh during Start.
        yield return null;

        grid = GetComponent<GravityGrid>();

        MeshFilter gridFilter =
            GetComponent<MeshFilter>();

        Mesh surface = gridFilter.sharedMesh;

        if (surface == null)
        {
            Debug.LogError(
                "BlackHolePhotonFlow: Gravity grid has no mesh.",
                this
            );

            yield break;
        }

        meshVertices = surface.vertices;

        // Locate black holes in the SAME coordinate
        // system used by GravityGrid's mesh.
        BlackHole[] foundHoles =
            FindObjectsByType<BlackHole>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (BlackHole hole in foundHoles)
        {
            if (hole == null || !hole.gameObject.activeInHierarchy)
                continue;

            Vector3 localPosition =
                transform.InverseTransformPoint(
                    hole.transform.position
                );

            holeCenters.Add(
                new Vector2(
                    localPosition.x,
                    localPosition.z
                )
            );
        }

        Shader shader = Shader.Find(
            "Universal Render Pipeline/Unlit"
        );

        if (shader == null)
        {
            Debug.LogError(
                "BlackHolePhotonFlow: URP Unlit shader missing.",
                this
            );

            yield break;
        }

        backgroundMaterial =
            CreateMaterial(shader, backgroundColor);

        outerMaterial =
            CreateMaterial(shader, outerFlowColor);

        innerMaterial =
            CreateMaterial(shader, innerFlowColor);

        GameObject temporaryCube =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        photonMesh =
            temporaryCube.GetComponent<MeshFilter>().sharedMesh;

        Destroy(temporaryCube);

        // Normal, grid-following particles.
        for (int i = 0; i < backgroundPhotons; i++)
        {
            Photon photon = CreatePhoton(-1);
            SpawnBackground(photon);
        }

        // Independent, permanent inward streams.
        // They do NOT rely on a normal particle
        // accidentally approaching the hole.
        for (int holeIndex = 0;
             holeIndex < holeCenters.Count;
             holeIndex++)
        {
            for (int i = 0; i < photonsPerHole; i++)
            {
                Photon photon = CreatePhoton(holeIndex);

                // Spread particles across the spiral
                // so every hole is draining continuously
                // from its very first frame.
                float phase =
                    (i + Random.Range(0f, 0.7f)) /
                    Mathf.Max(1, photonsPerHole);

                SpawnIntoStream(
                    photon,
                    holeIndex,
                    phase
                );
            }
        }

        Debug.Log(
            "BlackHolePhotonFlow: Found " +
            holeCenters.Count +
            " black holes; created " +
            photons.Count +
            " photons.",
            this
        );

        if (holeCenters.Count == 0)
        {
            Debug.LogWarning(
                "BlackHolePhotonFlow: No black holes found.",
                this
            );
        }
    }

    private Material CreateMaterial(
        Shader shader,
        Color color)
    {
        Material material = new Material(shader);

        material.SetColor("_BaseColor", color);

        return material;
    }

    private Photon CreatePhoton(int homeHole)
    {
        GameObject obj =
            new GameObject("Photon");

        obj.transform.SetParent(transform, false);

        MeshFilter filter =
            obj.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            obj.AddComponent<MeshRenderer>();

        filter.sharedMesh = photonMesh;
        renderer.sharedMaterial = backgroundMaterial;

        renderer.shadowCastingMode =
            ShadowCastingMode.Off;

        renderer.receiveShadows = false;

        Photon photon = new Photon
        {
            transform = obj.transform,
            renderer = renderer,
            homeHole = homeHole,
            mode = PhotonMode.Hidden,
            speed = Random.Range(0.85f, 1.15f)
        };

        photons.Add(photon);

        return photon;
    }

    private bool WithinGrid(Vector2 position)
    {
        return Mathf.Abs(position.x) <
                   grid.width * 0.49f &&
               Mathf.Abs(position.y) <
                   grid.length * 0.49f;
    }

    private bool SafeBackgroundPosition(
        Vector2 position)
    {
        if (!WithinGrid(position))
            return false;

        // Never create new photons inside a
        // black hole's attraction zone.
        foreach (Vector2 center in holeCenters)
        {
            if (Vector2.Distance(position, center) <
                attractionRadius + 0.25f)
            {
                return false;
            }
        }

        return true;
    }

    private void HidePhoton(Photon photon)
    {
        photon.mode = PhotonMode.Hidden;
        photon.renderer.enabled = false;

        photon.hiddenTime =
            Random.Range(0.08f, 0.25f);
    }

    private void SpawnBackground(Photon photon)
    {
        // Randomly select a horizontal or vertical
        // grid line and a travel direction.
        for (int attempt = 0; attempt < 120; attempt++)
        {
            bool horizontal =
                Random.value < 0.5f;

            float sign =
                Random.value < 0.5f ? -1f : 1f;

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

            if (horizontal)
            {
                float spacing =
                    grid.length / grid.rows;

                float origin =
                    -grid.length * 0.5f;

                position.y = origin +
                    Mathf.Round(
                        (position.y - origin) / spacing
                    ) * spacing;
            }
            else
            {
                float spacing =
                    grid.width / grid.columns;

                float origin =
                    -grid.width * 0.5f;

                position.x = origin +
                    Mathf.Round(
                        (position.x - origin) / spacing
                    ) * spacing;
            }

            if (!SafeBackgroundPosition(position))
                continue;

            photon.position = position;

            photon.direction = horizontal
                ? new Vector2(sign, 0f)
                : new Vector2(0f, sign);

            photon.speed =
                Random.Range(0.85f, 1.15f);

            photon.mode = PhotonMode.Background;

            photon.renderer.enabled = true;
            photon.renderer.sharedMaterial =
                backgroundMaterial;

            PositionVisual(
                photon,
                photon.direction,
                0.8f
            );

            return;
        }

        HidePhoton(photon);
    }

    // Create a particle on the OUTSIDE of the well
    // and immediately assign it to inward motion.
    private void SpawnIntoStream(
        Photon photon,
        int holeIndex,
        float initialProgress)
    {
        if (holeIndex < 0 ||
            holeIndex >= holeCenters.Count)
        {
            HidePhoton(photon);
            return;
        }

        Vector2 center = holeCenters[holeIndex];

        float minimumRadius = Mathf.Max(
            eventHorizonRadius + 0.6f,
            streamStartRadius * 0.85f
        );

        float maximumRadius = Mathf.Max(
            minimumRadius + 0.1f,
            streamStartRadius * 1.05f
        );

        bool found = false;

        for (int attempt = 0; attempt < 100; attempt++)
        {
            float angle =
                Random.Range(0f, Mathf.PI * 2f);

            float radius =
                Random.Range(
                    minimumRadius,
                    maximumRadius
                );

            Vector2 candidate = center +
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                ) * radius;

            if (!WithinGrid(candidate))
                continue;

            photon.startingAngle = angle;
            photon.startingRadius = radius;
            found = true;

            break;
        }

        if (!found)
        {
            HidePhoton(photon);
            return;
        }

        photon.targetHole = holeIndex;

        photon.rotationSign =
            holeIndex % 2 == 0 ? 1f : -1f;

        photon.duration =
            Mathf.Max(
                0.2f,
                absorptionSeconds *
                Random.Range(0.85f, 1.15f)
            );

        photon.elapsed =
            photon.duration *
            Mathf.Clamp01(initialProgress);

        photon.speed = 1f;

        photon.mode = PhotonMode.Absorbing;

        photon.renderer.enabled = true;

        // Place at the correct point immediately.
        // This prevents visible outward teleporting.
        EvaluateAbsorption(photon, false);
    }

    // Normal particles entering a hole get permanently
    // transferred to its absorption path.
    private void CaptureBackground(
        Photon photon,
        int holeIndex)
    {
        Vector2 center = holeCenters[holeIndex];

        Vector2 offset =
            photon.position - center;

        float radius = offset.magnitude;

        if (radius <= eventHorizonRadius)
        {
            HidePhoton(photon);
            return;
        }

        photon.targetHole = holeIndex;
        photon.startingRadius = radius;

        photon.startingAngle =
            Mathf.Atan2(offset.y, offset.x);

        photon.rotationSign =
            holeIndex % 2 == 0 ? 1f : -1f;

        photon.elapsed = 0f;

        photon.duration = Mathf.Max(
            0.2f,
            absorptionSeconds *
            Mathf.Clamp(
                radius / Mathf.Max(streamStartRadius, 0.1f),
                0.35f,
                1.2f
            )
        );

        photon.mode = PhotonMode.Absorbing;

        photon.renderer.enabled = true;
    }

    private void Update()
    {
        if (grid == null || meshVertices == null)
            return;

        float dt = Time.deltaTime;

        if (dt <= 0f)
            return;

        foreach (Photon photon in photons)
        {
            switch (photon.mode)
            {
                case PhotonMode.Background:
                    MoveBackground(photon, dt);
                    break;

                case PhotonMode.Absorbing:
                    photon.elapsed += dt;
                    EvaluateAbsorption(photon, true);
                    break;

                case PhotonMode.Hidden:
                    photon.hiddenTime -= dt;

                    if (photon.hiddenTime <= 0f)
                    {
                        if (photon.homeHole >= 0)
                        {
                            SpawnIntoStream(
                                photon,
                                photon.homeHole,
                                0f
                            );
                        }
                        else
                        {
                            SpawnBackground(photon);
                        }
                    }

                    break;
            }
        }
    }

    private void MoveBackground(
        Photon photon,
        float dt)
    {
        Vector2 previous = photon.position;

        Vector2 next = previous +
            photon.direction *
            backgroundSpeed *
            photon.speed *
            dt;

        if (!WithinGrid(next))
        {
            HidePhoton(photon);
            return;
        }

        // Detect whether the line segment traveled
        // by this photon crosses a capture zone.
        int capturedBy = -1;
        float nearestDistance = float.MaxValue;

        Vector2 displacement = next - previous;

        foreach (
            int index in HoleIndices()
        )
        {
            Vector2 center = holeCenters[index];

            float t = displacement.sqrMagnitude > 0f
                ? Mathf.Clamp01(
                    Vector2.Dot(
                        center - previous,
                        displacement
                    ) / displacement.sqrMagnitude
                )
                : 0f;

            Vector2 closest =
                previous + displacement * t;

            float distance =
                Vector2.Distance(closest, center);

            if (distance <= attractionRadius &&
                distance < nearestDistance)
            {
                nearestDistance = distance;
                capturedBy = index;
            }
        }

        photon.position = next;

        if (capturedBy >= 0)
        {
            CaptureBackground(photon, capturedBy);
            return;
        }

        PositionVisual(
            photon,
            photon.direction,
            0.8f
        );
    }

    // Enumerate cached hole indices without requiring
    // any black-hole physics or influence settings.
    private IEnumerable<int> HoleIndices()
    {
        for (int i = 0; i < holeCenters.Count; i++)
            yield return i;
    }

    // Dedicated flow and captured ambient photons both
    // use this guaranteed inward motion.
    private void EvaluateAbsorption(
        Photon photon,
        bool checkEnd)
    {
        float t = Mathf.Clamp01(
            photon.elapsed / photon.duration
        );

        if (checkEnd && t >= 1f)
        {
            HidePhoton(photon);
            return;
        }

        Vector2 center =
            holeCenters[photon.targetHole];

        Vector2 previous = photon.position;

        // Accelerating radial motion.
        float inwardProgress = t;

        float radius = Mathf.Lerp(
            photon.startingRadius,
            eventHorizonRadius,
            inwardProgress
        );

        // Visible spiral while radius shrinks.
        float angle = photon.startingAngle +
            photon.rotationSign *
            spiralRevolutions *
            Mathf.PI * 2f * t;

        Vector2 offset = new Vector2(
            Mathf.Cos(angle),
            Mathf.Sin(angle)
        );

        photon.position =
            center + offset * radius;

        // The photon cannot move outward:
        // radius is determined only by decreasing time.
        float nextT = Mathf.Min(1f, t + 0.01f);

        float nextRadius = Mathf.Lerp(
            photon.startingRadius,
            eventHorizonRadius,
            nextT
        );

        float nextAngle = photon.startingAngle +
            photon.rotationSign *
            spiralRevolutions *
            Mathf.PI * 2f * nextT;

        Vector2 nextOffset = new Vector2(
            Mathf.Cos(nextAngle),
            Mathf.Sin(nextAngle)
        );

        Vector2 tangent =
            center + nextOffset * nextRadius -
            photon.position;

        // Brighter warm colors toward the center.
        photon.renderer.sharedMaterial =
            photon.homeHole < 0 && t < 0.65f
                ? backgroundMaterial
                : t < 0.85f
                    ? outerMaterial
                    : innerMaterial;

        float fadeIn = photon.homeHole < 0
            ? 1f
            : Mathf.Clamp01(t / 0.06f);

        float fadeOut =
            Mathf.Clamp01((1f - t) / 0.14f);

        float shrink =
            Mathf.Lerp(1.4f, 0.45f, t);

        float size =
            fadeIn * fadeOut * shrink;

        PositionVisual(
            photon,
            tangent.sqrMagnitude > 0.00001f
                ? tangent
                : center - photon.position,
            size
        );
    }

    // Exact triangle interpolation matching the
    // generated GravityGrid mesh triangles.
    // More accurate than bilinear interpolation
    // near steep black-hole walls.
    private float GetSurfaceHeight(
        float x,
        float z)
    {
        float u = Mathf.Clamp01(
            x / grid.width + 0.5f
        );

        float v = Mathf.Clamp01(
            z / grid.length + 0.5f
        );

        float fx = u * grid.meshColumns;
        float fz = v * grid.meshRows;

        int ix = Mathf.Min(
            Mathf.FloorToInt(fx),
            grid.meshColumns - 1
        );

        int iz = Mathf.Min(
            Mathf.FloorToInt(fz),
            grid.meshRows - 1
        );

        float tx = fx - ix;
        float tz = fz - iz;

        int stride = grid.meshColumns + 1;

        int a = iz * stride + ix;
        int b = a + 1;
        int c = a + stride;
        int d = c + 1;

        float ha = meshVertices[a].y;
        float hb = meshVertices[b].y;
        float hc = meshVertices[c].y;
        float hd = meshVertices[d].y;

        if (tz >= tx)
        {
            // Triangle a, c, d.
            return (1f - tz) * ha +
                   (tz - tx) * hc +
                   tx * hd;
        }

        // Triangle a, d, b.
        return (1f - tx) * ha +
               (tx - tz) * hb +
               tz * hd;
    }

    private void PositionVisual(
        Photon photon,
        Vector2 movement,
        float sizeMultiplier)
    {
        float y = GetSurfaceHeight(
            photon.position.x,
            photon.position.y
        );

        photon.transform.localPosition =
            new Vector3(
                photon.position.x,
                y + heightAboveGrid,
                photon.position.y
            );

        float scale =
            Mathf.Max(0.001f, sizeMultiplier);

        photon.transform.localScale =
            new Vector3(
                photonWidth * scale,
                0.03f * scale,
                photonLength * scale
            );

        Vector3 forward = new Vector3(
            movement.x,
            0f,
            movement.y
        );

        if (forward.sqrMagnitude > 0.00001f)
        {
            photon.transform.localRotation =
                Quaternion.LookRotation(
                    forward.normalized,
                    Vector3.up
                );
        }
    }

    private void OnDestroy()
    {
        if (backgroundMaterial != null)
            Destroy(backgroundMaterial);

        if (outerMaterial != null)
            Destroy(outerMaterial);

        if (innerMaterial != null)
            Destroy(innerMaterial);
    }
}
