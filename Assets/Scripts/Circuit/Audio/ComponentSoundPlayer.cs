using UnityEngine;
using System;

[RequireComponent(typeof(DraggableComponent))]
public class ComponentSoundPlayer : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip grabClip;
    [SerializeField] private AudioClip dragClip;
    [SerializeField] private AudioClip dropClip;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource oneShotSource;
    [SerializeField] private AudioSource loopSource;

    private DraggableComponent draggable;
    private bool isDragging;

    private void Awake()
    {
        draggable = GetComponent<DraggableComponent>();
        InitializeAudioSources();

        // Подписываемся на события DraggableComponent
        draggable.OnGrab += HandleGrab;
        draggable.OnDrag += HandleDrag;
        draggable.OnDrop += HandleDrop;
    }

    private void InitializeAudioSources()
    {
        if (oneShotSource == null)
            oneShotSource = gameObject.AddComponent<AudioSource>();

        if (loopSource == null)
        {
            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.loop = true;
        }

        // Настройки для источников звука
        oneShotSource.playOnAwake = false;
        loopSource.playOnAwake = false;
    }

    private void HandleGrab()
    {
        if (grabClip != null)
            oneShotSource.PlayOneShot(grabClip);

        isDragging = true;
    }

    private void HandleDrag()
    {
        if (!isDragging) return;

        if (dragClip != null && !loopSource.isPlaying)
        {
            loopSource.clip = dragClip;
            loopSource.Play();
        }
    }

    private void HandleDrop()
    {
        isDragging = false;
        loopSource.Stop();

        if (dropClip != null)
            oneShotSource.PlayOneShot(dropClip);
    }

    private void OnDestroy()
    {
        // Отписываемся от событий
        if (draggable != null)
        {
            draggable.OnGrab -= HandleGrab;
            draggable.OnDrag -= HandleDrag;
            draggable.OnDrop -= HandleDrop;
        }
    }
}