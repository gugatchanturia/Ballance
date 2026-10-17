
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class GravityFieldController : MonoBehaviour
{
    [Header("Tilt Settings")]
    public float tiltSpeed = 20f;
    public float maxTiltAngle = 20f;

    private Rigidbody rb;

    private float tiltX;
    private float tiltZ;

    private float forwardInput;
    private float sidewaysInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            forwardInput = 0f;
            sidewaysInput = 0f;
            return;
        }

        forwardInput = 0f;
        sidewaysInput = 0f;

        if (keyboard.wKey.isPressed ||
            keyboard.upArrowKey.isPressed)
            forwardInput += 1f;

        if (keyboard.sKey.isPressed ||
            keyboard.downArrowKey.isPressed)
            forwardInput -= 1f;

        if (keyboard.dKey.isPressed ||
            keyboard.rightArrowKey.isPressed)
            sidewaysInput += 1f;

        if (keyboard.aKey.isPressed ||
            keyboard.leftArrowKey.isPressed)
            sidewaysInput -= 1f;
    }

    void FixedUpdate()
    {
        // Update tilt angles using player input.
        tiltX += forwardInput * tiltSpeed * Time.fixedDeltaTime;
        tiltZ -= sidewaysInput * tiltSpeed * Time.fixedDeltaTime;

        // Restrict tilt in both axes.
        tiltX = Mathf.Clamp(tiltX, -maxTiltAngle, maxTiltAngle);
        tiltZ = Mathf.Clamp(tiltZ, -maxTiltAngle, maxTiltAngle);

        // Keep the current tilt when no input is pressed.
        Quaternion targetRotation =
            Quaternion.Euler(tiltX, 0f, tiltZ);

        rb.MoveRotation(targetRotation);
    }
}
