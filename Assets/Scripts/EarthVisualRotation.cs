using UnityEngine;

// Rotates only the globe's visual mesh. Does not move or rotate its Rigidbody.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EarthVisualRotation : MonoBehaviour
{
    [Tooltip("Degrees per second. Positive spins the globe around its local Y axis.")]
    public float degreesPerSecond = 8f;

    private Transform visualGlobe;

    void Awake()
    {
        MeshFilter sourceMesh = GetComponent<MeshFilter>();
        MeshRenderer sourceRenderer = GetComponent<MeshRenderer>();

        if (sourceMesh.sharedMesh == null)
            return;

        GameObject visual = new GameObject("Rotating Globe Visual");
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        visual.AddComponent<MeshFilter>().sharedMesh = sourceMesh.sharedMesh;
        MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = sourceRenderer.sharedMaterials;
        renderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
        renderer.receiveShadows = sourceRenderer.receiveShadows;

        // Hide only the original rendered sphere, NOT its collider or Rigidbody.
        sourceRenderer.enabled = false;
        visualGlobe = visual.transform;
    }

    void Update()
    {
        if (visualGlobe != null)
            visualGlobe.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
