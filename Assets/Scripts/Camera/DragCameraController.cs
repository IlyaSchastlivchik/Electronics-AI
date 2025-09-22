using UnityEngine;
using System.Collections;

public class DragCameraController : MonoBehaviour
{
    [Header("Drag Settings")]
    public float dragSpeed = 2.5f;
    public bool invertDrag = false;

    [Header("Keyboard Movement")]
    public float keyboardMoveSpeed = 5f;
    public bool smoothKeyboardMovement = true;
    public float smoothTime = 0.1f;

    [Header("Movement Boundaries")]
    public bool useBoundaries = false;
    public Vector2 minBoundary = new Vector2(-50f, -50f);
    public Vector2 maxBoundary = new Vector2(50f, 50f);

    private Camera mainCamera;
    private Vector3 dragOrigin;
    private bool isDragging = false;
    private Vector3 keyboardVelocity;
    private bool cameraDragEnabled = true; // Флаг, разрешающий перемещение камеры

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            this.enabled = false;
        }
    }

    void Update()
    {
        if (cameraDragEnabled)
        {
            HandleMouseDrag();
        }

        HandleKeyboardMovement();
        ClampCameraPosition();
    }

    private void HandleMouseDrag()
    {
        // Начало перетаскивания
        if (Input.GetMouseButtonDown(0))
        {
            // Проверяем, не кликнули ли мы на компонент
            if (!IsPointerOverDraggableComponent())
            {
                dragOrigin = GetMouseWorldPosition();
                isDragging = true;
            }
        }

        // Завершение перетаскивания
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        // Процесс перетаскивания
        if (isDragging)
        {
            Vector3 currentPos = GetMouseWorldPosition();
            Vector3 difference = dragOrigin - currentPos;

            if (invertDrag) difference = -difference;

            transform.position += difference;
        }
    }

    private void HandleKeyboardMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontal, vertical, 0f);

        if (moveDirection != Vector3.zero)
        {
            if (smoothKeyboardMovement)
            {
                Vector3 targetPosition = transform.position + moveDirection * keyboardMoveSpeed * Time.deltaTime;
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref keyboardVelocity, smoothTime);
            }
            else
            {
                transform.position += moveDirection * keyboardMoveSpeed * Time.deltaTime;
            }
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = -mainCamera.transform.position.z;
        return mainCamera.ScreenToWorldPoint(mousePoint);
    }

    private void ClampCameraPosition()
    {
        if (!useBoundaries) return;

        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minBoundary.x, maxBoundary.x);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, minBoundary.y, maxBoundary.y);
        transform.position = clampedPosition;
    }

    // Метод для проверки, находится ли курсор над перетаскиваемым компонентом
    private bool IsPointerOverDraggableComponent()
    {
        Vector3 mousePos = Input.mousePosition;
        RaycastHit2D hit = Physics2D.Raycast(GetMouseWorldPosition(), Vector2.zero);

        if (hit.collider != null)
        {
            DraggableComponent draggable = hit.collider.GetComponent<DraggableComponent>();
            return draggable != null;
        }

        return false;
    }

    // Метод для включения/отключения перемещения камеры
    public void SetCameraDragEnabled(bool enabled)
    {
        cameraDragEnabled = enabled;
        isDragging = false; // Сбрасываем состояние перетаскивания
    }

    // Методы для кнопок UI (можно вызвать из Unity Events)
    public void MoveLeft()
    {
        transform.position += Vector3.left * keyboardMoveSpeed;
    }

    public void MoveRight()
    {
        transform.position += Vector3.right * keyboardMoveSpeed;
    }

    public void MoveUp()
    {
        transform.position += Vector3.up * keyboardMoveSpeed;
    }

    public void MoveDown()
    {
        transform.position += Vector3.down * keyboardMoveSpeed;
    }
}