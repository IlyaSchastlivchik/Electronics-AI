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
        // ��������� AudioSource ���� ��� ���
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
        // ������������� ���� ��� ����������� �������
        PlayDisappearSound();
    }

    void OnDestroy()
    {
        // ������������� ���� ��� ����������� �������
        PlayDisappearSound();
    }

    public void PlayDisappearSound()
    {
        if (disappearSound != null)
        {
            // ���������� SoundManager ���� ��������, ����� ��������� AudioSource
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