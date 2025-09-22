using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class VerticalAutoSnap : MonoBehaviour
{
    [Header("Hotkey Settings")]
    public KeyCode hotkey = KeyCode.P;

    [Header("Snap Settings")]
    public float gridSize = 1.0f;
    public float verticalSpacing = 0.2f;

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
        Debug.Log("Starting vertical component processing...");

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

        Debug.Log("Vertical component processing completed");
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

        // ��� 1: ������� ����� ����� �� X ��� ���� ����������� ������
        float centerX = CalculateGroupCenterX(components);

        // ��� 2: ��������� �������� � ����������� �� ���������
        yield return StartCoroutine(ResolveCollisionsAndArrangeVertically(components, centerX));

        Debug.Log($"Group '{groupType}' processing completed");
    }

    private float CalculateGroupCenterX(List<CircuitComponent> components)
    {
        if (components.Count == 0) return 0;

        // ��������� ������� ������� �� X ��� ���� ����������� ������
        float sumX = 0;
        foreach (CircuitComponent component in components)
        {
            if (component != null)
            {
                sumX += component.transform.position.x;
            }
        }

        return sumX / components.Count;
    }

    private IEnumerator ResolveCollisionsAndArrangeVertically(List<CircuitComponent> components, float centerX)
    {
        if (components.Count == 0) yield break;

        // ��������� ���������� �� ������� ������� Y (������ ����)
        components.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));

        // ��������� ����� ������ ���� �����������
        float totalHeight = CalculateTotalHeight(components);

        // ��������� ��������� ������� Y (����� ������� ���������)
        float startY = components[0].transform.position.y;

        // ��������� ���������� �� ���������
        for (int i = 0; i < components.Count; i++)
        {
            CircuitComponent component = components[i];
            if (component == null) continue;

            // ��������� ������� ������� �� Y
            float targetY = startY;
            for (int j = 0; j < i; j++)
            {
                if (j < components.Count && components[j] != null)
                {
                    Bounds bounds = CalculateComponentBounds(components[j]);
                    targetY -= bounds.size.y + verticalSpacing;
                }
            }

            // ������� ������� �������
            Vector2 targetPosition = new Vector2(centerX, targetY);

            // ���������� ���������
            yield return StartCoroutine(MoveComponentSmoothly(component, targetPosition));
        }
    }

    private float CalculateTotalHeight(List<CircuitComponent> components)
    {
        float totalHeight = 0;

        foreach (CircuitComponent component in components)
        {
            if (component != null)
            {
                Bounds bounds = CalculateComponentBounds(component);
                totalHeight += bounds.size.y + verticalSpacing;
            }
        }

        return totalHeight - verticalSpacing; // ������� ��������� ����������
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