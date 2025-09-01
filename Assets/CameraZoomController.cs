using UnityEngine;
using System.Collections;

public class CameraZoomController : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float zoomSpeed = 100f;
    public float minZoom = 10f;
    public float maxZoom = 100f;
    public float smoothTime = 0.2f;

    [Header("Start Animation")]
    public float startZoomDuration = 1f;
    public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Camera mainCamera;
    private float targetZoom;
    private float zoomVelocity;
    private bool isAnimating = false;

    void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera != null)
        {
            // Устанавливаем начальный zoom на minZoom
            mainCamera.orthographicSize = minZoom;
            targetZoom = maxZoom;

            // Запускаем анимацию zoom
            StartCoroutine(AnimateStartZoom());
        }
    }

    void Update()
    {
        // Если анимация завершена, разрешаем управление мышью
        if (!isAnimating)
        {
            HandleMouseZoom();

            // Применяем сглаживание к zoom
            mainCamera.orthographicSize = Mathf.SmoothDamp(
                mainCamera.orthographicSize,
                targetZoom,
                ref zoomVelocity,
                smoothTime
            );
        }
    }

    // Обработка зума мышью
    private void HandleMouseZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            targetZoom -= scroll * zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }
    }

    // Корутина для анимированного стартового зума
    private IEnumerator AnimateStartZoom()
    {
        isAnimating = true;
        float elapsedTime = 0f;
        float startZoom = minZoom;

        while (elapsedTime < startZoomDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / startZoomDuration);
            float curveValue = zoomCurve.Evaluate(t);

            // Прямое установление значения во время анимации
            mainCamera.orthographicSize = Mathf.Lerp(startZoom, targetZoom, curveValue);

            yield return null;
        }

        // Убеждаемся, что конечное значение точно установлено
        mainCamera.orthographicSize = targetZoom;
        isAnimating = false;
    }
}