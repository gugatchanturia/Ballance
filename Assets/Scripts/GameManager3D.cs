
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

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
    private bool paused;
    private bool transitioning;

    private GameAudio3D gameAudio;
    private BlackHole[] blackHoles;

    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    private GUIStyle starStyle;
    private GUIStyle buttonStyle;
    private GUIStyle panelStyle;

    private Texture2D panelTexture;
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;

    private void Start()
    {
        Time.timeScale = 1f;

        gameAudio = GetComponent<GameAudio3D>();

        blackHoles = gravityField != null
            ? gravityField.GetComponentsInChildren<BlackHole>()
            : new BlackHole[0];
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame &&
            !gameOver && !victorySequenceStarted &&
            !levelComplete && !transitioning)
        {
            TogglePause();
        }

        if (paused || gameOver || victorySequenceStarted ||
            levelComplete || earth == null || gravityField == null)
            return;

        Vector3 localEarth =
            gravityField.InverseTransformPoint(earth.position);

        float localFallThreshold =
            0.5f - fallDistance / gravityField.lossyScale.y;

        if (localEarth.y < localFallThreshold)
            TriggerGameOver();
    }

    private void TogglePause()
    {
        paused = !paused;

        Time.timeScale = paused ? 0f : 1f;

        PlayButtonSound();

        Debug.Log(paused ? "GAME PAUSED" : "GAME RESUMED");
    }

    public void ResumeGame()
    {
        if (!paused)
            return;

        TogglePause();
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
                gameAudio != null && gameAudio.blackHole != null
                    ? gameAudio.blackHole.length *
                      blackHoleSoundDelayMultiplier
                    : 0f;

            StartCoroutine(ShowGameOverAfterDelay(delay));
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

    private IEnumerator ShowGameOverAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        if (gameAudio != null)
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

        // Save the best satellite rating for this level.
        string sceneName = SceneManager.GetActiveScene().name;

        for (int i = 1; i <= 5; i++)
        {
            string expectedScene =
                "Level" + i.ToString("00") + "-3D";

            if (sceneName != expectedScene)
                continue;

            int collected = satelliteCollector != null
                ? satelliteCollector.CollectedCount
                : 0;

            string completedKey = "Ballance_Completed_" + i;
            string starsKey = "Ballance_Stars_" + i;

            int previousBest = PlayerPrefs.GetInt(starsKey, 0);

            PlayerPrefs.SetInt(completedKey, 1);

            PlayerPrefs.SetInt(
                starsKey,
                Mathf.Max(previousBest, Mathf.Clamp(collected, 0, 3))
            );

            PlayerPrefs.Save();

            Debug.Log(
                "Level " + i + " completed. Best stars: " +
                PlayerPrefs.GetInt(starsKey)
            );

            break;
        }

        VictoryCamera3D victoryCamera = null;

        if (Camera.main != null)
            victoryCamera =
                Camera.main.GetComponent<VictoryCamera3D>();

        StartCoroutine(ShowVictoryAfterCamera(victoryCamera));

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

        StartCoroutine(
            LoadSceneWithClick(SceneManager.GetActiveScene().name)
        );
    }

    public void LoadNextLevel()
    {
        if (transitioning || !levelComplete ||
            string.IsNullOrWhiteSpace(nextSceneName))
            return;

        StartCoroutine(LoadSceneWithClick(nextSceneName));
    }

    public void ReturnToMainMenu()
    {
        if (transitioning)
            return;

        StartCoroutine(LoadSceneWithClick("MainMenu"));
    }

    private IEnumerator LoadSceneWithClick(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                "Scene missing from Build Profiles: " + sceneName
            );
            yield break;
        }

        transitioning = true;

        // Restore normal gameplay speed before changing scenes.
        Time.timeScale = 1f;
        paused = false;

        PlayButtonSound();

        // No waiting: scene changes immediately.
        SceneManager.LoadScene(sceneName);

        yield break;
    }

    private void PlayButtonSound()
    {
        if (gameAudio == null || gameAudio.button == null)
            return;

        // A temporary AudioSource lets the sound continue
        // after the current scene is unloaded.
        GameObject soundObject =
            new GameObject("Button Click Sound");

        DontDestroyOnLoad(soundObject);

        AudioSource source =
            soundObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = gameAudio.masterVolume;
        source.ignoreListenerPause = true;

        source.PlayOneShot(gameAudio.button);

        Destroy(soundObject, gameAudio.button.length + 0.1f);
    }

    private Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void InitializeStyles()
    {
        if (titleStyle != null)
            return;

        panelTexture = MakeTexture(
            new Color(0.045f, 0.075f, 0.13f, 0.94f)
        );

        buttonTexture = MakeTexture(
            new Color(0.12f, 0.20f, 0.31f, 1f)
        );

        buttonHoverTexture = MakeTexture(
            new Color(0.19f, 0.32f, 0.45f, 1f)
        );

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = panelTexture;
        panelStyle.border = new RectOffset(0, 0, 0, 0);

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 30;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor =
            new Color(1f, 0.82f, 0.43f);

        textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 16;
        textStyle.alignment = TextAnchor.MiddleCenter;
        textStyle.normal.textColor =
            new Color(0.76f, 0.86f, 0.98f);

        starStyle = new GUIStyle(GUI.skin.label);
        starStyle.fontSize = 43;
        starStyle.fontStyle = FontStyle.Bold;
        starStyle.alignment = TextAnchor.MiddleCenter;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 16;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.alignment = TextAnchor.MiddleCenter;

        buttonStyle.normal.background = buttonTexture;
        buttonStyle.hover.background = buttonHoverTexture;
        buttonStyle.active.background = buttonHoverTexture;

        buttonStyle.normal.textColor = Color.white;
        buttonStyle.hover.textColor =
            new Color(1f, 0.86f, 0.55f);
        buttonStyle.active.textColor = Color.white;
    }

    private bool DrawButton(
        Rect panel,
        float y,
        string label,
        float width = 220f)
    {
        return GUI.Button(
            new Rect(
                panel.x + (panel.width - width) / 2f,
                panel.y + y,
                width,
                45f
            ),
            label,
            buttonStyle
        );
    }

    private void OnGUI()
    {
        if (!paused && !showGameOver && !showVictory)
            return;

        InitializeStyles();

        if (paused)
        {
            DrawPauseMenu();
            return;
        }

        DrawResultMenu();
    }

    private void DrawPauseMenu()
    {
        float width = Mathf.Min(350f, Screen.width - 30f);
        float height = 315f;

        Rect panel = new Rect(
            (Screen.width - width) / 2f,
            (Screen.height - height) / 2f,
            width,
            height
        );

        GUI.Box(panel, "", panelStyle);

        GUI.Label(
            new Rect(panel.x, panel.y + 22f, width, 55f),
            "PAUSED",
            titleStyle
        );

        GUI.Label(
            new Rect(panel.x, panel.y + 70f, width, 28f),
            "PRESS ESC TO RESUME",
            textStyle
        );

        if (DrawButton(panel, 110f, "RESUME"))
            ResumeGame();

        if (DrawButton(panel, 172f, "RESTART LEVEL"))
            RestartLevel();

        if (DrawButton(panel, 234f, "MAIN MENU"))
            ReturnToMainMenu();
    }

    private void DrawResultMenu()
    {
        bool hasNextLevel =
            !string.IsNullOrWhiteSpace(nextSceneName);

        float width = Mathf.Min(350f, Screen.width - 30f);

        float height = showGameOver
            ? 275f
            : hasNextLevel ? 400f : 355f;

        Rect panel = new Rect(
            (Screen.width - width) / 2f,
            (Screen.height - height) / 2f,
            width,
            height
        );

        GUI.Box(panel, "", panelStyle);

        if (showGameOver)
        {
            GUI.Label(
                new Rect(panel.x, panel.y + 24f, width, 55f),
                "GAME OVER",
                titleStyle
            );

            GUI.Label(
                new Rect(panel.x, panel.y + 78f, width, 25f),
                "EARTH RECOVERY FAILED",
                textStyle
            );

            if (DrawButton(panel, 120f, "RESTART"))
                RestartLevel();

            if (DrawButton(panel, 185f, "MAIN MENU"))
                ReturnToMainMenu();

            return;
        }

        GUI.Label(
            new Rect(panel.x, panel.y + 20f, width, 45f),
            hasNextLevel ? "LEVEL COMPLETE!" : "GAME COMPLETE!",
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
            starStyle.normal.textColor = i < collected
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
            new Rect(panel.x, panel.y + 139f, width, 28f),
            "SATELLITES RECOVERED: " +
            collected + "/" + total,
            textStyle
        );

        if (hasNextLevel)
        {
            if (DrawButton(panel, 191f, "NEXT LEVEL"))
                LoadNextLevel();

            if (DrawButton(panel, 253f, "RESTART"))
                RestartLevel();

            if (DrawButton(panel, 315f, "MAIN MENU"))
                ReturnToMainMenu();
        }
        else
        {
            if (DrawButton(panel, 192f, "RESTART"))
                RestartLevel();

            if (DrawButton(panel, 257f, "MAIN MENU"))
                ReturnToMainMenu();
        }
    }

    private void OnDestroy()
    {
        // Prevent leaving the game frozen if this object
        // is destroyed while the pause menu is open.
        if (paused)
            Time.timeScale = 1f;

        if (panelTexture != null)
            Destroy(panelTexture);

        if (buttonTexture != null)
            Destroy(buttonTexture);

        if (buttonHoverTexture != null)
            Destroy(buttonHoverTexture);
    }
}
