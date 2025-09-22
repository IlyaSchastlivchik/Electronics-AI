using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AdvancedAutoSnap : MonoBehaviour
{
    [Header("Hotkey Settings")]
    public KeyCode hotkey = KeyCode.P;

    [Header("Collision Resolution")]
    public int maxPassesPerComponent = 3;
    public float gridSize = 1.0f;
    public int maxSearchSteps = 15;

    [Header("Vertical Sorting")]
    public bool enableVerticalSorting = true;
    public float verticalSpacing = 2.0f;
    public bool sortDescending = true;

    [Header("Animation")]
    public float moveDuration = 0.3f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Debug")]
    public bool drawDebugGizmos = true;
    public Color collisionColor = Color.red;
    public Color freeColor = Color.green;

    private bool isProcessing = false;
    private Dictionary<CircuitComponent, int> passCount = new Dictionary<CircuitComponent, int>();

    void Update()
    {
        if (Input.GetKeyDown(hotkey) && !isProcessing)
        {
            StartCoroutine(AutoResolveCollisions());
        }
    }

    private IEnumerator AutoResolveCollisions()
    {
        isProcessing = true;
        Debug.Log("Starting advanced collision resolution...");

        // ���������� ������� ��������
        passCount.Clear();

        // �������������� ������������� ������
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();

        // �������� ��� ����������
        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>();
        List<CircuitComponent> activeComponents = allComponents
            .Where(c => c != null && c.gameObject.activeInHierarchy)
            .ToList();

        // �������� ���� ���������� ��������
        bool needsMorePasses = true;
        int globalPass = 0;
        int maxGlobalPasses = activeComponents.Count * maxPassesPerComponent;

        while (needsMorePasses && globalPass < maxGlobalPasses)
        {
            globalPass++;
            needsMorePasses = false;
            Debug.Log($"Global pass {globalPass}/{maxGlobalPasses}");

            // ��������� ���������� �� ������ (������� ���������� � ������� �������)
            var sortedComponents = activeComponents
                .OrderBy(c => c.componentNumber)
                .ToList();

            foreach (CircuitComponent component in sortedComponents)
            {
                if (component == null) continue;

                // �������� ������� ���������� �������� ��� ����� ����������
                int currentPassCount = passCount.ContainsKey(component) ? passCount[component] : 0;

                // ���������� ����������, ������� ��� ���������� ������������ ���������� ���
                if (currentPassCount >= maxPassesPerComponent) continue;

                // ��������� ��������
                if (HasCollisions(component))
                {
                    Debug.Log($"Collision detected for {component.name} (pass {currentPassCount + 1})");

                    // ������� ��������� �������
                    Vector2 freePosition = FindFreePosition(component);

                    if (freePosition != (Vector2)component.transform.position)
                    {
                        // ���������� ���������
                        yield return StartCoroutine(MoveComponentSmoothly(component, freePosition));
                        needsMorePasses = true;

                        // ����������� ������� �������� ��� ����� ����������
                        passCount[component] = currentPassCount + 1;
                    }
                    else
                    {
                        // �� ������� ����� ��������� �������, �������� ��� ������������
                        passCount[component] = maxPassesPerComponent;
                    }
                }
                else
                {
                    // ��� ��������, �������� ��� ������������
                    passCount[component] = maxPassesPerComponent;
                }

                yield return null;
            }
        }

        // ������������ ���������� ����� ���������� ��������
        if (enableVerticalSorting)
        {
            yield return StartCoroutine(SortComponentsVertically(activeComponents));
        }

        Debug.Log("Collision resolution completed");
        isProcessing = false;
    }

    private IEnumerator SortComponentsVertically(List<CircuitComponent> components)
    {
        Debug.Log("Starting vertical sorting...");

        // ���������� ���������� �� ����
        var groupedComponents = components
            .GroupBy(c => c.componentType)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.componentNumber).ToList());

        // ��������� ������ ������ �� ���������
        foreach (var group in groupedComponents)
        {
            string componentType = group.Key;
            List<CircuitComponent> typeComponents = group.Value;

            Debug.Log($"Sorting {typeComponents.Count} components of type {componentType}");

            // ���������� ��������� ������� ��� ���� ������
            float startY = typeComponents.Max(c => c.transform.position.y);
            if (!sortDescending)
            {
                startY = typeComponents.Min(c => c.transform.position.y);
            }

            // ����������� ���������� �� ���������
            for (int i = 0; i < typeComponents.Count; i++)
            {
                CircuitComponent component = typeComponents[i];
                Vector2 currentPosition = component.transform.position;

                // ��������� ������� ������� �� Y
                float targetY;
                if (sortDescending)
                {
                    targetY = startY - i * verticalSpacing;
                }
                else
                {
                    targetY = startY + i * verticalSpacing;
                }

                Vector2 targetPosition = new Vector2(currentPosition.x, targetY);

                // ���������� ���������, ���� ������� ����������
                if (Mathf.Abs(currentPosition.y - targetY) > 0.01f)
                {
                    yield return StartCoroutine(MoveComponentSmoothly(component, targetPosition));
                }
            }
        }

        Debug.Log("Vertical sorting completed");
    }

    private bool HasCollisions(CircuitComponent component)
    {
        if (component == null) return false;

        Bounds bounds = CalculateComponentBounds(component);

        // ���������� OverlapBoxAll ��� ������� ����������� ��������
        Collider2D[] colliders = Physics2D.OverlapBoxAll(
            bounds.center,
            bounds.size,
            0
        );

        foreach (Collider2D collider in colliders)
        {
            if (collider != null && collider.enabled)
            {
                CircuitComponent otherComponent = collider.GetComponentInParent<CircuitComponent>();
                if (otherComponent != null && otherComponent != component)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private Vector2 FindFreePosition(CircuitComponent component)
    {
        if (component == null) return Vector2.zero;

        Vector2 currentPosition = component.transform.position;
        Vector2 bestPosition = currentPosition;
        float bestDistance = float.MaxValue;

        Bounds bounds = CalculateComponentBounds(component);
        Vector2 componentSize = bounds.size;

        // ���� ��������� ������� �� ������� �� ������� �������
        for (int distance = 1; distance <= maxSearchSteps; distance++)
        {
            for (int angle = 0; angle < 360; angle += 45)
            {
                Vector2 direction = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

                Vector2 testPosition = currentPosition + direction * distance * gridSize;
                testPosition = SnapToGrid(testPosition);

                // ���������, �������� �� �������
                if (IsPositionFree(testPosition, componentSize, component))
                {
                    float testDistance = Vector2.Distance(currentPosition, testPosition);
                    if (testDistance < bestDistance)
                    {
                        bestDistance = testDistance;
                        bestPosition = testPosition;
                    }
                }
            }
        }

        return bestPosition;
    }

    private bool IsPositionFree(Vector2 position, Vector2 size, CircuitComponent ignoringComponent)
    {
        // ������� ��������� Bounds ��� ��������
        Bounds testBounds = new Bounds(position, size);

        // ��������� �������� � ������� �������
        Collider2D[] colliders = Physics2D.OverlapBoxAll(
            testBounds.center,
            testBounds.size,
            0
        );

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

    private IEnumerator MoveComponentSmoothly(CircuitComponent component, Vector2 targetPosition)
    {
        if (component == null) yield break;

        Vector2 startPosition = component.transform.position;
        float elapsed = 0f;

        // �������� Rigidbody2D ���� ����
        Rigidbody2D rb = component.GetComponentInChildren<Rigidbody2D>();
        bool wasKinematic = false;

        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
        }

        // ������� �����������
        while (elapsed < moveDuration)
        {
            float t = moveCurve.Evaluate(elapsed / moveDuration);
            component.transform.position = Vector2.Lerp(startPosition, targetPosition, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // ��������� �������
        component.transform.position = targetPosition;

        // ��������������� ������
        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
        }

        // �������������� ������
        Physics2D.SyncTransforms();
    }

    private Vector2 SnapToGrid(Vector2 position)
    {
        float snapInverse = 1.0f / gridSize;
        float x = Mathf.Round(position.x * snapInverse) / snapInverse;
        float y = Mathf.Round(position.y * snapInverse) / snapInverse;
        return new Vector2(x, y);
    }

    private Bounds CalculateComponentBounds(CircuitComponent component)
    {
        if (component == null)
            return new Bounds(Vector3.zero, Vector3.zero);

        // �������� ��� ��������� � ���������� � ��� �����
        Renderer[] renderers = component.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            // ���������� ������ �������� ��� ������
            Bounds bounds = renderers[0].bounds;

            // ��������� ������� ��� ��������� ���� ��������� ����������
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].enabled)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }
            return bounds;
        }

        // ���� ���������� ���, ��������� ����������
        Collider2D[] colliders = component.GetComponentsInChildren<Collider2D>();
        if (colliders.Length > 0)
        {
            Bounds bounds = colliders[0].bounds;

            for (int i = 1; i < colliders.Length; i++)
            {
                if (colliders[i] != null && colliders[i].enabled)
                {
                    bounds.Encapsulate(colliders[i].bounds);
                }
            }
            return bounds;
        }

        // ���� ��� �� ����������, �� �����������, ���������� GeometryUtility.CalculateBounds
        // �������� ��� �������� ������� � �� �������
        List<Vector3> allPositions = new List<Vector3>();
        GetAllChildPositions(component.transform, ref allPositions);

        if (allPositions.Count > 0)
        {
            return GeometryUtility.CalculateBounds(allPositions.ToArray(), Matrix4x4.identity);
        }

        // ���� ������ �� �������, ���������� bounds �� ���������
        return new Bounds(component.transform.position, Vector3.one * gridSize);
    }

    private void GetAllChildPositions(Transform parent, ref List<Vector3> positions)
    {
        if (parent == null) return;

        // ��������� ������� �������� �������
        positions.Add(parent.position);

        // ���������� ��������� ������� ���� �����
        for (int i = 0; i < parent.childCount; i++)
        {
            GetAllChildPositions(parent.GetChild(i), ref positions);
        }
    }

    // ������������ ��� �������
    private void OnDrawGizmos()
    {
        if (!drawDebugGizmos || !Application.isPlaying) return;

        // ������������ ��������
        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>();
        foreach (CircuitComponent component in allComponents)
        {
            if (component == null || !component.gameObject.activeInHierarchy) continue;

            Bounds bounds = CalculateComponentBounds(component);
            bool hasCollision = HasCollisions(component);

            Gizmos.color = hasCollision ? collisionColor : freeColor;
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            if (hasCollision)
            {
                Gizmos.DrawIcon(bounds.center + Vector3.up * bounds.extents.y, "warning");
            }
        }
    }
}