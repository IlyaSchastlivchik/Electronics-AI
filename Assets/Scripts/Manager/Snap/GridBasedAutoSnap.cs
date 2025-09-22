using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GridBasedAutoSnap : MonoBehaviour
{
    [Header("Hotkey Settings")]
    public KeyCode hotkey = KeyCode.P;

    [Header("Snap Settings")]
    public float gridSize = 1.0f;
    public int maxSearchSteps = 15;

    [Header("Debug Visualization")]
    public bool visualizeOccupiedGrid = true;
    public Color occupiedColor = Color.red;
    public Color freeColor = Color.green;

    private SnapGridSystem gridSystem;
    private HashSet<Vector2> occupiedGridPoints = new HashSet<Vector2>();
    private bool isProcessing = false;
    private List<CircuitComponent> alreadyPositionedComponents = new List<CircuitComponent>();

    void Start()
    {
        gridSystem = FindObjectOfType<SnapGridSystem>();
        if (gridSystem == null)
        {
            Debug.LogError("SnapGridSystem not found in scene!");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(hotkey) && !isProcessing)
        {
            StartCoroutine(ResolveCollisions());
        }
    }

    private IEnumerator ResolveCollisions()
    {
        isProcessing = true;
        Debug.Log("=== GRID-BASED AUTO SNAP STARTED ===");

        // �������������� ���������� ������
        Physics2D.SyncTransforms();
        yield return null;

        // �������� ��� ����������
        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>(true);
        List<CircuitComponent> activeComponents = allComponents
            .Where(c => c != null && c.gameObject.activeInHierarchy)
            .ToList();

        Debug.Log($"Processing {activeComponents.Count} active components");

        // ���������� ����������, ������� ��� ��������� positioned
        UpdateAlreadyPositionedComponents(activeComponents);

        // ��������� ���������� �� ���� � ������
        var groupedComponents = activeComponents
            .GroupBy(c => c.componentType)
            .OrderBy(g => g.Key);

        foreach (var group in groupedComponents)
        {
            Debug.Log($"Processing group: {group.Key}");

            // ��� ������� ���� ����������� ��������� �� ������ (������� ������� ������)
            var sortedComponents = group
                .OrderBy(c => c.componentNumber)
                .ToList();

            // ������ ����� ��������� �����
            BuildOccupancyMap(activeComponents);

            // ������������ ������ ��������� � ������
            foreach (CircuitComponent component in sortedComponents)
            {
                if (component == null) continue;

                // ���������� ��� positioned ����������
                if (alreadyPositionedComponents.Contains(component))
                {
                    Debug.Log($"Skipping already positioned component: {component.name}");
                    continue;
                }

                // �������� ���������� ����������� ���� �����
                HashSet<Vector2> componentGridPoints = GetComponentGridPoints(component);

                // ��������� �������� � ������� ������������
                if (HasCollisions(componentGridPoints, component))
                {
                    Debug.Log($"Component {component.name} has collisions, resolving...");

                    // ������� ��������� ����� ��� ���������� � ������ ���������� ���������
                    Vector2 freePosition = FindFreePositionWithPriority(component, componentGridPoints, group.Key);

                    if (freePosition != (Vector2)component.transform.position)
                    {
                        // ���������� ���������
                        MoveComponentToPosition(component, freePosition);

                        // ��������� ����� ���������
                        BuildOccupancyMap(activeComponents);

                        Debug.Log($"Moved {component.name} to {freePosition}");
                        yield return null;
                    }
                }
                else
                {
                    // ���� �������� ���, ��������� � ������ already positioned
                    if (!alreadyPositionedComponents.Contains(component))
                    {
                        alreadyPositionedComponents.Add(component);
                        Debug.Log($"Component {component.name} is already correctly positioned");
                    }
                }
            }

            yield return null;
        }

        Debug.Log("=== GRID-BASED AUTO SNAP COMPLETED ===");
        isProcessing = false;
    }

    private void UpdateAlreadyPositionedComponents(List<CircuitComponent> allComponents)
    {
        // ������� ������ � ������ ��������� ����� ���������� ��� ��������� positioned
        alreadyPositionedComponents.Clear();

        foreach (CircuitComponent component in allComponents)
        {
            if (component == null) continue;

            HashSet<Vector2> componentGridPoints = GetComponentGridPoints(component);
            if (!HasCollisions(componentGridPoints, component))
            {
                alreadyPositionedComponents.Add(component);
            }
        }

        Debug.Log($"Found {alreadyPositionedComponents.Count} already positioned components");
    }

    private Vector2 FindFreePositionWithPriority(CircuitComponent component, HashSet<Vector2> originalGridPoints, string componentType)
    {
        if (component == null) return component.transform.position;

        Vector2 currentPosition = component.transform.position;
        Vector2 bestPosition = currentPosition;
        float bestPriority = float.MinValue;

        // �������� ������� ���������� � �������� �����
        int gridStepsX = 0;
        int gridStepsY = 0;

        DraggableComponent draggable = component.GetComponentInChildren<DraggableComponent>();
        if (draggable != null)
        {
            Collider2D collider = draggable.GetComponent<Collider2D>();
            if (collider != null)
            {
                Bounds bounds = collider.bounds;
                gridStepsX = Mathf.CeilToInt(bounds.size.x / gridSize / 2f);
                gridStepsY = Mathf.CeilToInt(bounds.size.y / gridSize / 2f);
            }
        }

        // ���� ��������� ����� � ������ ���������� ���������
        for (int step = 1; step <= maxSearchSteps; step++)
        {
            // ������� ������� � ������ ������������ � ����������� ��� ��� X
            for (int x = 0; x <= step; x++)
            {
                for (int y = 0; y <= step; y++)
                {
                    if (x == 0 && y == 0) continue;

                    // ������� ��� ���������� �����������
                    for (int xDir = -1; xDir <= 1; xDir += 2)
                    {
                        for (int yDir = -1; yDir <= 1; yDir += 2)
                        {
                            Vector2 testPosition = currentPosition +
                                                  new Vector2(x * xDir * gridSize, y * yDir * gridSize);
                            testPosition = SnapToGrid(testPosition);

                            // ���������, �������� �� �������
                            if (IsPositionFree(testPosition, gridStepsX, gridStepsY, component))
                            {
                                // ��������� ��������� �������
                                float priority = CalculatePositionPriority(testPosition, component, componentType);

                                if (priority > bestPriority)
                                {
                                    bestPriority = priority;
                                    bestPosition = testPosition;
                                }
                            }
                        }
                    }
                }
            }

            // ���� ����� ���������� �������, ���������� ��
            if (bestPosition != currentPosition)
                break;
        }

        return bestPosition;
    }

    private float CalculatePositionPriority(Vector2 position, CircuitComponent component, string componentType)
    {
        float priority = 0f;

        // ��������� ��� ��� X: ������� ������ ������ ���� �����
        if (component.componentType == componentType)
        {
            // ��� ���������� � ������ �����������: ������� ������ �����
            priority -= position.x * 10f; // ��� �����, ��� ���� ���������

            // ��� ��� Y: ������� ������ ������ ���� ����
            priority += position.y * 5f; // ��� ����, ��� ���� ���������
        }

        return priority;
    }

    private void BuildOccupancyMap(List<CircuitComponent> components)
    {
        occupiedGridPoints.Clear();

        foreach (CircuitComponent component in components)
        {
            if (component == null) continue;

            // �������� ��������� DraggableCircle
            DraggableComponent draggable = component.GetComponentInChildren<DraggableComponent>();
            if (draggable == null) continue;

            Collider2D collider = draggable.GetComponent<Collider2D>();
            if (collider == null) continue;

            // ���������� ���������� ���� �����
            Bounds bounds = collider.bounds;
            Vector2 center = bounds.center;
            Vector2 size = bounds.size;

            // ��������� ���������� ���� �����
            int gridStepsX = Mathf.CeilToInt(size.x / gridSize / 2f);
            int gridStepsY = Mathf.CeilToInt(size.y / gridSize / 2f);

            for (int x = -gridStepsX; x <= gridStepsX; x++)
            {
                for (int y = -gridStepsY; y <= gridStepsY; y++)
                {
                    Vector2 gridPoint = SnapToGrid(center + new Vector2(x * gridSize, y * gridSize));
                    occupiedGridPoints.Add(gridPoint);
                }
            }
        }
    }

    private HashSet<Vector2> GetComponentGridPoints(CircuitComponent component)
    {
        HashSet<Vector2> gridPoints = new HashSet<Vector2>();

        if (component == null) return gridPoints;

        // �������� ��������� DraggableCircle
        DraggableComponent draggable = component.GetComponentInChildren<DraggableComponent>();
        if (draggable == null) return gridPoints;

        Collider2D collider = draggable.GetComponent<Collider2D>();
        if (collider == null) return gridPoints;

        // ���������� ���������� ���� �����
        Bounds bounds = collider.bounds;
        Vector2 center = bounds.center;
        Vector2 size = bounds.size;

        // ��������� ���������� ���� �����
        int gridStepsX = Mathf.CeilToInt(size.x / gridSize / 2f);
        int gridStepsY = Mathf.CeilToInt(size.y / gridSize / 2f);

        for (int x = -gridStepsX; x <= gridStepsX; x++)
        {
            for (int y = -gridStepsY; y <= gridStepsY; y++)
            {
                Vector2 gridPoint = SnapToGrid(center + new Vector2(x * gridSize, y * gridSize));
                gridPoints.Add(gridPoint);
            }
        }

        return gridPoints;
    }

    private bool HasCollisions(HashSet<Vector2> componentGridPoints, CircuitComponent ignoringComponent)
    {
        // ���������, ������������ �� ���� ���������� � �������� ������
        foreach (Vector2 point in componentGridPoints)
        {
            if (occupiedGridPoints.Contains(point))
            {
                // ���������� �������� � ����� �����
                CircuitComponent otherComponent = FindComponentAtGridPoint(point);
                if (otherComponent != null && otherComponent != ignoringComponent)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private CircuitComponent FindComponentAtGridPoint(Vector2 gridPoint)
    {
        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>(true);

        foreach (CircuitComponent component in allComponents)
        {
            if (component == null || !component.gameObject.activeInHierarchy) continue;

            HashSet<Vector2> componentGridPoints = GetComponentGridPoints(component);
            if (componentGridPoints.Contains(gridPoint))
            {
                return component;
            }
        }

        return null;
    }

    private bool IsPositionFree(Vector2 position, int gridStepsX, int gridStepsY, CircuitComponent ignoringComponent)
    {
        // ���������, �������� �� ��� ���� � ������� ����������
        for (int x = -gridStepsX; x <= gridStepsX; x++)
        {
            for (int y = -gridStepsY; y <= gridStepsY; y++)
            {
                Vector2 gridPoint = SnapToGrid(position + new Vector2(x * gridSize, y * gridSize));

                if (occupiedGridPoints.Contains(gridPoint))
                {
                    // ���������� �������� � ����� �����
                    CircuitComponent otherComponent = FindComponentAtGridPoint(gridPoint);
                    if (otherComponent != null && otherComponent != ignoringComponent)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private void MoveComponentToPosition(CircuitComponent component, Vector2 targetPosition)
    {
        if (component == null) return;

        // �������� DraggableComponent ��� ���������� �������
        DraggableComponent draggable = component.GetComponentInChildren<DraggableComponent>();
        Rigidbody2D rb = draggable != null ? draggable.GetComponent<Rigidbody2D>() : null;

        // ��������� ��������� ������
        bool wasKinematic = false;
        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // ���������� ���������
        component.transform.position = targetPosition;

        // ��������������� ��������� ������
        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
        }

        // ��������� ������
        Physics2D.SyncTransforms();
    }

    private Vector2 SnapToGrid(Vector2 position)
    {
        float snapInverse = 1.0f / gridSize;
        float x = Mathf.Round(position.x * snapInverse) / snapInverse;
        float y = Mathf.Round(position.y * snapInverse) / snapInverse;
        return new Vector2(x, y);
    }

    // ������������ ������� ����� �����
    private void OnDrawGizmos()
    {
        if (!visualizeOccupiedGrid || !Application.isPlaying) return;

        // ������������ ������� ����� �����
        Gizmos.color = occupiedColor;
        foreach (Vector2 point in occupiedGridPoints)
        {
            Gizmos.DrawWireCube(point, Vector3.one * gridSize * 0.8f);
        }

        // ������������ already positioned �����������
        Gizmos.color = freeColor;
        foreach (CircuitComponent component in alreadyPositionedComponents)
        {
            if (component != null && component.gameObject.activeInHierarchy)
            {
                Gizmos.DrawWireSphere(component.transform.position, gridSize * 0.5f);
            }
        }
    }

    // ����� ��� ������ �� ������ ��������
    public void ManualTriggerSnap()
    {
        if (!isProcessing)
        {
            StartCoroutine(ResolveCollisions());
        }
    }
}