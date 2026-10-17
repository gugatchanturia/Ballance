
using System.Collections;
using UnityEngine;

public class BallController : MonoBehaviour
{
    [Header("UI")]
    public GameObject restartButton;

    [Header("Fall Detection")]
    public float fallBoundaryY = -7.5f;

    [Header("Hole Animation")]
    public float sinkDuration = 0.5f;

    private Rigidbody2D rb;
    private PlatformController platformController;
    private bool gameOver = false;

    void Start()
    {
        if (restartButton != null)
        {
            restartButton.SetActive(false);
        }
        rb = GetComponent<Rigidbody2D>();
        platformController = FindAnyObjectByType<PlatformController>();
    }

    void Update()
    {
        if (gameOver)
        {
            return;
        }

        // Detect when the ball falls below the play area.
        if (transform.position.y < fallBoundaryY)
        {
            EndGame("GAME OVER: Ball fell!");
        }
    }

    // Called by a hole when the ball reaches its center.
    public void CaptureByHole(Vector3 holePosition, bool isGoal)
    {
        if (gameOver)
        {
            return;
        }

        // Mark the game as over immediately to prevent
        // multiple holes or fall detection from triggering.
        gameOver = true;

        StopGameplay();

        StartCoroutine(SinkIntoHole(holePosition, isGoal));
    }

    IEnumerator SinkIntoHole(Vector3 holePosition, bool isGoal)
    {
        Vector3 startPosition = transform.position;
        Vector3 startScale = transform.localScale;

        // Preserve the ball's Z coordinate.
        holePosition.z = startPosition.z;

        float elapsed = 0f;

        while (elapsed < sinkDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / sinkDuration);

            // Move the ball toward the hole's center.
            transform.position = Vector3.Lerp(
                startPosition,
                holePosition,
                t
            );

            // Gradually shrink the ball.
            transform.localScale = Vector3.Lerp(
                startScale,
                Vector3.zero,
                t
            );

            yield return null;
        }

        transform.position = holePosition;
        transform.localScale = Vector3.zero;

        ShowRestartButton();

        if (isGoal)
        {
            Debug.Log("LEVEL COMPLETE!");
        }
        else
        {
            Debug.Log("GAME OVER: Ball entered a dangerous hole!");
        }
    }

    void StopGameplay()
    {
        // Stop the ball's physics simulation.
        rb.simulated = false;

        // Disable platform keyboard controls.
        if (platformController != null)
        {
            platformController.enabled = false;
        }
    }

    void EndGame(string message)
    {
        if (gameOver)
        {
            return;
        }

        gameOver = true;

        StopGameplay();

        ShowRestartButton();

        Debug.Log(message);
    }
    void ShowRestartButton()
    {
        if (restartButton != null)
        {
            restartButton.SetActive(true);
        }
    }
}
