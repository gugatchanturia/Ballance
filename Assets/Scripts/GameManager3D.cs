
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager3D : MonoBehaviour
{
    [Header("References")]
    public Rigidbody earth;
    public Transform gravityField;
    public GravityFieldController fieldController;

    [Header("Fall Detection")]
    public float fallDistance = 3f;

    private bool gameOver;

    void Update()
    {
        if (gameOver || earth == null || gravityField == null)
            return;

        // Detect falling relative to the tilted field,
        // not relative to the world's horizontal plane.
        Vector3 localEarth = gravityField.InverseTransformPoint(
            earth.position
        );

        // GravityField is a cube scaled to 0.2 units thick,
        // so its top surface is at local Y = 0.5.
        float localFallThreshold =
            0.5f - fallDistance / gravityField.lossyScale.y;

        if (localEarth.y < localFallThreshold)
        {
            TriggerGameOver();
        }
    }

    void TriggerGameOver()
    {
        gameOver = true;

        if (fieldController != null)
            fieldController.enabled = false;

        Debug.Log("GAME OVER: Earth fell off the gravity field!");
    }

    void OnGUI()
    {
        if (!gameOver)
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

        GUI.Label(
            new Rect(panel.x, panel.y + 25f, width, 50f),
            "GAME OVER",
            titleStyle
        );

        if (GUI.Button(
            new Rect(panel.x + 55f, panel.y + 105f, 170f, 45f),
            "RESTART"
        ))
        {
            RestartLevel();
        }
    }

    public void RestartLevel()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}
