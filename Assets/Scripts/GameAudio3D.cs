
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameAudio3D : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip button;
    public AudioClip lose;
    public AudioClip solarSuction;
    public AudioClip blackHole;
    public AudioClip win;
    public AudioClip itemPickup;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float masterVolume = 0.8f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void Play(AudioClip clip)
    {
        if (clip == null)
            return;

        audioSource.PlayOneShot(clip, masterVolume);
    }

    public void PlayButton() => Play(button);
    public void PlayLose() => Play(lose);
    public void PlaySolarSuction() => Play(solarSuction);
    public void PlayBlackHole() => Play(blackHole);
    public void PlayWin() => Play(win);
    public void PlayPickup() => Play(itemPickup);
}
