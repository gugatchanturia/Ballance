
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class SatellitePickup : MonoBehaviour
{
    [Header("References")]
    public EarthSatelliteCollector collector;

    [Header("Visuals")]
    public float visualScale = 1f;
    public float idleSpinSpeed = 50f;
    public float bobHeight = 0.08f;
    public float bobSpeed = 2f;

    private Transform visualRoot;
    private SphereCollider triggerCollider;
    private bool collected;

    private void Start()
    {
        if (collector == null)
        {
            collector =
                FindFirstObjectByType<EarthSatelliteCollector>();
        }

        if (collector != null)
            collector.Register(this);

        triggerCollider = GetComponent<SphereCollider>();
        triggerCollider.isTrigger = true;

        CreateSatelliteVisual();
    }

    private void Update()
    {
        if (visualRoot == null)
            return;

        // Rotate the small satellite itself.
        visualRoot.Rotate(
            Vector3.up,
            idleSpinSpeed * Time.deltaTime,
            Space.Self
        );

        if (!collected)
        {
            visualRoot.localPosition = new Vector3(
                0f,
                Mathf.Sin(Time.time * bobSpeed) * bobHeight,
                0f
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || collector == null ||
            collector.earth == null)
            return;

        if (other.attachedRigidbody != collector.earth)
            return;

        collected = true;

        // Remove the pickup trigger.
        triggerCollider.enabled = false;

        // Detach from GravityGrid. The collector will
        // position this satellite around Earth in world space.
        transform.SetParent(null, true);

        visualRoot.localPosition = Vector3.zero;

        // Shrink the satellite after collection.
        // Its original pickup appearance stays unchanged.
        visualRoot.localScale *= 0.45f;

        collector.Collect(this);

        GameAudio3D audio = collector.GetComponent<GameAudio3D>();

        if (audio != null)
            audio.PlayPickup();
    }

    private void CreateSatelliteVisual()
    {
        GameObject root = new GameObject("Satellite Visual");
        root.transform.SetParent(transform, false);
        visualRoot = root.transform;

        Material bodyMaterial = CreateMaterial(
            new Color(0.75f, 0.80f, 0.86f)
        );

        Material panelMaterial = CreateMaterial(
            new Color(0.08f, 0.35f, 0.85f)
        );

        Material antennaMaterial = CreateMaterial(
            new Color(0.95f, 0.75f, 0.30f)
        );
        // Make the satellite visible against the glowing grid.
        panelMaterial.EnableKeyword("_EMISSION");
        panelMaterial.SetColor(
            "_EmissionColor",
            new Color(0.1f, 0.5f, 1f) * 2.5f
        );

        antennaMaterial.EnableKeyword("_EMISSION");
        antennaMaterial.SetColor(
            "_EmissionColor",
            new Color(1f, 0.75f, 0.2f) * 2f
        );

        // Main silver satellite body.
        CreatePart(
            "Satellite Body",
            new Vector3(0f, 0f, 0f),
            new Vector3(0.18f, 0.14f, 0.14f),
            bodyMaterial
        );

        // Left solar panel.
        CreatePart(
            "Left Solar Panel",
            new Vector3(-0.25f, 0f, 0f),
            new Vector3(0.26f, 0.02f, 0.17f),
            panelMaterial
        );

        // Right solar panel.
        CreatePart(
            "Right Solar Panel",
            new Vector3(0.25f, 0f, 0f),
            new Vector3(0.26f, 0.02f, 0.17f),
            panelMaterial
        );

        // Small golden antenna.
        CreatePart(
            "Antenna",
            new Vector3(0f, 0.13f, 0f),
            new Vector3(0.025f, 0.16f, 0.025f),
            antennaMaterial
        );

        visualRoot.localScale =
            Vector3.one * visualScale;
    }

    private void CreatePart(
        string partName,
        Vector3 position,
        Vector3 scale,
        Material material
    )
    {
        GameObject part =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        part.name = partName;
        part.transform.SetParent(visualRoot, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;

        // All parts are visual-only.
        Destroy(part.GetComponent<Collider>());

        part.GetComponent<MeshRenderer>().material =
            material;
    }

    private Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find(
            "Universal Render Pipeline/Lit"
        );

        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.color = color;

        return material;
    }
}
