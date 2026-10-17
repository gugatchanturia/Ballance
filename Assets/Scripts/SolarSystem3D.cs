
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SolarSystem3D : MonoBehaviour
{
    [Header("Gameplay References")]
    public Rigidbody earth;
    public GravityFieldController fieldController;

    [Header("Sun")]
    public float sunRadius = 0.9f;
    public float winRadius = 1.1f;
    public float lightIntensity = 3f;
    public float lightRange = 5f;

    [Header("Sun Gravity")]
    public float gravityRadius = 4f;
    public float gravityStrength = 6f;
    public float maximumGravity = 12f;

    [Header("Visible Solar Gravity Well")]
    [Min(0.1f)] public float meshGravityRadius = 3f;
    [Min(0f)] public float meshGravityDepth = 0.65f;

    [Header("Earth Added As Third Planet After Winning")]
    public float earthOrbitRadius = 1.72f;
    public float earthPlanetRadius = 0.16f;
    public float earthOrbitSpeed = 48f;

    [Header("Earth Absorption")]
    public float absorptionDuration = 2.5f;
    public float absorptionOrbits = 2.5f;

    [Header("Inner Planet")]
    public float innerOrbitRadius = 1.35f;
    public float innerPlanetRadius = 0.12f;
    public float innerOrbitSpeed = 65f;

    [Header("Outer Planet")]
    public float outerOrbitRadius = 2f;
    public float outerPlanetRadius = 0.18f;
    public float outerOrbitSpeed = 35f;

    [Header("Orbit Appearance")]
    public int orbitSegments = 128;
    public float orbitLineWidth = 0.025f;

    private Transform innerPlanet;
    private Transform outerPlanet;
    private Transform victoryEarthPlanet;
    private float victoryEarthAngle;

    private float innerAngle;
    private float outerAngle;

    private bool absorbingEarth;
    private bool hasWon;

    private void Start()
    {
        CreateSun();

        CreateOrbit(
            "Inner Orbit",
            innerOrbitRadius,
            new Color(1f, 0.72f, 0.27f)
        );

        CreateOrbit(
            "Outer Orbit",
            outerOrbitRadius,
            new Color(0.45f, 0.75f, 1f)
        );

        innerPlanet = CreatePlanet(
            "Inner Planet",
            innerPlanetRadius,
            new Color(0.12f, 0.55f, 1f)
        );

        outerPlanet = CreatePlanet(
            "Outer Planet",
            outerPlanetRadius,
            new Color(1f, 0.3f, 0.08f)
        );

        innerAngle = 0f;
        outerAngle = 180f;

        UpdatePlanetPositions();

        SphereCollider goalCollider =
            GetComponent<SphereCollider>();

        if (goalCollider == null)
            goalCollider = gameObject.AddComponent<SphereCollider>();

        goalCollider.isTrigger = true;
        goalCollider.radius = winRadius;
    }

    private void Update()
    {
        // Decorative planets keep orbiting, including
        // during Earth's absorption animation.
        innerAngle += innerOrbitSpeed * Time.deltaTime;
        outerAngle += outerOrbitSpeed * Time.deltaTime;

        if (victoryEarthPlanet != null)
        {
            victoryEarthAngle += earthOrbitSpeed * Time.deltaTime;
            victoryEarthPlanet.localPosition = new Vector3(
                Mathf.Cos(victoryEarthAngle * Mathf.Deg2Rad) * earthOrbitRadius,
                0f,
                Mathf.Sin(victoryEarthAngle * Mathf.Deg2Rad) * earthOrbitRadius
            );
            victoryEarthPlanet.Rotate(Vector3.up, 90f * Time.deltaTime);
        }

        UpdatePlanetPositions();
    }

    private void FixedUpdate()
    {
        if (absorbingEarth || hasWon ||
            earth == null || earth.isKinematic)
            return;

        ApplySolarGravity();
    }

    private void ApplySolarGravity()
    {
        Vector3 difference =
            transform.position - earth.position;

        // Pull along the tilted platform instead of
        // lifting Earth vertically into the air.
        Vector3 planarDirection = Vector3.ProjectOnPlane(
            difference,
            transform.up
        );

        float distance = planarDirection.magnitude;

        if (distance < 0.001f ||
            distance > gravityRadius)
            return;

        // Gravity increases as Earth approaches the Sun.
        float proximity = 1f - distance / gravityRadius;

        float acceleration = Mathf.Min(
            gravityStrength * proximity * proximity * 4f,
            maximumGravity
        );

        earth.AddForce(
            planarDirection.normalized * acceleration,
            ForceMode.Acceleration
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (absorbingEarth || hasWon || earth == null)
            return;

        if (other.attachedRigidbody != earth)
            return;

        StartCoroutine(AbsorbEarth());
    }

    private IEnumerator AbsorbEarth()
    {
        absorbingEarth = true;

        // Prevent Game Over from interrupting the
        // victory animation.
        GameManager3D gameManager =
            FindFirstObjectByType<GameManager3D>();

        if (gameManager != null)
            gameManager.enabled = false;

        if (fieldController != null)
            fieldController.enabled = false;

        // Switch from physics to our orbital animation.
        earth.linearVelocity = Vector3.zero;
        earth.angularVelocity = Vector3.zero;
        earth.isKinematic = true;
        earth.useGravity = false;
        earth.detectCollisions = false;

        Vector3 initialScale = earth.transform.localScale;
        Vector3 initialOffset = earth.position - transform.position;

        // Find Earth's initial position around the Sun
        // in the Sun's own coordinate system.
        Vector3 localOffset =
            transform.InverseTransformVector(initialOffset);

        Vector3 flatOffset = new Vector3(
            localOffset.x,
            0f,
            localOffset.z
        );

        float initialRadius = flatOffset.magnitude;

        float initialAngle = Mathf.Atan2(
            flatOffset.z,
            flatOffset.x
        );

        float initialHeight = localOffset.y;

        float elapsed = 0f;

        while (elapsed < absorptionDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / Mathf.Max(absorptionDuration, 0.01f)
            );

            // Radius and size gradually decrease.
            float shrink = Mathf.SmoothStep(0f, 1f, t);

            float radius = Mathf.Lerp(
                initialRadius,
                0f,
                shrink
            );

            // Orbit faster as Earth approaches the center.
            float orbitProgress =
                t * t * (3f - 2f * t);

            float angle = initialAngle +
                orbitProgress * absorptionOrbits *
                Mathf.PI * 2f;

            float height = Mathf.Lerp(
                initialHeight,
                0f,
                shrink
            );

            Vector3 orbitalPosition = new Vector3(
                Mathf.Cos(angle) * radius,
                height,
                Mathf.Sin(angle) * radius
            );

            earth.position = transform.TransformPoint(
                orbitalPosition
            );

            earth.transform.localScale =
                initialScale * Mathf.Lerp(1f, 0.01f, shrink);

            // Earth spins while being absorbed.
            earth.transform.Rotate(
                Vector3.up,
                540f * Time.deltaTime,
                Space.Self
            );

            yield return null;
        }

        earth.gameObject.SetActive(false);

        // The player's Earth joins the display as planet number three.
        // Only create it AFTER absorption finishes.
        CreateVictoryEarth();

        hasWon = true;
        absorbingEarth = false;

        Debug.Log("YOU WIN! Earth was absorbed by the Sun.");
    }

    private void CreateVictoryEarth()
    {
        CreateOrbit(
            "Earth Orbit (Victory)",
            earthOrbitRadius,
            new Color(0.38f, 0.85f, 0.95f)
        );

        victoryEarthPlanet = CreatePlanet(
            "Earth (Third Planet)",
            earthPlanetRadius,
            new Color(0.1f, 0.65f, 1f)
        );

        // Place it immediately on its new orbit.
        victoryEarthAngle = 245f;
        victoryEarthPlanet.localPosition = new Vector3(
            Mathf.Cos(victoryEarthAngle * Mathf.Deg2Rad) * earthOrbitRadius,
            0f,
            Mathf.Sin(victoryEarthAngle * Mathf.Deg2Rad) * earthOrbitRadius
        );
    }

    private void UpdatePlanetPositions()
    {
        if (innerPlanet != null)
        {
            float angle = innerAngle * Mathf.Deg2Rad;

            innerPlanet.localPosition = new Vector3(
                Mathf.Cos(angle) * innerOrbitRadius,
                0f,
                Mathf.Sin(angle) * innerOrbitRadius
            );
        }

        if (outerPlanet != null)
        {
            float angle = outerAngle * Mathf.Deg2Rad;

            outerPlanet.localPosition = new Vector3(
                Mathf.Cos(angle) * outerOrbitRadius,
                0f,
                Mathf.Sin(angle) * outerOrbitRadius
            );
        }
    }

    private void CreateSun()
    {
        GameObject sun =
            GameObject.CreatePrimitive(PrimitiveType.Sphere);

        sun.name = "Sun";
        sun.transform.SetParent(transform, false);
        sun.transform.localPosition = Vector3.zero;
        sun.transform.localScale =
            Vector3.one * sunRadius * 2f;

        Destroy(sun.GetComponent<Collider>());

        Material material = CreateLitMaterial(
            new Color(1f, 0.58f, 0.08f)
        );

        material.EnableKeyword("_EMISSION");

        material.SetColor(
            "_EmissionColor",
            new Color(1f, 0.48f, 0.07f) * 4f
        );

        sun.GetComponent<MeshRenderer>().material =
            material;

        GameObject lightObject = new GameObject("Sun Light");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = Vector3.zero;

        Light sunLight = lightObject.AddComponent<Light>();

        sunLight.type = LightType.Point;
        sunLight.color = new Color(1f, 0.69f, 0.32f);
        sunLight.intensity = lightIntensity;
        sunLight.range = lightRange;
        sunLight.shadows = LightShadows.None;
    }

    private void CreateOrbit(
        string orbitName,
        float radius,
        Color color
    )
    {
        GameObject orbitObject = new GameObject(orbitName);
        orbitObject.transform.SetParent(transform, false);

        LineRenderer line =
            orbitObject.AddComponent<LineRenderer>();

        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = orbitSegments;
        line.startWidth = orbitLineWidth;
        line.endWidth = orbitLineWidth;
        line.numCornerVertices = 4;

        line.material = CreateUnlitMaterial(color);

        for (int i = 0; i < orbitSegments; i++)
        {
            float angle =
                i * Mathf.PI * 2f / orbitSegments;

            line.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            ));
        }
    }

    private Transform CreatePlanet(
        string planetName,
        float radius,
        Color color
    )
    {
        GameObject planet =
            GameObject.CreatePrimitive(PrimitiveType.Sphere);

        planet.name = planetName;
        planet.transform.SetParent(transform, false);
        planet.transform.localScale =
            Vector3.one * radius * 2f;

        Destroy(planet.GetComponent<Collider>());

        planet.GetComponent<MeshRenderer>().material =
            CreateLitMaterial(color);

        return planet.transform;
    }

    private Material CreateLitMaterial(Color color)
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

    private Material CreateUnlitMaterial(Color color)
    {
        Shader shader = Shader.Find(
            "Universal Render Pipeline/Unlit"
        );

        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader);
        material.color = color;

        return material;
    }

    private void OnGUI()
    {
        if (!hasWon)
            return;

        float width = 280f;
        float height = 180f;

        Rect panel = new Rect(
            (Screen.width - width) / 2f,
            (Screen.height - height) / 2f,
            width,
            height
        );

        GUI.Box(panel, "");

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);

        titleStyle.fontSize = 30;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.yellow;

        GUI.Label(
            new Rect(panel.x, panel.y + 25f, width, 50f),
            "YOU WIN!",
            titleStyle
        );

        if (GUI.Button(
            new Rect(panel.x + 55f, panel.y + 105f, 170f, 45f),
            "RESTART"
        ))
        {
            SceneManager.LoadScene(
                SceneManager.GetActiveScene().name
            );
        }
    }
}
