using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Visual-only enhancement. Does not edit SolarSystem3D, gravity, colliders or orbits.
[RequireComponent(typeof(SolarSystem3D))]
public class SolarAppearance3D : MonoBehaviour
{
    [Header("Surface rotation (degrees per second)")]
    public float sunSpin = 5f;
    public float mercurySpin = 22f;
    public float venusSpin = -12f; // Venus rotates retrograde.

    [Header("Sun glow")]
    [Range(1.1f, 2.5f)] public float coronaSizeMultiplier = 1.8f;
    [Range(0f, 1f)] public float coronaOpacity = 0.55f;

    [Header("Texture detail")]
    [Range(128, 1024)] public int textureWidth = 512;

    private Transform sun;
    private Transform mercury;
    private Transform venus;
    private Transform corona;
    private Material coronaMaterial;
    private Texture2D coronaTexture;
    private Camera targetCamera;
    private readonly List<Material> createdMaterials = new List<Material>();
    private readonly List<Texture2D> createdTextures = new List<Texture2D>();

    private IEnumerator Start()
    {
        // SolarSystem3D.Start constructs the visual objects at runtime.
        yield return null;

        sun = transform.Find("Sun");
        mercury = transform.Find("Inner Planet");
        venus = transform.Find("Outer Planet");
        targetCamera = Camera.main;

        if (sun == null || mercury == null || venus == null)
        {
            Debug.LogWarning("SolarAppearance3D: Sun or planets not found. Keep SolarSystem3D enabled.", this);
            yield break;
        }

        ApplySurface(sun, CreateSurfaceTexture(0), true);
        ApplySurface(mercury, CreateSurfaceTexture(1), false);
        ApplySurface(venus, CreateSurfaceTexture(2), false);
        CreateCorona();
    }

    private void Update()
    {
        // Visual spin is independent of orbital movement.
        if (sun != null) sun.Rotate(Vector3.up, sunSpin * Time.deltaTime, Space.Self);
        if (mercury != null) mercury.Rotate(Vector3.up, mercurySpin * Time.deltaTime, Space.Self);
        if (venus != null) venus.Rotate(Vector3.up, venusSpin * Time.deltaTime, Space.Self);
    }

    private void LateUpdate()
    {
        if (corona == null) return;
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera != null)
            corona.rotation = targetCamera.transform.rotation;
        if (coronaMaterial != null)
            coronaMaterial.SetColor("_BaseColor", new Color(1f, 0.65f, 0.25f, coronaOpacity));
    }

    private Texture2D CreateSurfaceTexture(int kind)
    {
        int w = Mathf.Clamp(textureWidth, 128, 1024);
        int h = w / 2;
        Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, true);
        texture.name = kind == 0 ? "Procedural Sun Surface" : kind == 1 ? "Procedural Mercury" : "Procedural Venus";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        Color32[] pixels = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = (float)x / w;
                float v = (float)y / h;
                float n1 = SeamlessNoise(u, v, kind * 23f + 4f, 6f, 4f);
                float n2 = SeamlessNoise(u, v, kind * 17f + 31f, 20f, 12f);
                float n3 = SeamlessNoise(u, v, kind * 11f + 74f, 52f, 25f);
                Color color;

                if (kind == 0)
                {
                    // Golden plasma and amber sunspots, not a plain yellow disc.
                    float plasma = Mathf.Clamp01(n1 * 0.65f + n2 * 0.28f + n3 * 0.07f);
                    color = Color.Lerp(new Color(0.73f, 0.17f, 0.025f), new Color(1f, 0.79f, 0.21f), Mathf.SmoothStep(0f, 1f, plasma));
                    float hot = Mathf.SmoothStep(0.60f, 0.83f, n2);
                    color = Color.Lerp(color, new Color(1f, 0.91f, 0.48f), hot * 0.33f);
                }
                else if (kind == 1)
                {
                    // Mercury: mottled gray, crater-like mineral variation.
                    float rock = Mathf.Clamp01(n1 * 0.72f + n2 * 0.28f);
                    color = Color.Lerp(new Color(0.22f, 0.20f, 0.19f), new Color(0.76f, 0.72f, 0.66f), rock);
                }
                else
                {
                    // Venus: muted warm cloud bands. Retrograde rotation is applied separately.
                    float bands = Mathf.Sin(v * Mathf.PI * 20f + (n1 - 0.5f) * 5f) * 0.095f;
                    float clouds = Mathf.Clamp01(n1 * 0.68f + n2 * 0.32f + bands);
                    color = Color.Lerp(new Color(0.58f, 0.30f, 0.12f), new Color(1f, 0.85f, 0.57f), clouds);
                }
                pixels[y * w + x] = color;
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(true, true);
        createdTextures.Add(texture);
        return texture;
    }

    private static float SeamlessNoise(float u, float v, float seed, float horizontalFrequency, float verticalFrequency)
    {
        // Blend opposite U edges, avoiding an obvious texture seam on spheres.
        float a = Mathf.PerlinNoise(seed + u * horizontalFrequency, seed + v * verticalFrequency);
        float b = Mathf.PerlinNoise(seed + (u - 1f) * horizontalFrequency, seed + v * verticalFrequency);
        return Mathf.Lerp(a, b, u);
    }

    private void ApplySurface(Transform sphere, Texture2D texture, bool isSun)
    {
        Renderer renderer = sphere.GetComponent<Renderer>();
        if (renderer == null) return;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return;

        Material material = new Material(shader);
        material.name = sphere.name + " Surface (Runtime)";
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", isSun ? 0.12f : 0.24f);
        if (isSun)
        {
            material.EnableKeyword("_EMISSION");
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_EmissionColor", new Color(1f, 0.46f, 0.09f) * 1.5f);
        }
        renderer.sharedMaterial = material;
        createdMaterials.Add(material);
    }

    private void CreateCorona()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) return;

        const int size = 256;
        coronaTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        coronaTexture.name = "Soft Solar Corona";
        coronaTexture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = ((x + 0.5f) / size - 0.5f) * 2f;
                float dy = ((y + 0.5f) / size - 0.5f) * 2f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - r);
                a = Mathf.Pow(a, 2.8f) * 0.68f;
                pixels[y * size + x] = new Color(1f, 0.56f, 0.16f, a);
            }
        }
        coronaTexture.SetPixels32(pixels);
        coronaTexture.Apply(false, true);

        coronaMaterial = new Material(shader);
        coronaMaterial.name = "Solar Corona (Runtime)";
        coronaMaterial.SetTexture("_BaseMap", coronaTexture);
        coronaMaterial.SetColor("_BaseColor", new Color(1f, 0.65f, 0.25f, coronaOpacity));
        coronaMaterial.SetFloat("_Surface", 1f);
        coronaMaterial.SetFloat("_Blend", 0f);
        coronaMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        coronaMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        coronaMaterial.SetFloat("_ZWrite", 0f);
        coronaMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        coronaMaterial.renderQueue = (int)RenderQueue.Transparent;

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Sun Corona (Visual Only)";
        quad.transform.SetParent(sun, false);
        quad.transform.localPosition = Vector3.zero;
        // Sun mesh is a unit sphere scaled to the desired diameter.
        quad.transform.localScale = Vector3.one * coronaSizeMultiplier;
        Collider collider = quad.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        Renderer renderer = quad.GetComponent<Renderer>();
        renderer.sharedMaterial = coronaMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        corona = quad.transform;
    }

    private void OnDestroy()
    {
        foreach (Material material in createdMaterials) if (material != null) Destroy(material);
        foreach (Texture2D texture in createdTextures) if (texture != null) Destroy(texture);
        if (coronaMaterial != null) Destroy(coronaMaterial);
        if (coronaTexture != null) Destroy(coronaTexture);
    }
}
