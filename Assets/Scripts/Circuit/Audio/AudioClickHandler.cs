using UnityEngine;
using UnityEngine.UI;

public class AudioClickHandler : MonoBehaviour
{
    [Header("Sound Settings")]
    public AudioClip clickSound;
    [Range(0.0f, 1.0f)]
    public float volume = 1f;

    private AudioSource audioSource;

    void Start()
    {
        // Добавляем AudioSource если его нет
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.volume = volume;

        // Находим кнопку и добавляем обработчик звука
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(PlayClickSound);
        }
    }

    public void PlayClickSound()
    {
        // Используем SoundManager если доступен, иначе локальный AudioSource
        if (SoundManager.Instance != null && SoundManager.Instance.mouseClickSound != null)
        {
            SoundManager.Instance.PlayMouseClick();
        }
        else if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}