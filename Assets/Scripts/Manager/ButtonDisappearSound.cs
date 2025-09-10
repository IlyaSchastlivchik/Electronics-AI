using UnityEngine;

public class ButtonDisappearSound : MonoBehaviour
{
    [Header("Sound Settings")]
    public AudioClip disappearSound;
    [Range(0.0f, 1.0f)]
    public float volume = 1f;

    private AudioSource audioSource;

    void Awake()
    {
        // Добавляем AudioSource если его нет
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.volume = volume;
    }

    void OnDisable()
    {
        // Воспроизводим звук при деактивации объекта
        PlayDisappearSound();
    }

    void OnDestroy()
    {
        // Воспроизводим звук при уничтожении объекта
        PlayDisappearSound();
    }

    public void PlayDisappearSound()
    {
        if (disappearSound != null)
        {
            // Используем SoundManager если доступен, иначе локальный AudioSource
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySound(disappearSound, volume);
            }
            else if (audioSource != null)
            {
                audioSource.PlayOneShot(disappearSound, volume);
            }
        }
    }
}