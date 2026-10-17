
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlatformController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float maxTiltAngle = 25f;
    public float minHeight = -7f;
    public float maxHeight = 7f;

    private Rigidbody2D rb;
    private float leftHeight;
    private float rightHeight;
    private float platformLength;

    private float leftInput;
    private float rightInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // The platform begins horizontally.
        leftHeight = rb.position.y;
        rightHeight = rb.position.y;

        // Calculate its length in world units.
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        platformLength = box.size.x * Mathf.Abs(transform.lossyScale.x);
    }

    void Update()
    {
        // Read keyboard input independently for each endpoint.
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            leftInput = 0f;
            rightInput = 0f;
            return;
        }

        leftInput = 0f;
        rightInput = 0f;

        if (keyboard.wKey.isPressed) leftInput += 1f;
        if (keyboard.sKey.isPressed) leftInput -= 1f;

        if (keyboard.upArrowKey.isPressed) rightInput += 1f;
        if (keyboard.downArrowKey.isPressed) rightInput -= 1f;
    }

    void FixedUpdate()
    {
        // Move each endpoint's requested height.
        leftHeight += leftInput * moveSpeed * Time.fixedDeltaTime;
        rightHeight += rightInput * moveSpeed * Time.fixedDeltaTime;

        // Keep both endpoints inside the playable area.
        leftHeight = Mathf.Clamp(leftHeight, minHeight, maxHeight);
        rightHeight = Mathf.Clamp(rightHeight, minHeight, maxHeight);

        // Limit the height difference to prevent excessive tilt.
        float maxDifference =
            platformLength * Mathf.Sin(maxTiltAngle * Mathf.Deg2Rad);

        float difference = leftHeight - rightHeight;

        if (Mathf.Abs(difference) > maxDifference)
        {
            float center = (leftHeight + rightHeight) / 2f;

            difference = Mathf.Clamp(
                difference, -maxDifference, maxDifference);

            leftHeight = center + difference / 2f;
            rightHeight = center - difference / 2f;
        }

        // Calculate the center and angle of the rigid plank.
        float centerY = (leftHeight + rightHeight) / 2f;

        float angle = Mathf.Asin(
            (rightHeight - leftHeight) / platformLength)
            * Mathf.Rad2Deg;

        // Move through Unity's 2D physics system.
        rb.MovePosition(new Vector2(0f, centerY));
        rb.MoveRotation(angle);
    }
}
