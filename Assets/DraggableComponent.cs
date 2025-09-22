using UnityEngine;
using System;

public class DraggableComponent : MonoBehaviour
{
    private Vector3 screenPoint;
    private Vector3 offset;
    private Rigidbody2D rb;
    private bool isDragging = false;
    private bool wasKinematicInitially;
    private DragCameraController cameraController; // ��������� ������ �� ���������� ������

    // ������� ��� ��������� ������
    public event Action OnGrab;
    public event Action OnDrag;
    public event Action OnDrop;

    [Header("Grid Settings")]
    [SerializeField] private float gridSize = 1.0f; // ������ ������ �����

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody2D not found on " + gameObject.name + ". Please add Rigidbody2D component.");
            return;
        }

        // ��������� �������� ��������� kinematic
        wasKinematicInitially = rb.isKinematic;

        // ������� ���������� ������
        cameraController = Camera.main.GetComponent<DragCameraController>();
        if (cameraController == null)
        {
            Debug.LogError("DragCameraController not found on Main Camera!");
        }
    }

    void OnMouseDown()
    {
        if (rb != null)
        {
            screenPoint = Camera.main.WorldToScreenPoint(transform.position);
            offset = transform.position - Camera.main.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));

            // ��������� ������� ��������� � ������ kinematic ��� �������� ��������������
            rb.isKinematic = true;
            isDragging = true;

            // ��������� ����������� ������
            if (cameraController != null)
            {
                cameraController.SetCameraDragEnabled(false);
            }

            OnGrab?.Invoke();
        }
    }

    void OnMouseDrag()
    {
        if (isDragging && rb != null)
        {
            Vector3 cursorScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
            Vector3 cursorPosition = Camera.main.ScreenToWorldPoint(cursorScreenPoint) + offset;
            transform.position = cursorPosition;

            OnDrag?.Invoke();
        }
    }

    void OnMouseUp()
    {
        if (rb != null)
        {
            isDragging = false;

            // ��������������� � �����
            SnapToGrid();

            // ��������������� �������� ��������� kinematic
            rb.isKinematic = wasKinematicInitially;

            // ������������ ����������� ������
            if (cameraController != null)
            {
                cameraController.SetCameraDragEnabled(true);
            }

            OnDrop?.Invoke();
        }
    }

    // ����� ��������������� � �����
    private void SnapToGrid()
    {
        // ��������� ������� �� ��������� ������ ����� 
        float snapInverse = 1.0f / gridSize;
        float x = Mathf.Round(transform.position.x * snapInverse) / snapInverse;
        float y = Mathf.Round(transform.position.y * snapInverse) / snapInverse;

        // ������������� ����� �������
        transform.position = new Vector3(x, y, transform.position.z);
    }

    // ����� ��� ��������������� � ���������� ������� (���� ����� �����)
    public void SnapToPosition(Vector3 gridPosition)
    {
        float snapInverse = 1.0f / gridSize;
        float x = Mathf.Round(gridPosition.x * snapInverse) / snapInverse;
        float y = Mathf.Round(gridPosition.y * snapInverse) / snapInverse;

        transform.position = new Vector3(x, y, transform.position.z);
    }
}