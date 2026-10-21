
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class VictoryCamera3D : MonoBehaviour
{
    [Header("Victory Camera")]
    public float zoomDuration = 2.5f;

    [Tooltip("Distance between the camera and the Sun after zooming.")]
    public float finalDistance = 10f;

    [Tooltip("How far above the Sun to aim.")]
    public float focusHeight = 0f;

    private Coroutine zoomCoroutine;

    public void StartVictoryFocus(Transform sun)
    {
        if (sun == null)
            return;

        if (zoomCoroutine != null)
            StopCoroutine(zoomCoroutine);

        zoomCoroutine = StartCoroutine(ZoomTowardSun(sun));
    }

    private IEnumerator ZoomTowardSun(Transform sun)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        // Preserve the camera's existing viewing direction
        // instead of switching to a top-down view.
        Vector3 direction =
            (transform.position - sun.position).normalized;

        Vector3 targetPosition =
            sun.position + direction * finalDistance;

        Vector3 focusPoint =
            sun.position + sun.up * focusHeight;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                focusPoint - targetPosition,
                Vector3.up
            );

        float elapsed = 0f;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / Mathf.Max(zoomDuration, 0.01f)
            );

            // Smooth acceleration and deceleration.
            t = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

            transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                t
            );

            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;

        zoomCoroutine = null;
    }
}
