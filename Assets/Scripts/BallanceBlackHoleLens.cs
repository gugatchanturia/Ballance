using System.Collections;
using UnityEngine;

/// <summary>
/// Positions a separate, VFX-only black-hole prefab at the bottom of Ballance's
/// physically curved grid. Does not touch gravity, collision, or Earth physics.
/// Intended for the MIT-licensed tantaneity/urp-black-hole Unity 6 URP prefab.
/// Attach to the imported VFX prefab ROOT, not the physical BlackHole GameObject.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer))]
public class BallanceBlackHoleLens : MonoBehaviour
{
    [Header("Existing Ballance Objects")]
    [SerializeField] private BlackHole physicalBlackHole;
    [SerializeField] private GravityGrid gravityGrid;

    [Header("Appearance / Alignment")]
    [Tooltip("The imported prefab uses a unit-diameter sphere. This is its world diameter.")]
    [Min(0.1f)] public float visualDiameter = 4.5f;

    [Tooltip("Height above the lowest part of the physical well, along its local up axis.")]
    public float heightAboveWell = 0.2f;

    [Tooltip("Extra disk tilt in degrees around the field's local X axis.")]
    public float diskTiltDegrees = 0f;

    [Header("Interstellar Visual Parameters")]
    [Range(0.02f, 0.2f)] public float eventHorizonScale = 0.18f;
    [Range(1.2f, 6f)] public float diskInnerRadius = 1.5f;
    [Range(4f, 12f)] public float diskOuterRadius = 4f;
    [Range(0f, 30f)] public float diskBrightness = 6f;
    [Range(0f, 3f)] public float diskRotationSpeed = 1.2f;
    [Range(0f, 2f)] public float starfieldIntensity = 1f;

    [Header("Performance (begin here)")]
    [Range(64, 320)] public int raymarchSteps = 128;
    public bool supersampling = false;

    private MeshRenderer vfxRenderer;
    private MeshFilter gridFilter;
    private Material runtimeMaterial;
    private Vector3[] surfaceVertices;
    private bool initialized;

    private IEnumerator Start()
    {
        if (physicalBlackHole == null || gravityGrid == null)
        {
            Debug.LogError("BallanceBlackHoleLens: Assign the physical BlackHole and GravityGrid references.", this);
            yield break;
        }

        vfxRenderer = GetComponent<MeshRenderer>();
        gridFilter = gravityGrid.GetComponent<MeshFilter>();

        // GravityGrid creates its mesh in Start; CurvedFieldPhysicsTest can replace
        // its MeshFilter mesh after a frame. Wait for BOTH before sampling it.
        yield return null;
        yield return null;

        if (gridFilter == null || gridFilter.sharedMesh == null || vfxRenderer.sharedMaterial == null)
        {
            Debug.LogError("BallanceBlackHoleLens: The generated grid mesh or the VFX material is missing.", this);
            yield break;
        }

        surfaceVertices = gridFilter.sharedMesh.vertices;
        int expected = (gravityGrid.meshColumns + 1) * (gravityGrid.meshRows + 1);
        if (surfaceVertices.Length != expected)
        {
            Debug.LogError("BallanceBlackHoleLens: Grid mesh dimensions do not match GravityGrid settings.", this);
            yield break;
        }

        // Material instances prevent changes to the imported prefab or other holes.
        runtimeMaterial = new Material(vfxRenderer.sharedMaterial);
        runtimeMaterial.name = "Ballance Black Hole Lens (Runtime)";
        runtimeMaterial.SetFloat("_HorizonScale", eventHorizonScale);
        runtimeMaterial.SetFloat("_DiskInnerRadius", diskInnerRadius);
        runtimeMaterial.SetFloat("_DiskOuterRadius", diskOuterRadius);
        runtimeMaterial.SetFloat("_DiskBrightness", diskBrightness);
        runtimeMaterial.SetFloat("_DiskRotationSpeed", diskRotationSpeed);
        runtimeMaterial.SetFloat("_StarfieldIntensity", starfieldIntensity);
        runtimeMaterial.SetFloat("_StepCount", raymarchSteps);
        runtimeMaterial.SetFloat("_Supersample", supersampling ? 1f : 0f);
        if (supersampling)
            runtimeMaterial.EnableKeyword("_SUPERSAMPLE_4X");
        else
            runtimeMaterial.DisableKeyword("_SUPERSAMPLE_4X");

        vfxRenderer.sharedMaterial = runtimeMaterial;
        initialized = true;
        FollowHole();
    }

    private void LateUpdate()
    {
        if (initialized)
            FollowHole();
    }

    private void FollowHole()
    {
        // Sample the *actual rendered/collision mesh* at the gameplay hole's X/Z.
        // No duplicated gravitational-well formulas or scaled-parent arithmetic.
        Transform gridTransform = gravityGrid.transform;
        Vector3 holeLocal = gridTransform.InverseTransformPoint(physicalBlackHole.transform.position);
        float surfaceY = SampleGridHeight(holeLocal.x, holeLocal.z);

        Vector3 worldCenter = gridTransform.TransformPoint(
            new Vector3(holeLocal.x, surfaceY + heightAboveWell, holeLocal.z)
        );

        transform.SetPositionAndRotation(
            worldCenter,
            gridTransform.rotation * Quaternion.Euler(diskTiltDegrees, 0f, 0f)
        );

        // Keep the visual effect's world scale uniform; NEVER parent it under
        // GravityField's non-uniform (12,0.2,18) scale.
        transform.localScale = Vector3.one * visualDiameter;
    }

    private float SampleGridHeight(float x, float z)
    {
        int cols = gravityGrid.meshColumns;
        int rows = gravityGrid.meshRows;

        float gx = Mathf.Clamp01(x / Mathf.Max(0.01f, gravityGrid.width) + 0.5f) * cols;
        float gz = Mathf.Clamp01(z / Mathf.Max(0.01f, gravityGrid.length) + 0.5f) * rows;

        int x0 = Mathf.Min(Mathf.FloorToInt(gx), cols - 1);
        int z0 = Mathf.Min(Mathf.FloorToInt(gz), rows - 1);
        float tx = gx - x0;
        float tz = gz - z0;
        int stride = cols + 1;

        float h00 = surfaceVertices[z0 * stride + x0].y;
        float h10 = surfaceVertices[z0 * stride + x0 + 1].y;
        float h01 = surfaceVertices[(z0 + 1) * stride + x0].y;
        float h11 = surfaceVertices[(z0 + 1) * stride + x0 + 1].y;

        return Mathf.Lerp(
            Mathf.Lerp(h00, h10, tx),
            Mathf.Lerp(h01, h11, tx),
            tz
        );
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}
