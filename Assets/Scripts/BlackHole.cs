
using System.Collections;
using UnityEngine;

public class BlackHole : MonoBehaviour
{
    [Header("Target")]
    public Rigidbody earth;

    [Header("Gravity")]
    public float influenceRadius = 3f;
    public float gravityStrength = 10f;

    [Header("Capture")]
    public float captureRadius = 0.45f;
    public float swallowDuration = 0.7f;
    public float swallowDepth = 1.6f;

    private bool captured;

    void FixedUpdate()
    {
        if (captured || earth == null)
            return;

        Transform field = transform.parent;

        // Compare Earth and black hole in the field's
        // coordinate system, ignoring their height difference.
        Vector3 localEarth =
            field.InverseTransformPoint(earth.position);

        Vector3 localHole = transform.localPosition;

        Vector2 difference = new Vector2(
            localHole.x - localEarth.x,
            localHole.z - localEarth.z
        );

        Vector3 worldOffset = field.TransformVector(
            new Vector3(difference.x, 0f, difference.y)
        );

        float distance = worldOffset.magnitude;

        if (distance <= captureRadius)
        {
            CaptureEarth();
            return;
        }

        if (distance >= influenceRadius)
            return;

        float proximity = 1f - distance / influenceRadius;

        float acceleration =
            gravityStrength * proximity * proximity;

        earth.AddForce(
            worldOffset.normalized * acceleration,
            ForceMode.Acceleration
        );
    }

    void CaptureEarth()
    {
        captured = true;
        StartCoroutine(SwallowEarth());
    }

    IEnumerator SwallowEarth()
    {
        // Stop normal physics so the animation can take over.
        earth.linearVelocity = Vector3.zero;
        earth.angularVelocity = Vector3.zero;

        earth.isKinematic = true;
        earth.detectCollisions = false;

        Transform earthTransform = earth.transform;

        Vector3 startPosition = earthTransform.position;
        Vector3 startScale = earthTransform.localScale;

        // The destination is below the hole, measured
        // along the tilting field's downward direction.
        Transform field = transform.parent;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, swallowDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth movement into the opening.
            float eased = t * t * (3f - 2f * t);

            Vector3 destination =
                transform.position - field.up * swallowDepth;

            earthTransform.position = Vector3.Lerp(
                startPosition,
                destination,
                eased
            );

            earthTransform.localScale = Vector3.Lerp(
                startScale,
                startScale * 0.05f,
                eased
            );

            yield return null;
        }

        earth.gameObject.SetActive(false);

        Debug.Log(
            "GAME OVER: Earth was swallowed by a black hole!"
        );
    }
}
