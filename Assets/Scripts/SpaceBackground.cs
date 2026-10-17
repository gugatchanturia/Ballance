
using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SpaceBackground : MonoBehaviour
{
    [Header("Starfield")]
    [Range(100, 3000)]
    public int starCount = 1400;

    [Range(0f, 1f)]
    public float starBrightness = 0.75f;

    [Range(0f, 1f)]
    public float backgroundBrightness = 0.25f;

    [Header("Generation")]
    public int seed = 583;

    private const int Width = 2048;
    private const int Height = 1024;

    private Texture2D starTexture;
    private Material skyboxMaterial;
    private Material originalSkybox;

    private void Awake()
    {
        CreateStarfield();

        Camera camera = GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
    }

    private void CreateStarfield()
    {
        Shader skyboxShader =
            Shader.Find("Skybox/Panoramic");

        if (skyboxShader == null)
        {
            Debug.LogError(
                "SpaceBackground: Panoramic skybox shader not found."
            );
            return;
        }

        originalSkybox = RenderSettings.skybox;

        starTexture = new Texture2D(
            Width,
            Height,
            TextureFormat.RGBA32,
            false
        );

        starTexture.name = "Generated Space Starfield";
        starTexture.wrapMode = TextureWrapMode.Repeat;
        starTexture.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[Width * Height];

        // Subtle navy-black space, rather than flat black.
        byte red = (byte)Mathf.RoundToInt(
            4f + backgroundBrightness * 8f
        );

        byte green = (byte)Mathf.RoundToInt(
            6f + backgroundBrightness * 10f
        );

        byte blue = (byte)Mathf.RoundToInt(
            13f + backgroundBrightness * 19f
        );

        Color32 spaceColor = new Color32(
            red, green, blue, 255
        );

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = spaceColor;

        System.Random random = new System.Random(seed);

        for (int i = 0; i < starCount; i++)
        {
            int x = random.Next(Width);
            int y = random.Next(Height);

            // Most stars are dim and tiny.
            float brightness = (float)random.NextDouble();

            brightness = Mathf.Pow(brightness, 2.2f);
            brightness *= starBrightness;

            // A small proportion of brighter stars.
            if (i % 37 == 0)
                brightness = starBrightness;

            float intensity = Mathf.Lerp(
                0.10f,
                1f,
                brightness
            );

            // Slightly warm or cool variations.
            float temperature =
                (float)random.NextDouble();

            Color starColor;

            if (temperature < 0.25f)
            {
                starColor = new Color(
                    1f, 0.82f, 0.65f
                );
            }
            else if (temperature < 0.50f)
            {
                starColor = new Color(
                    0.72f, 0.85f, 1f
                );
            }
            else
            {
                starColor = Color.white;
            }

            starColor *= intensity;

            AddStarPixel(pixels, x, y, starColor);

            // A few brighter stars receive a tiny halo.
            if (i % 53 == 0)
            {
                Color halo = starColor * 0.18f;

                AddStarPixel(pixels, x - 1, y, halo);
                AddStarPixel(pixels, x + 1, y, halo);
                AddStarPixel(pixels, x, y - 1, halo);
                AddStarPixel(pixels, x, y + 1, halo);
            }
        }

        starTexture.SetPixels32(pixels);
        starTexture.Apply(false, true);

        skyboxMaterial = new Material(skyboxShader);

        skyboxMaterial.SetTexture(
            "_MainTex",
            starTexture
        );

        skyboxMaterial.SetColor(
            "_Tint",
            Color.white
        );

        skyboxMaterial.SetFloat("_Exposure", 1f);
        skyboxMaterial.SetFloat("_Rotation", 0f);

        RenderSettings.skybox = skyboxMaterial;
    }

    private void AddStarPixel(
        Color32[] pixels,
        int x,
        int y,
        Color color
    )
    {
        if (y < 0 || y >= Height)
            return;

        // Wrap around the panorama horizontally.
        x = (x % Width + Width) % Width;

        int index = y * Width + x;

        Color existing = pixels[index];
        Color combined = existing + color;
        combined.a = 1f;

        pixels[index] = combined;
    }

    private void OnDestroy()
    {
        if (skyboxMaterial != null)
        {
            if (RenderSettings.skybox == skyboxMaterial)
                RenderSettings.skybox = originalSkybox;

            Destroy(skyboxMaterial);
        }

        if (starTexture != null)
            Destroy(starTexture);
    }
}
