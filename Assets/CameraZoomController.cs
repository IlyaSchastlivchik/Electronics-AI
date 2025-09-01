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
            // ������������� ��������� zoom �� minZoom
            mainCamera.orthographicSize = minZoom;
            targetZoom = maxZoom;

            // ��������� �������� zoom
            StartCoroutine(AnimateStartZoom());
        }
    }

    void Update()
    {
        // ���� �������� ���������, ��������� ���������� �����
        if (!isAnimating)
        {
            HandleMouseZoom();

            // ��������� ����������� � zoom
            mainCamera.orthographicSize = Mathf.SmoothDamp(
                mainCamera.orthographicSize,
                targetZoom,
                ref zoomVelocity,
                smoothTime
            );
        }
    }

    // ��������� ���� �����
    private void HandleMouseZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            targetZoom -= scroll * zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }
    }

    // �������� ��� �������������� ���������� ����
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

            // ������ ������������ �������� �� ����� ��������
            mainCamera.orthographicSize = Mathf.Lerp(startZoom, targetZoom, curveValue);

            yield return null;
        }

        // ����������, ��� �������� �������� ����� �����������
        mainCamera.orthographicSize = targetZoom;
        isAnimating = false;
    }
}