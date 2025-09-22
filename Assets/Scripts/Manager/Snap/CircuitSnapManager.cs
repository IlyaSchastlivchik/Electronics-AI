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

        // Получаем все компоненты
        circuitComponents = FindObjectsOfType<CircuitComponent>().ToList();

        // Сортируем по номеру компонента (по убыванию)
        var sortedComponents = circuitComponents
            .OrderByDescending(c => c.componentNumber)
            .ToList();

        foreach (var component in sortedComponents)
        {
            if (HasCollisions(component))
            {
                Debug.Log($"Component {component.componentId} has collisions, searching for free position...");

                // Используем метод, аналогичный DraggableComponent для безопасного перемещения
                if (SafeMoveComponentToFreePosition(component))
                {
                    Debug.Log($"Successfully moved {component.componentId}");
                }
                else
                {
                    Debug.LogWarning($"Could not find free position for {component.componentId}");
                }

                // Ждем следующий кадр для распределения нагрузки
                yield return null;
            }
        }

        Debug.Log("Auto-snap process completed");
        isProcessing = false;
    }

    private bool SafeMoveComponentToFreePosition(CircuitComponent component)
    {
        // Получаем DraggableComponent для управления физикой
        DraggableComponent draggable = component.GetComponentInChildren<DraggableComponent>();
        if (draggable == null)
        {
            Debug.LogError($"No DraggableComponent found for {component.componentId}");
            return false;
        }

        // Получаем Rigidbody2D
        Rigidbody2D rb = draggable.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError($"No Rigidbody2D found for {component.componentId}");
            return false;
        }

        // Сохраняем исходное состояние
        bool wasKinematic = rb.isKinematic;

        // Устанавливаем kinematic для плавного перемещения
        rb.isKinematic = true;

        // Ищем свободную позицию
        Vector3 freePosition = FindFreePosition(component);

        if (freePosition != component.transform.position)
        {
            // Перемещаем компонент
            component.transform.position = freePosition;

            // Применяем примагничивание к сетке
            SnapToGrid(draggable);

            Debug.Log($"Moved {component.componentId} to {freePosition}");
        }

        // Восстанавливаем исходное состояние
        rb.isKinematic = wasKinematic;

        // Принудительно обновляем физику
        Physics2D.SyncTransforms();

        return true;
    }

    private Vector3 FindFreePosition(CircuitComponent component)
    {
        Vector3 currentPos = component.transform.position;
        Vector3 bestPosition = currentPos;
        float bestDistance = float.MaxValue;

        // Проверяем позиции в радиусе поиска
        for (float x = -searchRadius; x <= searchRadius; x += gridSize)
        {
            for (float y = -searchRadius; y <= searchRadius; y += gridSize)
            {
                Vector3 testPosition = currentPos + new Vector3(x, y, 0);

                // Пропускаем слишком далекие позиции
                if (Vector3.Distance(currentPos, testPosition) > searchRadius)
                    continue;

                // Примагничиваем к сетке
                testPosition = SnapToGridPosition(testPosition);

                // Проверяем, свободна ли позиция
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
        // Используем метод из DraggableComponent для примагничивания
        draggable.SnapToPosition(draggable.transform.position);
    }

    private bool HasCollisions(CircuitComponent component)
    {
        // Проверяем коллизии с помощью Physics2D.OverlapCircleAll
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
        // Проверяем коллизии с помощью Physics2D.OverlapCircleAll
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