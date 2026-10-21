
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager3D : MonoBehaviour
{
    [Header("Gameplay References")]
    public Rigidbody earth;
    public Transform gravityField;
    public GravityFieldController fieldController;
    public EarthSatelliteCollector satelliteCollector;

    [Header("Fall Detection")]
    public float fallDistance = 3f;

    [Header("Level Progression")]
    [Tooltip("Exact scene name. Leave empty for the final level.")]
    public string nextSceneName = "";

    private bool gameOver;
    private bool victorySequenceStarted;
    private bool levelComplete;

    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    private GUIStyle starStyle;
    private GUIStyle buttonStyle;

    private void Update()
    {
        if (gameOver || victorySequenceStarted ||
            levelComplete || earth == null ||
            gravityField == null)
            return;

        // Detect falling below the tilted field.
        Vector3 localEarth =
            gravityField.InverseTransformPoint(
                earth.position
            );

        float localFallThreshold =
            0.5f - fallDistance / gravityField.lossyScale.y;

        if (localEarth.y < localFallThreshold)
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        if (gameOver || levelComplete ||
            victorySequenceStarted)
            return;

        gameOver = true;

        if (fieldController != null)
            fieldController.enabled = false;

        Debug.Log("GAME OVER: Earth fell off the gravity field!");
    }

    // Called when Earth enters the Sun.
    // We wait until the absorption animation is finished
    // before displaying the victory screen.
    public void BeginVictorySequence()
    {
        if (gameOver || levelComplete)
            return;

        victorySequenceStarted = true;

        if (fieldController != null)
            fieldController.enabled = false;
    }

    // Called after Earth finishes being absorbed.
    public void CompleteVictory()
    {
        if (gameOver || levelComplete)
            return;

        victorySequenceStarted = true;
        levelComplete = true;

        Debug.Log("LEVEL COMPLETE!");
    }

    public void RestartLevel()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void LoadNextLevel()
    {
        if (!levelComplete ||
            string.IsNullOrWhiteSpace(nextSceneName))
            return;

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError(
                "Next scene is missing from Build Profiles: " +
                nextSceneName
            );
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void InitializeStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 30;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor =
            new Color(1f, 0.85f, 0.35f);

        textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 16;
        textStyle.alignment = TextAnchor.MiddleCenter;
        textStyle.normal.textColor =
            new Color(0.85f, 0.92f, 1f);

        starStyle = new GUIStyle(GUI.skin.label);
        starStyle.fontSize = 43;
        starStyle.fontStyle = FontStyle.Bold;
        starStyle.alignment = TextAnchor.MiddleCenter;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 16;
        buttonStyle.fontStyle = FontStyle.Bold;
    }

    private void OnGUI()
    {
        if (!gameOver && !levelComplete)
            return;

        InitializeStyles();

        bool hasNextLevel =
            !string.IsNullOrWhiteSpace(nextSceneName);

        float width = Mathf.Min(340f, Screen.width - 30f);
        float height = levelComplete ? 310f : 200f;

        Rect panel = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height
        );

        GUI.Box(panel, "");

        if (gameOver)
        {
            GUI.Label(
                new Rect(panel.x, panel.y + 25f, width, 55f),
                "GAME OVER",
                titleStyle
            );

            if (GUI.Button(
                new Rect(
                    panel.x + 60f,
                    panel.y + 115f,
                    width - 120f,
                    48f
                ),
                "RESTART",
                buttonStyle
            ))
            {
                RestartLevel();
            }

            return;
        }

        string heading = hasNextLevel
            ? "LEVEL COMPLETE!"
            : "GAME COMPLETE!";

        GUI.Label(
            new Rect(panel.x, panel.y + 20f, width, 45f),
            heading,
            titleStyle
        );

        int collected = satelliteCollector != null
            ? satelliteCollector.CollectedCount
            : 0;

        int total = satelliteCollector != null
            ? satelliteCollector.TotalCount
            : 3;

        // Up to three illuminated stars.
        float starSize = 52f;
        float spacing = 10f;
        float starsWidth = 3f * starSize + 2f * spacing;
        float startX = panel.x + (width - starsWidth) / 2f;

        for (int i = 0; i < 3; i++)
        {
            bool lit = i < collected;

            starStyle.normal.textColor = lit
                ? new Color(1f, 0.83f, 0.25f)
                : new Color(0.30f, 0.35f, 0.44f);

            GUI.Label(
                new Rect(
                    startX + i * (starSize + spacing),
                    panel.y + 75f,
                    starSize,
                    starSize
                ),
                "\u2605",
                starStyle
            );
        }

        GUI.Label(
            new Rect(panel.x, panel.y + 135f, width, 28f),
            "SATELLITES RECOVERED: " +
            collected + "/" + total,
            textStyle
        );

        if (hasNextLevel)
        {
            if (GUI.Button(
                new Rect(
                    panel.x + 60f,
                    panel.y + 185f,
                    width - 120f,
                    42f
                ),
                "NEXT LEVEL",
                buttonStyle
            ))
            {
                LoadNextLevel();
            }
        }

        float restartY = hasNextLevel ? 242f : 200f;

        if (GUI.Button(
            new Rect(
                panel.x + 60f,
                panel.y + restartY,
                width - 120f,
                42f
            ),
            "RESTART",
            buttonStyle
        ))
        {
            RestartLevel();
        }
    }
}
