
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EarthRollingAudio : MonoBehaviour
{
    [Header("Rolling Sound")]
    public AudioClip rollingClip;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float maxVolume = 0.3f;

    public float minSpeed = 0.25f;
    public float maxSpeed = 8f;
    public float fadeSpeed = 4f;

    [Header("Pitch")]
    public float minPitch = 0.8f;
    public float maxPitch = 1.15f;

    [Header("Ground Detection")]
    public LayerMask groundLayers = ~0;
    public float groundCheckDistance = 0.45f;

    private Rigidbody rb;
    private AudioSource audioSource;

    private bool gameEnded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0f;
        audioSource.clip = rollingClip;
    }

    private void Start()
    {
        if (rollingClip != null)
        {
            audioSource.clip = rollingClip;
            audioSource.Play();
        }
    }

    private void Update()
    {
        if (rb == null || audioSource == null)
            return;

        if (rollingClip != audioSource.clip)
        {
            audioSource.clip = rollingClip;

            if (rollingClip != null)
                audioSource.Play();
        }

        // Stop rolling audio when physics is disabled,
        // such as during the Sun absorption animation.
        if (rb.isKinematic || gameEnded)
        {
            FadeTo(0f);
            return;
        }

        // Earth must be touching the field.
        bool grounded = Physics.Raycast(
            transform.position,
            Vector3.down,
            groundCheckDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );

        float speed = grounded
            ? rb.linearVelocity.magnitude
            : 0f;

        float t = Mathf.InverseLerp(
            minSpeed,
            maxSpeed,
            speed
        );

        float targetVolume = t * maxVolume;

        FadeTo(targetVolume);

        audioSource.pitch = Mathf.Lerp(
            minPitch,
            maxPitch,
            t
        );
    }

    private void FadeTo(float targetVolume)
    {
        audioSource.volume = Mathf.MoveTowards(
            audioSource.volume,
            targetVolume,
            fadeSpeed * Time.deltaTime
        );
    }

    public void StopRolling()
    {
        gameEnded = true;
    }
}
