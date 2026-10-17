
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EarthAccelerationMeter : MonoBehaviour
{
    [Header("Meter Settings")]
    public float maxAcceleration = 10f;
    public float smoothing = 8f;
    public float meterSize = 120f;

    private Rigidbody rb;
    private Vector3 previousVelocity;
    private Vector2 displayedAcceleration;

    private Texture2D dialTexture;
    private Texture2D dotTexture;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        previousVelocity = rb.linearVelocity;

        CreateTextures();
    }

    void FixedUpdate()
    {
        Vector3 velocity = rb.linearVelocity;

        // Calculate acceleration from velocity changes.
        Vector3 acceleration =
            (velocity - previousVelocity) / Time.fixedDeltaTime;

        previousVelocity = velocity;

        // X = left/right, Z = forward/backward.
        Vector2 planarAcceleration = new Vector2(
            acceleration.x,
            acceleration.z
        );

        // Smooth the display without changing physics.
        float t = 1f - Mathf.Exp(
            -smoothing * Time.fixedDeltaTime
        );

        displayedAcceleration = Vector2.Lerp(
            displayedAcceleration,
            planarAcceleration,
            t
        );
    }

    void OnGUI()
    {
        if (dialTexture == null || dotTexture == null)
            return;

        float size = meterSize;
        float x = 16f;
        float y = Screen.height - size - 40f;

        GUI.DrawTexture(
            new Rect(x, y, size, size),
            dialTexture
        );

        // Limit the indicator to the circular display.
        Vector2 normalized = Vector2.ClampMagnitude(
            displayedAcceleration / Mathf.Max(maxAcceleration, 0.01f),
            1f
        );

        float radius = size * 0.42f;
        Vector2 center = new Vector2(
            x + size / 2f,
            y + size / 2f
        );

        Vector2 dotPosition = center + new Vector2(
            normalized.x * radius,
            -normalized.y * radius
        );

        float dotSize = 12f;

        GUI.DrawTexture(
            new Rect(
                dotPosition.x - dotSize / 2f,
                dotPosition.y - dotSize / 2f,
                dotSize,
                dotSize
            ),
            dotTexture
        );

        GUI.Label(
            new Rect(x, y + size, size, 24f),
            "ACCEL  X / Z"
        );
    }

    void CreateTextures()
    {
        const int resolution = 128;
        dialTexture = new Texture2D(resolution, resolution);

        float center = (resolution - 1) / 2f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                Color color = Color.clear;

                if (distance <= 60f)
                    color = new Color(0.04f, 0.08f, 0.16f, 0.8f);

                // Circular measurement rings.
                if (Mathf.Abs(distance - 56f) < 1.3f ||
                    Mathf.Abs(distance - 28f) < 0.8f)
                    color = new Color(0.4f, 0.65f, 0.85f, 0.8f);

                // Horizontal and vertical reference axes.
                if (distance < 56f &&
                    (Mathf.Abs(dx) < 0.7f ||
                     Mathf.Abs(dy) < 0.7f))
                    color = new Color(0.35f, 0.5f, 0.7f, 0.5f);

                dialTexture.SetPixel(x, y, color);
            }
        }

        dialTexture.Apply();
        dialTexture.filterMode = FilterMode.Bilinear;

        dotTexture = new Texture2D(16, 16);

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float distance = Vector2.Distance(
                    new Vector2(x, y),
                    new Vector2(7.5f, 7.5f)
                );

                dotTexture.SetPixel(
                    x, y,
                    distance <= 6.5f
                        ? new Color(1f, 0.65f, 0.15f, 1f)
                        : Color.clear
                );
            }
        }

        dotTexture.Apply();
        dotTexture.filterMode = FilterMode.Bilinear;
    }

    void OnDestroy()
    {
        if (dialTexture != null)
            Destroy(dialTexture);

        if (dotTexture != null)
            Destroy(dotTexture);
    }
}
