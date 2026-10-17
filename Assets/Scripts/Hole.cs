
using UnityEngine;

public class Hole : MonoBehaviour
{
    [Header("Hole Type")]
    public bool isGoal = false;

    [Header("Detection")]
    public float captureRadius = 0.25f;

    void OnTriggerStay2D(Collider2D other)
    {
        BallController ball = other.GetComponent<BallController>();

        // Ignore objects that are not the ball.
        if (ball == null)
        {
            return;
        }

        float distance = Vector2.Distance(
            other.transform.position,
            transform.position
        );

        // Capture the ball only when sufficiently centered.
        if (distance <= captureRadius)
        {
            ball.CaptureByHole(transform.position, isGoal);
        }
    }
}
