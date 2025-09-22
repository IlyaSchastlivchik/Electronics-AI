using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SimpleDenseAutoSnap : MonoBehaviour
{
    [Header("Hotkey Settings")]
    public KeyCode hotkey = KeyCode.P;

    [Header("Snap Settings")]
    public float gridSize = 1.0f;
    public float componentSpacing = 0.1f;

    [Header("Animation")]
    public float moveDuration = 0.3f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isProcessing = false;
    private Dictionary<string, List<CircuitComponent>> componentGroups = new Dictionary<string, List<CircuitComponent>>();

    void Update()
    {
        if (Input.GetKeyDown(hotkey) && !isProcessing)
        {
            StartCoroutine(ProcessAllComponents());
        }
    }

    private IEnumerator ProcessAllComponents()
    {
        isProcessing = true;
        Debug.Log("Starting component processing...");

        // �������������� ������������� ������
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();

        // �������� ��� ����������
        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>();
        List<CircuitComponent> activeComponents = allComponents
            .Where(c => c != null && c.gameObject.activeInHierarchy)
            .ToList();

        Debug.Log($"Found {activeComponents.Count} active components");

        // ���������� ���������� �� ����
        GroupComponentsByType(activeComponents);

        // ������������ ������ ������
        foreach (var group in componentGroups)
        {
            yield return StartCoroutine(ProcessComponentGroup(group.Key, group.Value));
        }

        Debug.Log("Component processing completed");
        isProcessing = false;
    }

    private void GroupComponentsByType(List<CircuitComponent> components)
    {
        componentGroups.Clear();

        foreach (CircuitComponent component in components)
        {
            if (component == null) continue;

            string type = component.componentType;
            if (string.IsNullOrEmpty(type))
            {
                type = "Unknown";
                component.componentType = type;
            }

            if (!componentGroups.ContainsKey(type))
            {
                componentGroups[type] = new List<CircuitComponent>();
            }

            componentGroups[type].Add(component);
        }

        // ��������� ���������� � ������ ������ �� ������
        foreach (var group in componentGroups)
        {
            group.Value.Sort((a, b) => a.componentNumber.CompareTo(b.componentNumber));
        }
    }

    private IEnumerator ProcessComponentGroup(string groupType, List<CircuitComponent> components)
    {
        Debug.Log($"Processing group '{groupType}' with {components.Count} components");

        // ��� 1: ��������� �������� ����� ������������ ������
        yield return StartCoroutine(ResolveGroupCollisions(components));

        // ��� 2: ������ ��������� ���������� �� ���������
        yield return StartCoroutine(ArrangeVertically(components));

        Debug.Log($"Group '{groupType}' processing completed");
    }

    private IEnumerator ResolveGroupCollisions(List<CircuitComponent> components)
    {
        bool hasCollisions = true;
        int maxIterations = components.Count * 2;
        int iteration = 0;

        while (hasCollisions && iteration < maxIterations)
        {
            iteration++;
            hasCollisions = false;

            foreach (CircuitComponent component in components)
            {
                if (component == null) continue;

                // ��������� ��������
                if (HasCollisions(component, components))
                {
                    hasCollisions = true;

                    // ������� ��������� ������� ������ �� ����������
                    Vector2 freePosition = FindFreePositionRight(component);

                    // ���������� ���������
                    yield return StartCoroutine(MoveComponentSmoothly(component, freePosition));
                }
            }
        }

        if (iteration >= maxIterations)
        {
            Debug.LogWarning($"Reached max iterations ({maxIterations}) for collision resolution");
        }
    }

    private IEnumerator ArrangeVertically(List<CircuitComponent> components)
    {
        if (components.Count == 0) yield break;

        // ��������� ���������� �� ������������ ������� (������ ����)
        components.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));

        // ������� ����� ������� ���������
        CircuitComponent topComponent = components[0];
        float currentY = topComponent.transform.position.y;

        // ��������� ��� ���������� ������ �� ���������
        for (int i = 0; i < components.Count; i++)
        {
            CircuitComponent component = components[i];
            if (component == null) continue;

            // �������� ������� ����������
            Bounds bounds = CalculateComponentBounds(component);
            float componentHeight = bounds.size.y;

            // ��������� ������� �������
            Vector2 targetPosition = new Vector2(
                component.transform.position.x,
                currentY
            );

            // ���� ��� �� ������ ���������, ��������� ����������
            if (i > 0)
            {
                currentY -= componentHeight + componentSpacing;
            }
            else
            {
                currentY -= componentHeight;
            }

            // ���������� ���������, ���� ������� ����������
            if (Vector2.Distance(component.transform.position, targetPosition) > 0.01f)
            {
                yield return StartCoroutine(MoveComponentSmoothly(component, targetPosition));
            }
        }
    }

    private bool HasCollisions(CircuitComponent component, List<CircuitComponent> otherComponents)
    {
        if (component == null) return false;

        Bounds bounds = CalculateComponentBounds(component);

        // ��������� �������� �� ����� ������� ������������
        foreach (CircuitComponent otherComponent in otherComponents)
        {
            if (otherComponent == null || otherComponent == component) continue;

            Bounds otherBounds = CalculateComponentBounds(otherComponent);

            if (bounds.Intersects(otherBounds))
            {
                return true;
            }
        }

        return false;
    }

    private Vector2 FindFreePositionRight(CircuitComponent component)
    {
        if (component == null) return Vector2.zero;

        Vector2 currentPosition = component.transform.position;
        Vector2 testPosition = currentPosition;

        // ���� ��������� ������� ������ �� ����������
        for (int i = 1; i <= 10; i++)
        {
            testPosition = currentPosition + new Vector2(i * gridSize, 0);
            testPosition = SnapToGrid(testPosition);

            // ���������, �������� �� �������
            if (IsPositionFree(testPosition, component))
            {
                return testPosition;
            }
        }

        // ���� �� ����� ��������� �������, ���������� ��������
        return currentPosition;
    }

    private bool IsPositionFree(Vector2 position, CircuitComponent ignoringComponent)
    {
        // ������� ��������� Bounds ��� ��������
        Bounds testBounds = new Bounds(position, CalculateComponentBounds(ignoringComponent).size);

        // ��������� �������� �� ����� ������������
        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>();
        foreach (CircuitComponent component in allComponents)
        {
            if (component == null || component == ignoringComponent) continue;

            Bounds componentBounds = CalculateComponentBounds(component);
            if (testBounds.Intersects(componentBounds))
            {
                return false;
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

        // ���� ������ �� �������, ���������� bounds �� ���������
        return new Bounds(component.transform.position, Vector3.one * gridSize);
    }

    // ����� ��� ������ �� ������ ��������
    public void ManualTriggerSnap()
    {
        if (!isProcessing)
        {
            StartCoroutine(ProcessAllComponents());
        }
    }
}