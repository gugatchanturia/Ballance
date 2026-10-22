
using System.Collections;
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

    [Tooltip("Horizontal distance used to identify black-hole deaths.")]
    public float blackHoleDetectionRadius = 2.5f;

    [Header("Death Audio Timing")]
    [Range(0f, 1f)]
    public float blackHoleSoundDelayMultiplier = 0.5f;

    [Header("Level Progression")]
    [Tooltip("Exact scene name. Leave empty for the final level.")]
    public string nextSceneName = "";

    private bool gameOver;
    private bool victorySequenceStarted;
    private bool levelComplete;

    private bool showGameOver;
    private bool showVictory;
    private bool transitioning;

    private GameAudio3D gameAudio;
    private BlackHole[] blackHoles;

    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    private GUIStyle starStyle;
    private GUIStyle buttonStyle;

    private void Start()
    {
        gameAudio = GetComponent<GameAudio3D>();

        blackHoles = gravityField != null
            ? gravityField.GetComponentsInChildren<BlackHole>()
            : new BlackHole[0];
    }

    private void Update()
    {
        if (gameOver || victorySequenceStarted ||
            levelComplete || earth == null ||
            gravityField == null)
            return;

        Vector3 localEarth =
            gravityField.InverseTransformPoint(earth.position);

        float localFallThreshold =
            0.5f - fallDistance / gravityField.lossyScale.y;

        if (localEarth.y < localFallThreshold)
            TriggerGameOver();
    }

    private void TriggerGameOver()
    {
        if (gameOver || levelComplete || victorySequenceStarted)
            return;

        gameOver = true;

        if (fieldController != null)
            fieldController.enabled = false;

        bool fellIntoBlackHole = false;

        if (earth != null && gravityField != null &&
            blackHoles != null)
        {
            Vector3 earthLocal =
                gravityField.InverseTransformPoint(earth.position);

            foreach (BlackHole hole in blackHoles)
            {
                if (hole == null)
                    continue;

                Vector3 holeLocal =
                    gravityField.InverseTransformPoint(
                        hole.transform.position);

                Vector3 offset = earthLocal - holeLocal;
                offset.y = 0f;

                // Convert horizontal offset to world-space distance.
                float distance =
                    gravityField.TransformVector(offset).magnitude;

                if (distance < blackHoleDetectionRadius)
                {
                    fellIntoBlackHole = true;
                    break;
                }
            }
        }

        if (fellIntoBlackHole)
        {
            if (gameAudio != null)
                gameAudio.PlayBlackHole();

            float delay =
                gameAudio != null &&
                gameAudio.blackHole != null
                    ? gameAudio.blackHole.length *
                      blackHoleSoundDelayMultiplier
                    : 0f;

            StartCoroutine(
                ShowGameOverAfterDelay(delay, true)
            );
        }
        else
        {
            if (gameAudio != null)
                gameAudio.PlayLose();

            showGameOver = true;
        }

        Debug.Log(
            fellIntoBlackHole
                ? "GAME OVER: Earth entered a black hole!"
                : "GAME OVER: Earth fell off the field!"
        );
    }

    private IEnumerator ShowGameOverAfterDelay(
        float delay,
        bool playLoseSound)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        if (playLoseSound && gameAudio != null)
            gameAudio.PlayLose();

        showGameOver = true;
    }

    public void BeginVictorySequence()
    {
        if (gameOver || levelComplete)
            return;

        victorySequenceStarted = true;

        if (fieldController != null)
            fieldController.enabled = false;
    }

    public void CompleteVictory()
    {
        if (gameOver || levelComplete)
            return;

        victorySequenceStarted = true;
        levelComplete = true;

        VictoryCamera3D victoryCamera = null;

        if (Camera.main != null)
            victoryCamera =
                Camera.main.GetComponent<VictoryCamera3D>();

        StartCoroutine(
            ShowVictoryAfterCamera(victoryCamera)
        );

        Debug.Log("LEVEL COMPLETE!");
    }

    private IEnumerator ShowVictoryAfterCamera(
        VictoryCamera3D victoryCamera)
    {
        if (victoryCamera != null)
        {
            float duration =
                Mathf.Max(0f, victoryCamera.zoomDuration) + 0.1f;

            yield return new WaitForSecondsRealtime(duration);
        }

        showVictory = true;
    }

    public void RestartLevel()
    {
        if (transitioning)
            return;

        Scene currentScene = SceneManager.GetActiveScene();

        StartCoroutine(
            LoadSceneWithClick(currentScene.name)
        );
    }

    public void LoadNextLevel()
    {
        if (transitioning ||
            !levelComplete ||
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

        StartCoroutine(
            LoadSceneWithClick(nextSceneName)
        );
    }

    private IEnumerator LoadSceneWithClick(string sceneName)
    {
        transitioning = true;

        if (gameAudio != null && gameAudio.button != null)
        {
            // Let the click sound survive the scene transition.
            GameObject soundObject =
                new GameObject("Button Click Sound");

            DontDestroyOnLoad(soundObject);

            AudioSource source =
                soundObject.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = gameAudio.masterVolume;

            source.PlayOneShot(gameAudio.button);

            // Clean up automatically after playback finishes.
            Destroy(
                soundObject,
                gameAudio.button.length + 0.1f
            );
        }

        // Scene changes immediately; no audio delay.
        SceneManager.LoadScene(sceneName);

        yield break;
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
        if (!showGameOver && !showVictory)
            return;

        InitializeStyles();

        bool hasNextLevel =
            !string.IsNullOrWhiteSpace(nextSceneName);

        float width = Mathf.Min(340f, Screen.width - 30f);
        float height = showVictory ? 310f : 200f;

        Rect panel = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height
        );

        GUI.Box(panel, "");

        if (showGameOver)
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

        float starSize = 52f;
        float spacing = 10f;
        float starsWidth = 3f * starSize + 2f * spacing;

        float startX =
            panel.x + (width - starsWidth) / 2f;

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
            new Rect(
                panel.x,
                panel.y + 135f,
                width,
                28f
            ),
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

        float restartY =
            hasNextLevel ? 242f : 200f;

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
