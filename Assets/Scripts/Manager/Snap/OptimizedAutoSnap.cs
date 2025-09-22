using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class OptimizedAutoSnap : MonoBehaviour
{
    [Header("Hotkey Settings")]
    public KeyCode hotkey = KeyCode.P;

    [Header("Grid Settings")]
    public float gridSize = 1.0f;
    public int maxSearchDistance = 10;

    [Header("Animation Settings")]
    public float moveDuration = 0.3f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Sorting Settings")]
    public bool sortLeftToRight = true;
    public bool sortTopToBottom = true;

    private Dictionary<CircuitComponent, List<Vector2>> componentOccupiedPoints = new Dictionary<CircuitComponent, List<Vector2>>();
    private HashSet<Vector2> allOccupiedPoints = new HashSet<Vector2>();
    private bool isProcessing = false;

    void Update()
    {
        if (Input.GetKeyDown(hotkey) && !isProcessing)
        {
            StartCoroutine(OptimizedSnapProcess());
        }
    }

    private IEnumerator OptimizedSnapProcess()
    {
        isProcessing = true;
        Debug.Log("=== OPTIMIZED SNAP PROCESS STARTED ===");

        // ��� 1: �������������� ��� ���������� � �����
        SnapAllComponentsToGrid();

        // ��� 2: ��������� ���������� ���� ��� ������� ����������
        CalculateOccupiedPoints();

        // ��� 3: ���������� � ��������� ����������
        var componentGroups = GroupAndSortComponents();

        // ��� 4: ��������� �������� ��� ������ ������
        foreach (var group in componentGroups)
        {
            yield return StartCoroutine(ResolveGroupCollisions(group));
        }

        Debug.Log("=== OPTIMIZED SNAP PROCESS COMPLETED ===");
        isProcessing = false;
    }

    private void SnapAllComponentsToGrid()
    {
        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>();
        foreach (CircuitComponent component in allComponents)
        {
            if (component != null)
            {
                Vector3 snappedPosition = SnapToGrid(component.transform.position);
                component.transform.position = snappedPosition;
            }
        }
    }

    private void CalculateOccupiedPoints()
    {
        componentOccupiedPoints.Clear();
        allOccupiedPoints.Clear();

        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>();
        foreach (CircuitComponent component in allComponents)
        {
            if (component == null) continue;

            List<Vector2> points = new List<Vector2>();
            Bounds bounds = CalculateComponentBounds(component);

            // ��������� ���������� ���� ����� �� ������ ������ ����������
            int xSteps = Mathf.CeilToInt(bounds.size.x / gridSize);
            int ySteps = Mathf.CeilToInt(bounds.size.y / gridSize);

            for (int x = 0; x <= xSteps; x++)
            {
                for (int y = 0; y <= ySteps; y++)
                {
                    Vector2 point = SnapToGrid(bounds.min + new Vector3(x * gridSize, y * gridSize));
                    points.Add(point);
                    allOccupiedPoints.Add(point);
                }
            }

            componentOccupiedPoints[component] = points;
        }
    }

    private Bounds CalculateComponentBounds(CircuitComponent component)
    {
        // �������� Renderer ��� ���������� ������
        Renderer renderer = component.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }

        // ���� ��������� ���, ���������� ���������
        Collider2D collider = component.GetComponentInChildren<Collider2D>();
        if (collider != null)
        {
            return collider.bounds;
        }

        // ���� ��� �� ���������, �� ����������, ���������� ������� ����������
        return new Bounds(component.transform.position, new Vector3(gridSize, gridSize, 0));
    }

    private List<List<CircuitComponent>> GroupAndSortComponents()
    {
        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>();

        // ���������� �� ���� ����������
        var groups = allComponents
            .Where(c => c != null)
            .GroupBy(c => c.componentType)
            .Select(g => g.ToList())
            .ToList();

        // ��������� ������ ������
        foreach (var group in groups)
        {
            group.Sort((a, b) =>
            {
                // ������� ��������� �� X (����� �������)
                if (sortLeftToRight)
                {
                    int xCompare = a.transform.position.x.CompareTo(b.transform.position.x);
                    if (xCompare != 0) return xCompare;
                }

                // ����� ��������� �� Y (������ ����)
                if (sortTopToBottom)
                {
                    return b.transform.position.y.CompareTo(a.transform.position.y);
                }

                return a.componentNumber.CompareTo(b.componentNumber);
            });
        }

        return groups;
    }

    private IEnumerator ResolveGroupCollisions(List<CircuitComponent> group)
    {
        // ������� ����� ������� ����� ��� ���� ������
        HashSet<Vector2> groupOccupiedPoints = new HashSet<Vector2>(allOccupiedPoints);

        foreach (CircuitComponent component in group)
        {
            if (component == null) continue;

            // �������� �����, ���������� ���� �����������
            List<Vector2> componentPoints;
            if (!componentOccupiedPoints.TryGetValue(component, out componentPoints))
                continue;

            // ���������, ���� �� ��������
            bool hasCollision = componentPoints.Any(point =>
                groupOccupiedPoints.Contains(point) && !IsPointOwnedByComponent(point, component));

            if (hasCollision)
            {
                // ������� ��������� �������
                Vector2 freePosition = FindFreePosition(component, groupOccupiedPoints);

                // ��������� �����������
                yield return StartCoroutine(AnimateMove(component, freePosition));

                // ��������� ������� �����
                UpdateOccupiedPoints(component, freePosition, groupOccupiedPoints);
            }

            // ��������� ����� ����� ���������� � �������
            foreach (Vector2 point in componentPoints)
            {
                groupOccupiedPoints.Add(point);
            }
        }
    }

    private bool IsPointOwnedByComponent(Vector2 point, CircuitComponent component)
    {
        List<Vector2> componentPoints;
        if (componentOccupiedPoints.TryGetValue(component, out componentPoints))
        {
            return componentPoints.Contains(point);
        }
        return false;
    }

    private Vector2 FindFreePosition(CircuitComponent component, HashSet<Vector2> occupiedPoints)
    {
        Vector2 currentPosition = component.transform.position;
        Vector2 bestPosition = currentPosition;
        float bestScore = float.MinValue;

        // �������� ������� ����������
        Bounds bounds = CalculateComponentBounds(component);
        int xSteps = Mathf.CeilToInt(bounds.size.x / gridSize);
        int ySteps = Mathf.CeilToInt(bounds.size.y / gridSize);

        // ���� ��������� ������� � ������� ������
        for (int dx = -maxSearchDistance; dx <= maxSearchDistance; dx++)
        {
            for (int dy = -maxSearchDistance; dy <= maxSearchDistance; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                Vector2 testPosition = SnapToGrid(currentPosition + new Vector2(dx * gridSize, dy * gridSize));

                // ���������, �������� �� �������
                if (IsPositionFree(testPosition, xSteps, ySteps, occupiedPoints))
                {
                    // ��������� "��������" ������� �� ������ ����������� ����������
                    float score = CalculatePositionScore(testPosition, component);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestPosition = testPosition;
                    }
                }
            }
        }

        return bestPosition;
    }

    private bool IsPositionFree(Vector2 position, int xSteps, int ySteps, HashSet<Vector2> occupiedPoints)
    {
        // ���������, �������� �� ��� ���� � ������� ����������
        for (int x = 0; x <= xSteps; x++)
        {
            for (int y = 0; y <= ySteps; y++)
            {
                Vector2 testPoint = SnapToGrid(position + new Vector2(x * gridSize, y * gridSize));
                if (occupiedPoints.Contains(testPoint))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private float CalculatePositionScore(Vector2 position, CircuitComponent component)
    {
        float score = 0f;

        // ��������� ��� ���������� ����� �������
        if (sortLeftToRight)
        {
            score -= position.x; // ��� �����, ��� �����
        }

        // ��������� ��� ���������� ������ ����
        if (sortTopToBottom)
        {
            score += position.y; // ��� ����, ��� �����
        }

        // ��������� ����� ����������
        score -= component.componentNumber * 0.1f;

        return score;
    }

    private IEnumerator AnimateMove(CircuitComponent component, Vector2 targetPosition)
    {
        Vector3 startPosition = component.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            float t = moveCurve.Evaluate(elapsedTime / moveDuration);
            component.transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        component.transform.position = targetPosition;
    }

    private void UpdateOccupiedPoints(CircuitComponent component, Vector2 newPosition, HashSet<Vector2> occupiedPoints)
    {
        // ������� ������ ����� ����������
        List<Vector2> oldPoints;
        if (componentOccupiedPoints.TryGetValue(component, out oldPoints))
        {
            foreach (Vector2 point in oldPoints)
            {
                occupiedPoints.Remove(point);
                allOccupiedPoints.Remove(point);
            }
        }

        // ��������� ����� �����
        Bounds bounds = CalculateComponentBounds(component);
        int xSteps = Mathf.CeilToInt(bounds.size.x / gridSize);
        int ySteps = Mathf.CeilToInt(bounds.size.y / gridSize);

        List<Vector2> newPoints = new List<Vector2>();
        for (int x = 0; x <= xSteps; x++)
        {
            for (int y = 0; y <= ySteps; y++)
            {
                Vector2 point = SnapToGrid(newPosition + new Vector2(x * gridSize, y * gridSize));
                newPoints.Add(point);
                occupiedPoints.Add(point);
                allOccupiedPoints.Add(point);
            }
        }

        componentOccupiedPoints[component] = newPoints;
    }

    private Vector2 SnapToGrid(Vector2 position)
    {
        float snapInverse = 1.0f / gridSize;
        float x = Mathf.Round(position.x * snapInverse) / snapInverse;
        float y = Mathf.Round(position.y * snapInverse) / snapInverse;
        return new Vector2(x, y);
    }

    // ����� ��� ������ �� ������ ��������
    public void ManualTriggerSnap()
    {
        if (!isProcessing)
        {
            StartCoroutine(OptimizedSnapProcess());
        }
    }
}