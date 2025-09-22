using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CircuitSnapManager : MonoBehaviour
{
    [Header("Hotkey Settings")]
    public KeyCode snapHotkey = KeyCode.P;
    public bool useControlModifier = true;

    [Header("Snap Settings")]
    public float searchRadius = 5f;
    public Vector2 searchDirectionPriority = Vector2.right;
    public int maxIterations = 100;
    public float collisionCheckRadius = 0.3f;
    public float gridSize = 1.0f;

    [Header("Debug Settings")]
    public bool enableDebugVisualization = true;
    public Color freePositionColor = Color.green;
    public Color occupiedPositionColor = Color.red;

    private bool isProcessing = false;
    private List<CircuitComponent> circuitComponents;

    void Update()
    {
        if (!isProcessing && CheckHotkey())
        {
            StartCoroutine(AutoSnapOverlappingComponentsCoroutine());
        }
    }

    private bool CheckHotkey()
    {
        if (useControlModifier)
            return Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(snapHotkey);
        else
            return Input.GetKeyDown(snapHotkey);
    }

    private System.Collections.IEnumerator AutoSnapOverlappingComponentsCoroutine()
    {
        isProcessing = true;
        Debug.Log("Starting auto-snap process...");

        // �������� ��� ����������
        circuitComponents = FindObjectsOfType<CircuitComponent>().ToList();

        // ��������� �� ������ ���������� (�� ��������)
        var sortedComponents = circuitComponents
            .OrderByDescending(c => c.componentNumber)
            .ToList();

        foreach (var component in sortedComponents)
        {
            if (HasCollisions(component))
            {
                Debug.Log($"Component {component.componentId} has collisions, searching for free position...");

                // ���������� �����, ����������� DraggableComponent ��� ����������� �����������
                if (SafeMoveComponentToFreePosition(component))
                {
                    Debug.Log($"Successfully moved {component.componentId}");
                }
                else
                {
                    Debug.LogWarning($"Could not find free position for {component.componentId}");
                }

                // ���� ��������� ���� ��� ������������� ��������
                yield return null;
            }
        }

        Debug.Log("Auto-snap process completed");
        isProcessing = false;
    }

    private bool SafeMoveComponentToFreePosition(CircuitComponent component)
    {
        // �������� DraggableComponent ��� ���������� �������
        DraggableComponent draggable = component.GetComponentInChildren<DraggableComponent>();
        if (draggable == null)
        {
            Debug.LogError($"No DraggableComponent found for {component.componentId}");
            return false;
        }

        // �������� Rigidbody2D
        Rigidbody2D rb = draggable.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError($"No Rigidbody2D found for {component.componentId}");
            return false;
        }

        // ��������� �������� ���������
        bool wasKinematic = rb.isKinematic;

        // ������������� kinematic ��� �������� �����������
        rb.isKinematic = true;

        // ���� ��������� �������
        Vector3 freePosition = FindFreePosition(component);

        if (freePosition != component.transform.position)
        {
            // ���������� ���������
            component.transform.position = freePosition;

            // ��������� ��������������� � �����
            SnapToGrid(draggable);

            Debug.Log($"Moved {component.componentId} to {freePosition}");
        }

        // ��������������� �������� ���������
        rb.isKinematic = wasKinematic;

        // ������������� ��������� ������
        Physics2D.SyncTransforms();

        return true;
    }

    private Vector3 FindFreePosition(CircuitComponent component)
    {
        Vector3 currentPos = component.transform.position;
        Vector3 bestPosition = currentPos;
        float bestDistance = float.MaxValue;

        // ��������� ������� � ������� ������
        for (float x = -searchRadius; x <= searchRadius; x += gridSize)
        {
            for (float y = -searchRadius; y <= searchRadius; y += gridSize)
            {
                Vector3 testPosition = currentPos + new Vector3(x, y, 0);

                // ���������� ������� ������� �������
                if (Vector3.Distance(currentPos, testPosition) > searchRadius)
                    continue;

                // �������������� � �����
                testPosition = SnapToGridPosition(testPosition);

                // ���������, �������� �� �������
                if (IsPositionFree(testPosition, component))
                {
                    float distance = Vector3.Distance(currentPos, testPosition);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestPosition = testPosition;
                    }
                }
            }
        }

        return bestPosition;
    }

    private Vector3 SnapToGridPosition(Vector3 position)
    {
        float snapInverse = 1.0f / gridSize;
        float x = Mathf.Round(position.x * snapInverse) / snapInverse;
        float y = Mathf.Round(position.y * snapInverse) / snapInverse;
        return new Vector3(x, y, position.z);
    }

    private void SnapToGrid(DraggableComponent draggable)
    {
        // ���������� ����� �� DraggableComponent ��� ���������������
        draggable.SnapToPosition(draggable.transform.position);
    }

    private bool HasCollisions(CircuitComponent component)
    {
        // ��������� �������� � ������� Physics2D.OverlapCircleAll
        Collider2D[] colliders = component.GetComponentsInChildren<Collider2D>();

        foreach (var collider in colliders)
        {
            if (collider == null || !collider.enabled) continue;

            Collider2D[] overlaps = Physics2D.OverlapCircleAll(
                collider.bounds.center,
                collider.bounds.size.magnitude / 2f
            );

            foreach (var overlap in overlaps)
            {
                if (overlap != null && overlap.enabled &&
                    overlap.transform != component.transform &&
                    !overlap.transform.IsChildOf(component.transform))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsPositionFree(Vector3 position, CircuitComponent ignoringComponent)
    {
        // ��������� �������� � ������� Physics2D.OverlapCircleAll
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, collisionCheckRadius);

        foreach (Collider2D collider in colliders)
        {
            if (collider != null && collider.enabled)
            {
                CircuitComponent otherComponent = collider.GetComponentInParent<CircuitComponent>();
                if (otherComponent != null && otherComponent != ignoringComponent)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void ManualTriggerSnap()
    {
        if (!isProcessing)
        {
            StartCoroutine(AutoSnapOverlappingComponentsCoroutine());
        }
    }
}