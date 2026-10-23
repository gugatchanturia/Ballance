
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class BallanceMenu : MonoBehaviour
{
    [Header("How To Play Screenshots")]
    public Texture2D controlsImage;
    public Texture2D satellitesImage;
    public Texture2D blackHolesImage;
    public Texture2D sunImage;

    [Header("Audio")]
    public AudioClip buttonSound;
    [Range(0f, 1f)]
    public float buttonVolume = 0.8f;

    private readonly string[] levelScenes =
    {
        "Level01-3D",
        "Level02-3D",
        "Level03-3D",
        "Level04-3D",
        "Level05-3D"
    };

    private readonly string[] tutorialTitles =
    {
        "TILT THE GRID",
        "COLLECT SATELLITES",
        "AVOID BLACK HOLES",
        "REACH THE SUN"
    };

    private Texture2D[] tutorialImages;

    private enum MenuPage
    {
        Home,
        Levels,
        Tutorial
    }

    private MenuPage currentPage = MenuPage.Home;
    private int tutorialPage;

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle textStyle;
    private GUIStyle buttonStyle;
    private GUIStyle disabledStyle;
    private GUIStyle starStyle;
    private GUIStyle panelStyle;

    private void Awake()
    {
        Time.timeScale = 1f;

        tutorialImages = new Texture2D[]
        {
            controlsImage,
            satellitesImage,
            blackHolesImage,
            sunImage
        };
    }

    private void Update()
    {
        if (Keyboard.current != null &&
        Keyboard.current.escapeKey.wasPressedThisFrame &&
        currentPage != MenuPage.Home)
        {
            currentPage = MenuPage.Home;
            PlayButtonSound();
        }
    }

    private void InitializeStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 38;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor =
            new Color(1f, 0.80f, 0.40f);

        subtitleStyle = new GUIStyle(GUI.skin.label);
        subtitleStyle.fontSize = 17;
        subtitleStyle.alignment = TextAnchor.MiddleCenter;
        subtitleStyle.normal.textColor =
            new Color(0.65f, 0.78f, 0.92f);

        textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 18;
        textStyle.alignment = TextAnchor.MiddleCenter;
        textStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 19;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.alignment = TextAnchor.MiddleCenter;

        disabledStyle = new GUIStyle(buttonStyle);
        disabledStyle.normal.textColor = Color.gray;

        starStyle = new GUIStyle(GUI.skin.label);
        starStyle.fontSize = 24;
        starStyle.alignment = TextAnchor.MiddleCenter;
        starStyle.normal.textColor =
            new Color(1f, 0.78f, 0.30f);

        panelStyle = new GUIStyle(GUI.skin.box);
    }

    private void OnGUI()
    {
        InitializeStyles();

        // Scale the menu uniformly for smaller portrait windows.
        float scale = Mathf.Min(
            Screen.width / 600f,
            Screen.height / 800f
        );

        scale = Mathf.Max(scale, 0.2f);

        Matrix4x4 originalMatrix = GUI.matrix;

        GUI.matrix = Matrix4x4.Scale(
            new Vector3(scale, scale, 1f)
        );

        float width = Screen.width / scale;
        float height = Screen.height / scale;

        float centerX = width * 0.5f;

        switch (currentPage)
        {
            case MenuPage.Home:
                DrawHome(centerX, height);
                break;

            case MenuPage.Levels:
                DrawLevels(centerX, height);
                break;

            case MenuPage.Tutorial:
                DrawTutorial(width, height);
                break;
        }

        GUI.matrix = originalMatrix;
    }

    private void DrawHome(float cx, float height)
    {
        GUI.Label(
            new Rect(cx - 280f, height * 0.19f, 560f, 70f),
            "B A L L A N C E",
            titleStyle
        );

        GUI.Label(
            new Rect(cx - 260f, height * 0.28f, 520f, 35f),
            "EARTH RECOVERY PROTOCOL",
            subtitleStyle
        );

        float buttonY = height * 0.43f;

        if (DrawButton(cx, buttonY, "PLAY / LEVEL SELECT"))
        {
            currentPage = MenuPage.Levels;
            PlayButtonSound();
        }

        if (DrawButton(cx, buttonY + 85f, "HOW TO PLAY"))
        {
            tutorialPage = 0;
            currentPage = MenuPage.Tutorial;
            PlayButtonSound();
        }

        if (DrawButton(cx, buttonY + 170f, "QUIT"))
        {
            PlayButtonSound();
            Application.Quit();
        }

        GUI.Label(
            new Rect(cx - 250f, height - 75f, 500f, 30f),
            "5 LEVELS  /  15 STARS",
            subtitleStyle
        );
    }

    private void DrawLevels(float cx, float height)
    {
        GUI.Label(
            new Rect(cx - 250f, 35f, 500f, 55f),
            "SELECT LEVEL",
            titleStyle
        );

        GUI.Label(
            new Rect(cx - 260f, 95f, 520f, 35f),
            "COMPLETE LEVELS TO UNLOCK THE NEXT",
            subtitleStyle
        );

        float top = 155f;

        for (int i = 0; i < levelScenes.Length; i++)
        {
            int levelNumber = i + 1;

            bool unlocked = i == 0 ||
                PlayerPrefs.GetInt(
                    "Ballance_Completed_" + i, 0
                ) == 1;

            bool completed = PlayerPrefs.GetInt(
                "Ballance_Completed_" + levelNumber, 0
            ) == 1;

            int stars = PlayerPrefs.GetInt(
                "Ballance_Stars_" + levelNumber, 0
            );

            float y = top + i * 104f;

            GUI.Box(
                new Rect(cx - 230f, y, 460f, 94f),
                "",
                panelStyle
            );

            if (unlocked)
            {
                if (GUI.Button(
                    new Rect(cx - 210f, y + 10f, 260f, 74f),
                    "LEVEL " + levelNumber.ToString("00"),
                    buttonStyle
                ))
                {
                    PlayButtonSound();
                    LoadLevel(levelScenes[i]);
                }

                string rating = completed
                    ? BuildStars(stars)
                    : "NOT COMPLETED";

                GUI.Label(
                    new Rect(cx + 55f, y + 23f, 160f, 48f),
                    rating,
                    completed ? starStyle : subtitleStyle
                );
            }
            else
            {
                GUI.Label(
                    new Rect(cx - 205f, y + 15f, 410f, 60f),
                    "LEVEL " +
                    levelNumber.ToString("00") +
                    "   -   LOCKED",
                    disabledStyle
                );
            }
        }

        DrawBackButton(cx, height - 80f);
    }

    private string BuildStars(int count)
    {
        string result = "";

        for (int i = 0; i < 3; i++)
            result += i < count ? "\u2605" : "\u2606";

        return result;
    }

    private void DrawTutorial(float width, float height)
    {
        float cx = width * 0.5f;

        GUI.Label(
            new Rect(cx - 280f, 15f, 560f, 55f),
            "HOW TO PLAY",
            titleStyle
        );

        GUI.Label(
            new Rect(cx - 250f, 70f, 500f, 35f),
            (tutorialPage + 1) + " / 4  -  " +
            tutorialTitles[tutorialPage],
            subtitleStyle
        );

        Texture2D image = tutorialImages[tutorialPage];

        Rect imageArea = new Rect(
            25f,
            120f,
            width - 50f,
            Mathf.Max(50f, height - 255f)
        );

        if (image != null)
        {
            GUI.DrawTexture(
                imageArea,
                image,
                ScaleMode.ScaleToFit
            );
        }
        else
        {
            GUI.Box(imageArea, "IMAGE NOT ASSIGNED");
        }

        float navY = height - 120f;

        if (tutorialPage > 0)
        {
            if (GUI.Button(
                new Rect(cx - 245f, navY, 150f, 52f),
                "PREVIOUS",
                buttonStyle
            ))
            {
                tutorialPage--;
                PlayButtonSound();
            }
        }

        if (tutorialPage < tutorialImages.Length - 1)
        {
            if (GUI.Button(
                new Rect(cx + 95f, navY, 150f, 52f),
                "NEXT",
                buttonStyle
            ))
            {
                tutorialPage++;
                PlayButtonSound();
            }
        }

        DrawBackButton(cx, height - 60f);
    }

    private bool DrawButton(
        float centerX,
        float y,
        string label)
    {
        return GUI.Button(
            new Rect(centerX - 190f, y, 380f, 65f),
            label,
            buttonStyle
        );
    }

    private void DrawBackButton(float cx, float y)
    {
        if (GUI.Button(
            new Rect(cx - 105f, y, 210f, 45f),
            "BACK",
            buttonStyle
        ))
        {
            currentPage = MenuPage.Home;
            PlayButtonSound();
        }
    }

    private void LoadLevel(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError(
                "Scene missing from Build Profiles: " + sceneName
            );
        }
    }

    private void PlayButtonSound()
    {
        if (buttonSound == null)
            return;

        GameObject soundObject =
            new GameObject("Menu Button Sound");

        DontDestroyOnLoad(soundObject);

        AudioSource source =
            soundObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = buttonVolume;

        source.PlayOneShot(buttonSound);

        Destroy(soundObject, buttonSound.length + 0.1f);
    }
}
