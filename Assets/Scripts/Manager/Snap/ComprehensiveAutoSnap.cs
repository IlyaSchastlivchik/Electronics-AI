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

        // Принудительная синхронизация физики
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();

        // Получаем все компоненты
        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>();
        List<CircuitComponent> activeComponents = allComponents
            .Where(c => c != null && c.gameObject.activeInHierarchy)
            .OrderByDescending(c => c.componentNumber)
            .ToList();

        // Многопроходная обработка для устойчивого разрешения коллизий
        for (int pass = 0; pass < 3; pass++)
        {
            bool movedAnyComponent = false;

            foreach (CircuitComponent component in activeComponents)
            {
                if (component == null || processedComponents.Contains(component)) continue;

                // Проверяем коллизии
                if (HasCollisions(component))
                {
                    Debug.Log($"Collision detected for {component.name}");

                    // Находим свободную позицию
                    Vector2 freePosition = FindFreePosition(component);

                    if (freePosition != (Vector2)component.transform.position)
                    {
                        // Перемещаем компонент
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

        // Используем OverlapBoxAll для точного обнаружения коллизий
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

        // Ищем свободную позицию по спирали от текущей позиции
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

                // Проверяем, свободна ли позиция
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
        // Создаем временный Bounds для проверки
        Bounds testBounds = new Bounds(position, size);

        // Проверяем коллизии в целевой позиции
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

        // Получаем Rigidbody2D если есть
        Rigidbody2D rb = component.GetComponentInChildren<Rigidbody2D>();
        bool wasKinematic = false;

        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
        }

        // Плавное перемещение
        while (elapsed < duration)
        {
            component.transform.position = Vector2.Lerp(startPosition, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Финальная позиция
        component.transform.position = targetPosition;

        // Восстанавливаем физику
        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
        }

        // Синхронизируем физику
        Physics2D.SyncTransforms();
    }

    private Vector2 SnapToGrid(Vector2 position)
    {
        float snapInverse = 1.0f / gridSize;
        float x = Mathf.Round(position.x * snapInverse) / snapInverse;
        float y = Mathf.Round(position.y * snapInverse) / snapInverse;
        return new Vector2(x, y);
    }

    // Визуализация для отладки
    private void OnDrawGizmos()
    {
        if (!drawDebugGizmos || !Application.isPlaying) return;

        // Визуализация коллизий
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

        // Получаем все рендереры в компоненте и его детях
        Renderer[] renderers = component.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            // Используем первый рендерер как основу
            Bounds bounds = renderers[0].bounds;

            // Расширяем границы для включения всех остальных рендереров
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].enabled)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }
            return bounds;
        }

        // Если рендереров нет, проверяем коллайдеры
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

        // Если нет ни рендереров, ни коллайдеров, используем GeometryUtility.CalculateBounds
        // Получаем все дочерние объекты и их позиции
        List<Vector3> allPositions = new List<Vector3>();
        GetAllChildPositions(component.transform, ref allPositions);

        if (allPositions.Count > 0)
        {
            return GeometryUtility.CalculateBounds(allPositions.ToArray(), Matrix4x4.identity);
        }

        // Если ничего не найдено, возвращаем bounds по умолчанию
        return new Bounds(component.transform.position, Vector3.one * gridSize);
    }

    private void GetAllChildPositions(Transform parent, ref List<Vector3> positions)
    {
        if (parent == null) return;

        // Добавляем позицию текущего объекта
        positions.Add(parent.position);

        // Рекурсивно добавляем позиции всех детей
        for (int i = 0; i < parent.childCount; i++)
        {
            GetAllChildPositions(parent.GetChild(i), ref positions);
        }
    }
}