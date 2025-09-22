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

        // Принудительное обновление физики
        Physics2D.SyncTransforms();
        yield return null;

        // Получаем все компоненты
        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>(true);
        List<CircuitComponent> activeComponents = allComponents
            .Where(c => c != null && c.gameObject.activeInHierarchy)
            .ToList();

        Debug.Log($"Processing {activeComponents.Count} active components");

        // Определяем компоненты, которые уже правильно positioned
        UpdateAlreadyPositionedComponents(activeComponents);

        // Сортируем компоненты по типу и номеру
        var groupedComponents = activeComponents
            .GroupBy(c => c.componentType)
            .OrderBy(g => g.Key);

        foreach (var group in groupedComponents)
        {
            Debug.Log($"Processing group: {group.Key}");

            // Для каждого типа компонентов сортируем по номеру (сначала младшие номера)
            var sortedComponents = group
                .OrderBy(c => c.componentNumber)
                .ToList();

            // Строим карту занятости сетки
            BuildOccupancyMap(activeComponents);

            // Обрабатываем каждый компонент в группе
            foreach (CircuitComponent component in sortedComponents)
            {
                if (component == null) continue;

                // Пропускаем уже positioned компоненты
                if (alreadyPositionedComponents.Contains(component))
                {
                    Debug.Log($"Skipping already positioned component: {component.name}");
                    continue;
                }

                // Получаем занимаемые компонентом узлы сетки
                HashSet<Vector2> componentGridPoints = GetComponentGridPoints(component);

                // Проверяем коллизии с другими компонентами
                if (HasCollisions(componentGridPoints, component))
                {
                    Debug.Log($"Component {component.name} has collisions, resolving...");

                    // Находим свободное место для компонента с учетом приоритета нумерации
                    Vector2 freePosition = FindFreePositionWithPriority(component, componentGridPoints, group.Key);

                    if (freePosition != (Vector2)component.transform.position)
                    {
                        // Перемещаем компонент
                        MoveComponentToPosition(component, freePosition);

                        // Обновляем карту занятости
                        BuildOccupancyMap(activeComponents);

                        Debug.Log($"Moved {component.name} to {freePosition}");
                        yield return null;
                    }
                }
                else
                {
                    // Если коллизий нет, добавляем в список already positioned
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
        // Очищаем список и заново проверяем какие компоненты уже правильно positioned
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

        // Получаем размеры компонента в единицах сетки
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

        // Ищем свободное место с учетом приоритета нумерации
        for (int step = 1; step <= maxSearchSteps; step++)
        {
            // Пробуем позиции в разных направлениях с приоритетом для оси X
            for (int x = 0; x <= step; x++)
            {
                for (int y = 0; y <= step; y++)
                {
                    if (x == 0 && y == 0) continue;

                    // Пробуем все комбинации направлений
                    for (int xDir = -1; xDir <= 1; xDir += 2)
                    {
                        for (int yDir = -1; yDir <= 1; yDir += 2)
                        {
                            Vector2 testPosition = currentPosition +
                                                  new Vector2(x * xDir * gridSize, y * yDir * gridSize);
                            testPosition = SnapToGrid(testPosition);

                            // Проверяем, свободна ли позиция
                            if (IsPositionFree(testPosition, gridStepsX, gridStepsY, component))
                            {
                                // Вычисляем приоритет позиции
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

            // Если нашли подходящую позицию, возвращаем ее
            if (bestPosition != currentPosition)
                break;
        }

        return bestPosition;
    }

    private float CalculatePositionPriority(Vector2 position, CircuitComponent component, string componentType)
    {
        float priority = 0f;

        // Приоритет для оси X: меньшие номера должны быть левее
        if (component.componentType == componentType)
        {
            // Для резисторов и других компонентов: меньшие номера левее
            priority -= position.x * 10f; // Чем левее, тем выше приоритет

            // Для оси Y: меньшие номера должны быть выше
            priority += position.y * 5f; // Чем выше, тем выше приоритет
        }

        return priority;
    }

    private void BuildOccupancyMap(List<CircuitComponent> components)
    {
        occupiedGridPoints.Clear();

        foreach (CircuitComponent component in components)
        {
            if (component == null) continue;

            // Получаем коллайдер DraggableCircle
            DraggableComponent draggable = component.GetComponentInChildren<DraggableComponent>();
            if (draggable == null) continue;

            Collider2D collider = draggable.GetComponent<Collider2D>();
            if (collider == null) continue;

            // Определяем занимаемые узлы сетки
            Bounds bounds = collider.bounds;
            Vector2 center = bounds.center;
            Vector2 size = bounds.size;

            // Вычисляем занимаемые узлы сетки
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

        // Получаем коллайдер DraggableCircle
        DraggableComponent draggable = component.GetComponentInChildren<DraggableComponent>();
        if (draggable == null) return gridPoints;

        Collider2D collider = draggable.GetComponent<Collider2D>();
        if (collider == null) return gridPoints;

        // Определяем занимаемые узлы сетки
        Bounds bounds = collider.bounds;
        Vector2 center = bounds.center;
        Vector2 size = bounds.size;

        // Вычисляем занимаемые узлы сетки
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
        // Проверяем, пересекаются ли узлы компонента с занятыми узлами
        foreach (Vector2 point in componentGridPoints)
        {
            if (occupiedGridPoints.Contains(point))
            {
                // Игнорируем коллизии с самим собой
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
        // Проверяем, свободны ли все узлы в области компонента
        for (int x = -gridStepsX; x <= gridStepsX; x++)
        {
            for (int y = -gridStepsY; y <= gridStepsY; y++)
            {
                Vector2 gridPoint = SnapToGrid(position + new Vector2(x * gridSize, y * gridSize));

                if (occupiedGridPoints.Contains(gridPoint))
                {
                    // Игнорируем коллизии с самим собой
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

        // Получаем DraggableComponent для управления физикой
        DraggableComponent draggable = component.GetComponentInChildren<DraggableComponent>();
        Rigidbody2D rb = draggable != null ? draggable.GetComponent<Rigidbody2D>() : null;

        // Сохраняем состояние физики
        bool wasKinematic = false;
        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Перемещаем компонент
        component.transform.position = targetPosition;

        // Восстанавливаем состояние физики
        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
        }

        // Обновляем физику
        Physics2D.SyncTransforms();
    }

    private Vector2 SnapToGrid(Vector2 position)
    {
        float snapInverse = 1.0f / gridSize;
        float x = Mathf.Round(position.x * snapInverse) / snapInverse;
        float y = Mathf.Round(position.y * snapInverse) / snapInverse;
        return new Vector2(x, y);
    }

    // Визуализация занятых узлов сетки
    private void OnDrawGizmos()
    {
        if (!visualizeOccupiedGrid || !Application.isPlaying) return;

        // Визуализация занятых узлов сетки
        Gizmos.color = occupiedColor;
        foreach (Vector2 point in occupiedGridPoints)
        {
            Gizmos.DrawWireCube(point, Vector3.one * gridSize * 0.8f);
        }

        // Визуализация already positioned компонентов
        Gizmos.color = freeColor;
        foreach (CircuitComponent component in alreadyPositionedComponents)
        {
            if (component != null && component.gameObject.activeInHierarchy)
            {
                Gizmos.DrawWireSphere(component.transform.position, gridSize * 0.5f);
            }
        }
    }

    // Метод для вызова из других скриптов
    public void ManualTriggerSnap()
    {
        if (!isProcessing)
        {
            StartCoroutine(ResolveCollisions());
        }
    }
}