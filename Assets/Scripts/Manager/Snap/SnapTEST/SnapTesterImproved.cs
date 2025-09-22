using UnityEngine;
using System.Collections.Generic;

public class SnapTesterImproved : MonoBehaviour
{
    [Header("Test Settings")]
    public KeyCode testKey = KeyCode.T;
    public bool includeInactiveComponents = true; // Важно: включать неактивные компоненты

    void Update()
    {
        if (Input.GetKeyDown(testKey))
        {
            TestSnapFunctionality();
        }
    }

    private void TestSnapFunctionality()
    {
        Debug.Log("=== IMPROVED SNAP TEST STARTED ===");

        // 1. Поиск всех ComponentManager в сцене
        ComponentManager[] componentManagers = FindObjectsOfType<ComponentManager>(includeInactiveComponents);
        Debug.Log($"Found {componentManagers.Length} component managers");

        // 2. Поиск всех CircuitComponent, включая неактивные
        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>(includeInactiveComponents);
        Debug.Log($"Found {allComponents.Length} circuit components (including inactive)");

        // 3. Проверка каждого компонента
        foreach (CircuitComponent component in allComponents)
        {
            if (component != null)
            {
                Debug.Log($"Component: {component.name}, Active: {component.gameObject.activeInHierarchy}, Parent: {GetParentPath(component.transform)}");

                // 4. Проверка DraggableCircle
                DraggableComponent draggable = component.GetComponentInChildren<DraggableComponent>(includeInactiveComponents);
                if (draggable != null)
                {
                    Debug.Log($"-- DraggableCircle found: {draggable.name}, Active: {draggable.gameObject.activeSelf}");

                    // 5. Проверка коллайдеров
                    Collider2D[] colliders = component.GetComponentsInChildren<Collider2D>(includeInactiveComponents);
                    Debug.Log($"-- Colliders found: {colliders.Length}");
                }
                else
                {
                    Debug.LogWarning($"-- No DraggableCircle found for {component.name}");
                }

                // 6. Проверка коллизий
                CheckComponentCollisions(component);
            }
        }

        // 7. Проверка сетки
        SnapGridSystem grid = FindObjectOfType<SnapGridSystem>();
        if (grid != null)
        {
            Debug.Log($"Grid found: {grid.name}, Points: {grid.GetAllPoints().Length}");
        }
        else
        {
            Debug.LogError("No grid system found!");
        }

        Debug.Log("=== IMPROVED SNAP TEST COMPLETED ===");
    }

    private string GetParentPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

    private void CheckComponentCollisions(CircuitComponent component)
    {
        Collider2D[] colliders = component.GetComponentsInChildren<Collider2D>(includeInactiveComponents);

        foreach (Collider2D collider in colliders)
        {
            if (collider != null && collider.enabled)
            {
                // Проверяем коллизии с помощью OverlapCircleAll
                Collider2D[] overlaps = Physics2D.OverlapCircleAll(
                    collider.bounds.center,
                    collider.bounds.size.magnitude / 2f
                );

                Debug.Log($"-- Collisions for {collider.name}: {overlaps.Length}");

                foreach (Collider2D overlap in overlaps)
                {
                    if (overlap != null && overlap.transform != component.transform &&
                        !overlap.transform.IsChildOf(component.transform))
                    {
                        Debug.Log($"--- Collision with: {overlap.name}");
                    }
                }
            }
        }
    }
}