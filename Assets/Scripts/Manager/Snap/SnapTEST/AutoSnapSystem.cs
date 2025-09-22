using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AutoSnapSystem : MonoBehaviour
{
    [Header("Settings")]
    public KeyCode snapHotkey = KeyCode.P;
    public bool useControlModifier = true;
    public float gridSize = 1.0f;
    public float searchRadius = 3.0f;

    [Header("Animation Settings")]
    public float moveDuration = 0.3f;
    public bool enableSmoothMovement = true;

    private bool isProcessing = false;
    private List<CircuitComponent> processedComponents = new List<CircuitComponent>();

    void Update()
    {
        if (CheckHotkey() && !isProcessing)
        {
            StartCoroutine(SnapOverlappingComponents());
        }
    }

    private bool CheckHotkey()
    {
        if (useControlModifier)
            return Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(snapHotkey);
        else
            return Input.GetKeyDown(snapHotkey);
    }

    private IEnumerator SnapOverlappingComponents()
    {
        isProcessing = true;
        processedComponents.Clear();

        Debug.Log("=== AUTO SNAP STARTED ===");

        // �������������� ���������� ������
        Physics2D.SyncTransforms();
        yield return null; // ���� ���� ���� ��� ���������� ������

        // ������� ��� ����������, ������� ����������
        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>(true);
        Debug.Log($"Found {allComponents.Length} components total");

        // ��������� ������ �������� ����������
        List<CircuitComponent> activeComponents = new List<CircuitComponent>();
        foreach (CircuitComponent component in allComponents)
        {
            if (component != null && component.gameObject.activeInHierarchy)
            {
                activeComponents.Add(component);
            }
        }

        Debug.Log($"Processing {activeComponents.Count} active components");

        // ��������� �� ������ (����� ���������� ����� ������� ������)
        activeComponents.Sort((a, b) => b.componentNumber.CompareTo(a.componentNumber));

        foreach (CircuitComponent component in activeComponents)
        {
            if (component == null) continue;

            Debug.Log($"Processing component: {component.name}");

            // ��������� �������� � ����������� ��������������
            Physics2D.SyncTransforms();
            yield return null; // ���� ���������� ������

            bool hasCollisions = HasCollisions(component);
            Debug.Log($"Component {component.name} has collisions: {hasCollisions}");

            if (hasCollisions)
            {
                Debug.Log($"Component {component.name} has collisions, finding free position...");

                // ������� ��������� �������
                Vector3 freePosition = FindFreePosition(component.transform.position, component);
                Debug.Log($"Free position found: {freePosition}");

                if (freePosition != component.transform.position)
                {
                    // ���������� ���������
                    yield return StartCoroutine(MoveComponentSmoothly(component, freePosition));
                    Debug.Log($"Moved {component.name} to {freePosition}");

                    // ��������� � ������ ������������
                    processedComponents.Add(component);
                }
                else
                {
                    Debug.LogWarning($"No free position found for {component.name}");
                }
            }

            yield return null; // ���� ��������� ����
        }

        Debug.Log("=== AUTO SNAP COMPLETED ===");
        Debug.Log($"Processed {processedComponents.Count} components");

        isProcessing = false;
    }

    private bool HasCollisions(CircuitComponent component)
    {
        if (component == null) return false;

        // �������� ��� ���������� ����������
        Collider2D[] colliders = component.GetComponentsInChildren<Collider2D>();
        if (colliders.Length == 0)
        {
            Debug.LogWarning($"No colliders found for {component.name}");
            return false;
        }

        foreach (Collider2D collider in colliders)
        {
            if (collider == null || !collider.enabled || collider.isTrigger) continue;

            // ��������� ��������
            Collider2D[] overlaps = Physics2D.OverlapBoxAll(
                collider.bounds.center,
                collider.bounds.size,
                0
            );

            foreach (Collider2D overlap in overlaps)
            {
                if (overlap == null || !overlap.enabled || overlap.isTrigger) continue;
                if (overlap == collider) continue;

                // ���������, ����������� �� ��������� ������� ����������
                CircuitComponent otherComponent = overlap.GetComponentInParent<CircuitComponent>();
                if (otherComponent != null && otherComponent != component)
                {
                    // ���������� �������� � ��� ������������� ������������
                    if (!processedComponents.Contains(otherComponent))
                    {
                        Debug.Log($"Collision detected: {component.name} with {otherComponent.name}");
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private Vector3 FindFreePosition(Vector3 currentPosition, CircuitComponent ignoringComponent)
    {
        Vector3 bestPosition = currentPosition;
        float bestDistance = float.MaxValue;

        // ���� ��������� ������� � ������� ������
        for (float x = -searchRadius; x <= searchRadius; x += gridSize)
        {
            for (float y = -searchRadius; y <= searchRadius; y += gridSize)
            {
                if (x == 0 && y == 0) continue; // ���������� ������� �������

                Vector3 testPosition = currentPosition + new Vector3(x, y, 0);
                testPosition = SnapToGrid(testPosition);

                // ���������, �������� �� �������
                if (IsPositionFree(testPosition, ignoringComponent))
                {
                    float distance = Vector3.Distance(currentPosition, testPosition);
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

    private bool IsPositionFree(Vector3 position, CircuitComponent ignoringComponent)
    {
        // ��������� �������� � ������� �������
        Collider2D[] colliders = Physics2D.OverlapBoxAll(position, new Vector2(gridSize * 0.9f, gridSize * 0.9f), 0);

        foreach (Collider2D collider in colliders)
        {
            if (collider == null || !collider.enabled || collider.isTrigger) continue;

            CircuitComponent otherComponent = collider.GetComponentInParent<CircuitComponent>();
            if (otherComponent != null && otherComponent != ignoringComponent)
            {
                // ���������� �������� � ��� ������������� ������������
                if (!processedComponents.Contains(otherComponent))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private IEnumerator MoveComponentSmoothly(CircuitComponent component, Vector3 targetPosition)
    {
        if (component == null) yield break;

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

        if (enableSmoothMovement)
        {
            // ������� �����������
            float elapsed = 0f;
            Vector3 startPosition = component.transform.position;

            while (elapsed < moveDuration)
            {
                if (component == null) yield break;

                component.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / moveDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // ��������� �������
        if (component != null)
        {
            component.transform.position = targetPosition;
        }

        // ��������������� ��������� ������
        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
        }

        // ��������� ������
        Physics2D.SyncTransforms();
        yield return null; // ���� ���������� ������
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        float snapInverse = 1.0f / gridSize;
        float x = Mathf.Round(position.x * snapInverse) / snapInverse;
        float y = Mathf.Round(position.y * snapInverse) / snapInverse;
        return new Vector3(x, y, position.z);
    }

    // ����� ��� ������ �� ������ ��������
    public void ManualTriggerSnap()
    {
        if (!isProcessing)
        {
            StartCoroutine(SnapOverlappingComponents());
        }
    }

    // ������������ � ���������
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // ������������ ������������ �����������
        Gizmos.color = Color.green;
        foreach (CircuitComponent component in processedComponents)
        {
            if (component != null)
            {
                Gizmos.DrawWireSphere(component.transform.position, 0.3f);
            }
        }

        // ������������ ���������� �������
        Gizmos.color = Color.yellow;
        CircuitComponent[] components = FindObjectsOfType<CircuitComponent>();
        foreach (CircuitComponent component in components)
        {
            if (component != null && component.gameObject.activeInHierarchy)
            {
                Gizmos.DrawWireSphere(component.transform.position, searchRadius);
            }
        }
    }
}