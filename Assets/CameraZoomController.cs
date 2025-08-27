using UnityEngine;

public class CameraZoomController : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;
    public float minZoom = 2f;
    public float maxZoom = 10f;
    public float smoothTime = 0.2f;

    private Camera cam;
    private float targetZoom;
    private float zoomVelocity;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
            Debug.LogWarning("CameraZoomController: No camera found, using Camera.main");
        }

        targetZoom = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;
    }

    private void Update()
    {
        HandleZoomInput();
        ApplyZoom();
    }

    private void HandleZoomInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0f) return;

        if (cam.orthographic)
        {
            // Для ортографической камеры
            targetZoom -= scroll * zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }
        else
        {
            // Для перспективной камеры
            targetZoom -= scroll * zoomSpeed * 10f;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }
    }

    private void ApplyZoom()
    {
        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.SmoothDamp(
                cam.orthographicSize,
                targetZoom,
                ref zoomVelocity,
                smoothTime
            );
        }
        else
        {
            cam.fieldOfView = Mathf.SmoothDamp(
                cam.fieldOfView,
                targetZoom,
                ref zoomVelocity,
                smoothTime
            );
        }
    }
}
