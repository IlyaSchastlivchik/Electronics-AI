using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ComprehensiveAutoSnap : MonoBehaviour
{
    [Header("Settings")]
    public KeyCode hotkey = KeyCode.P;
    public float gridSize = 1.0f;
    public int maxSearchSteps = 15;

    [Header("Debug")]
    public bool drawDebugGizmos = true;
    public Color collisionColor = Color.red;
    public Color freeColor = Color.green;

    private bool isProcessing = false;
    private List<CircuitComponent> processedComponents = new List<CircuitComponent>();

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
        Debug.Log("Starting collision resolution...");

        // �������������� ������������� ������
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();

        // �������� ��� ����������
        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>();
        List<CircuitComponent> activeComponents = allComponents
            .Where(c => c != null && c.gameObject.activeInHierarchy)
            .OrderByDescending(c => c.componentNumber)
            .ToList();

        // �������������� ��������� ��� ����������� ���������� ��������
        for (int pass = 0; pass < 3; pass++)
        {
            bool movedAnyComponent = false;

            foreach (CircuitComponent component in activeComponents)
            {
                if (component == null || processedComponents.Contains(component)) continue;

                // ��������� ��������
                if (HasCollisions(component))
                {
                    Debug.Log($"Collision detected for {component.name}");

                    // ������� ��������� �������
                    Vector2 freePosition = FindFreePosition(component);

                    if (freePosition != (Vector2)component.transform.position)
                    {
                        // ���������� ���������
                        yield return StartCoroutine(MoveComponentSmoothly(component, freePosition));
                        movedAnyComponent = true;
                        processedComponents.Add(component);
                    }
                }
                else
                {
                    processedComponents.Add(component);
                }

                yield return null;
            }

            if (!movedAnyComponent) break;
        }

        Debug.Log("Collision resolution completed");
        isProcessing = false;
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
                if (otherComponent != null &&
                    otherComponent != component &&
                    !processedComponents.Contains(otherComponent))
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
        float duration = 0.3f;
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
        while (elapsed < duration)
        {
            component.transform.position = Vector2.Lerp(startPosition, targetPosition, elapsed / duration);
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
}