using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class CompactVerticalAutoSnap : MonoBehaviour
{
    [Header("Hotkey Settings")]
    public KeyCode hotkey = KeyCode.P;

    [Header("Vertical Layout Settings")]
    public float verticalSpacing = 2.0f;
    public bool keepMinNumberStationary = true;

    [Header("Group Settings")]
    public int maxComponentsPerSubGroup = 5;
    public float groupSpacing = 5.0f;
    public float subGroupSpacing = 3.0f;

    [Header("Distance-Based Grouping")]
    public float groupingDistance = 15.0f;
    public bool useDistanceBasedGrouping = true;

    [Header("Container Settings")]
    public float containerSpacing = 20.0f;

    [Header("Animation")]
    public float moveDuration = 0.3f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Debug Visualization")]
    public bool showGroupLabels = true;
    public bool debugGrouping = true;
    public Color debugGroupingColor = Color.blue;

    private bool isProcessing = false;
    private SnapGridSystem gridSystem;
    private List<ComponentGroup> componentGroups = new List<ComponentGroup>();
    private static int groupsCreatedCount = 0;
    private float referenceYPosition;
    private GameObject mainContainer;

    void Start()
    {
        gridSystem = FindObjectOfType<SnapGridSystem>();

        // Находим главный контейнер или создаем его
        mainContainer = GameObject.Find("ComponentGroups");
        if (mainContainer == null)
        {
            mainContainer = new GameObject("ComponentGroups");
        }

        // Считаем количество уже существующих дочерних контейнеров
        groupsCreatedCount = mainContainer.transform.Cast<Transform>()
            .Count(child => child.name.StartsWith("ComponentGroups_"));
    }

    void Update()
    {
        if (Input.GetKeyDown(hotkey) && !isProcessing)
        {
            StartCoroutine(ArrangeComponentsVertically());
        }
    }

    private IEnumerator ArrangeComponentsVertically()
    {
        isProcessing = true;
        Debug.Log("Starting compact vertical arrangement...");

        componentGroups.Clear();
        ClearOnlyEmptyGroups();

        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();

        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>();

        // Исключаем компоненты, которые уже находятся в существующих группах
        HashSet<CircuitComponent> alreadyGroupedComponents = FindAlreadyGroupedComponents();

        // Удаляем дубликаты - оставляем только компоненты с уникальными номерами
        List<CircuitComponent> activeComponents = allComponents
            .Where(comp => comp != null && comp.gameObject.activeInHierarchy && !alreadyGroupedComponents.Contains(comp))
            .GroupBy(comp => $"{comp.componentType}{comp.componentNumber}")
            .Select(group => group.First())
            .ToList();

        Debug.Log($"Found {activeComponents.Count} unique active components");

        // Если нет новых компонентов для группировки, выходим
        if (activeComponents.Count == 0)
        {
            Debug.Log("No new components to arrange");
            isProcessing = false;
            yield break;
        }

        // Сортируем компоненты по их номерам
        activeComponents.Sort((a, b) => a.componentNumber.CompareTo(b.componentNumber));

        // Используем дистанционную группировку
        List<List<CircuitComponent>> distanceGroups = GroupComponentsByDistance(activeComponents);
        distanceGroups.RemoveAll(group => group.Count == 0);

        if (distanceGroups.Count == 0)
        {
            Debug.Log("No distance-based groups found");
            isProcessing = false;
            yield break;
        }

        // Сортируем группы по минимальному номеру компонента
        distanceGroups.Sort((a, b) =>
            a.Min(c => c.componentNumber).CompareTo(b.Min(c => c.componentNumber)));

        // Создаем новый дочерний контейнер
        groupsCreatedCount++;
        string containerName = $"ComponentGroups_{groupsCreatedCount}";
        GameObject groupsContainer = new GameObject(containerName);
        groupsContainer.transform.SetParent(mainContainer.transform);
        groupsContainer.transform.localPosition = Vector3.zero;

        // Находим самую правую позицию существующих контейнеров
        float rightmostX = FindRightmostContainerPosition();
        float currentXPosition = rightmostX + containerSpacing;

        // Находим высоту самого высокого компонента для выравнивания
        float maxComponentHeight = FindMaxComponentHeight(activeComponents);

        for (int groupIndex = 0; groupIndex < distanceGroups.Count; groupIndex++)
        {
            var group = distanceGroups[groupIndex];
            group.Sort((a, b) => a.componentNumber.CompareTo(b.componentNumber));

            ComponentGroup componentGroup = new ComponentGroup();
            componentGroup.groupIndex = groupIndex;
            componentGroup.groupContainer = new GameObject($"Group_{groupIndex + 1}");
            componentGroup.groupContainer.transform.SetParent(groupsContainer.transform);
            componentGroup.groupContainer.transform.position = new Vector3(currentXPosition, 0, 0);

            List<List<CircuitComponent>> subGroups = SplitIntoSubGroups(group, maxComponentsPerSubGroup);

            float totalGroupWidth = 0f;

            for (int subGroupIndex = 0; subGroupIndex < subGroups.Count; subGroupIndex++)
            {
                var subGroup = subGroups[subGroupIndex];
                subGroup.Sort((a, b) => a.componentNumber.CompareTo(b.componentNumber));

                ComponentSubGroup subGroupObj = new ComponentSubGroup();
                subGroupObj.subGroupIndex = subGroupIndex;
                subGroupObj.subGroupContainer = new GameObject($"Sub-group_{subGroupIndex + 1}");
                subGroupObj.subGroupContainer.transform.SetParent(componentGroup.groupContainer.transform);

                // Создаем имя для подгруппы
                string subGroupName = CreateSubGroupName(subGroup, subGroupIndex);
                subGroupObj.subGroupContainer.name = subGroupName;

                // Добавляем компоненты в подгруппу
                AddComponentsToSubGroup(subGroup, subGroupObj);

                componentGroup.subGroups.Add(subGroupObj);

                // Позиционируем подгруппу горизонтально
                subGroupObj.subGroupContainer.transform.localPosition = new Vector3(totalGroupWidth, 0, 0);

                // Расставляем компоненты в подгруппе вертикально с выравниванием по первому элементу
                if (subGroupIndex == 0)
                {
                    // Для первой подгруппы вычисляем референсную позицию
                    yield return StartCoroutine(ArrangeFirstSubGroup(subGroup, subGroupObj.subGroupContainer.transform, maxComponentHeight));
                }
                else
                {
                    // Для последующих подгрупп используем референсную позицию первой подгруппы
                    yield return StartCoroutine(ArrangeSubGroupWithReference(subGroup, subGroupObj.subGroupContainer.transform));
                }

                // Вычисляем размеры подгруппы
                Bounds subGroupBounds = CalculateSubGroupBounds(subGroup);
                totalGroupWidth += subGroupBounds.size.x + subGroupSpacing;

                if (debugGrouping)
                {
                    Debug.Log($"Sub-group {subGroupIndex + 1} in Group {groupIndex + 1} has {subGroup.Count} components");
                }
            }

            componentGroups.Add(componentGroup);

            // Увеличиваем позицию для следующей группы
            currentXPosition += totalGroupWidth + groupSpacing;

            if (groupIndex < distanceGroups.Count - 1)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }

        yield return StartCoroutine(CleanEmptyGroups(groupsContainer));
        LogGroupInformation();

        Debug.Log("Compact vertical arrangement completed");
        isProcessing = false;
    }

    // Метод группировки компонентов по расстоянию
    private List<List<CircuitComponent>> GroupComponentsByDistance(List<CircuitComponent> components)
    {
        List<List<CircuitComponent>> groups = new List<List<CircuitComponent>>();
        List<CircuitComponent> ungroupedComponents = new List<CircuitComponent>(components);

        while (ungroupedComponents.Count > 0)
        {
            CircuitComponent currentComponent = ungroupedComponents[0];
            ungroupedComponents.RemoveAt(0);

            List<CircuitComponent> currentGroup = new List<CircuitComponent> { currentComponent };

            // Ищем все компоненты, которые находятся рядом с текущим компонентом
            FindNearbyComponentsByDistance(currentComponent, currentGroup, ungroupedComponents);

            if (currentGroup.Count > 0)
            {
                groups.Add(currentGroup);

                if (debugGrouping)
                {
                    StringBuilder groupInfo = new StringBuilder();
                    groupInfo.Append($"Group {groups.Count}: [");
                    foreach (var comp in currentGroup)
                    {
                        groupInfo.Append($"{comp.componentType}{comp.componentNumber},");
                    }
                    groupInfo.Append("]");
                    Debug.Log(groupInfo.ToString());
                }
            }
        }

        return groups;
    }

    private void FindNearbyComponentsByDistance(CircuitComponent component, List<CircuitComponent> group,
                                               List<CircuitComponent> ungroupedComponents)
    {
        // Создаем временный список для безопасного удаления элементов
        List<CircuitComponent> componentsToRemove = new List<CircuitComponent>();

        foreach (CircuitComponent otherComponent in ungroupedComponents)
        {
            if (otherComponent != null && otherComponent != component)
            {
                float distance = Vector3.Distance(component.transform.position, otherComponent.transform.position);

                if (distance <= groupingDistance)
                {
                    if (debugGrouping)
                    {
                        Debug.Log($"Component {component.componentType}{component.componentNumber} is near {otherComponent.componentType}{otherComponent.componentNumber} (distance: {distance})");
                        Debug.DrawLine(component.transform.position, otherComponent.transform.position, debugGroupingColor, 2f);
                    }

                    group.Add(otherComponent);
                    componentsToRemove.Add(otherComponent);
                }
            }
        }

        // Удаляем сгруппированные компоненты из непомеченных
        foreach (CircuitComponent comp in componentsToRemove)
        {
            ungroupedComponents.Remove(comp);
        }

        // Рекурсивно ищем компоненты рядом с каждым найденном компонентом
        foreach (CircuitComponent comp in componentsToRemove)
        {
            FindNearbyComponentsByDistance(comp, group, ungroupedComponents);
        }
    }

    private Bounds CalculateSubGroupBounds(List<CircuitComponent> components)
    {
        if (components.Count == 0) return new Bounds(Vector3.zero, Vector3.zero);

        Bounds totalBounds = CalculateComponentBounds(components[0]);

        for (int i = 1; i < components.Count; i++)
        {
            if (components[i] != null)
            {
                Bounds componentBounds = CalculateComponentBounds(components[i]);
                totalBounds.Encapsulate(componentBounds);
            }
        }

        return totalBounds;
    }

    private IEnumerator CleanEmptyGroups(GameObject groupsContainer)
    {
        if (groupsContainer == null) yield break;

        List<GameObject> objectsToRemove = new List<GameObject>();

        foreach (Transform groupTransform in groupsContainer.transform)
        {
            bool groupHasComponents = false;

            foreach (Transform subGroupTransform in groupTransform)
            {
                bool subGroupHasComponents = false;

                foreach (Transform child in subGroupTransform)
                {
                    if (child.GetComponent<CircuitComponent>() != null)
                    {
                        subGroupHasComponents = true;
                        groupHasComponents = true;
                        break;
                    }
                }

                if (!subGroupHasComponents)
                {
                    objectsToRemove.Add(subGroupTransform.gameObject);
                }
            }

            if (!groupHasComponents)
            {
                objectsToRemove.Add(groupTransform.gameObject);
            }
        }

        foreach (GameObject obj in objectsToRemove)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }

        yield return null;
    }

    private List<List<CircuitComponent>> SplitIntoSubGroups(List<CircuitComponent> group, int maxSize)
    {
        List<List<CircuitComponent>> subGroups = new List<List<CircuitComponent>>();
        group.Sort((a, b) => a.componentNumber.CompareTo(b.componentNumber));

        for (int i = 0; i < group.Count; i += maxSize)
        {
            subGroups.Add(group.GetRange(i, Mathf.Min(maxSize, group.Count - i)));
        }

        return subGroups;
    }

    private IEnumerator ArrangeFirstSubGroup(List<CircuitComponent> components, Transform container, float maxComponentHeight)
    {
        if (components.Count == 0) yield break;

        Debug.Log($"Arranging first subgroup with {components.Count} components");

        components.Sort((a, b) => a.componentNumber.CompareTo(b.componentNumber));

        // Вычисляем общую высоту всех компонентов
        float totalHeight = CalculateTotalHeight(components);

        // Вычисляем начальную позицию Y (самый верхний компонент)
        referenceYPosition = container.position.y + (totalHeight / 2);

        for (int i = 0; i < components.Count; i++)
        {
            CircuitComponent component = components[i];
            if (component == null) continue;

            // Вычисляем целевую позицию по Y
            float targetY = referenceYPosition;

            // Вычитаем высоту всех предыдущих компонентов
            for (int j = 0; j < i; j++)
            {
                if (components[j] != null)
                {
                    Bounds bounds = CalculateComponentBounds(components[j]);
                    targetY -= bounds.size.y + verticalSpacing;
                }
            }

            // Привязываем к сетке
            Vector2 targetPosition = new Vector2(container.position.x, targetY);
            if (gridSystem != null)
            {
                targetPosition = gridSystem.GetNearestPoint(targetPosition);
            }

            // Перемещаем компонент
            yield return StartCoroutine(MoveComponentSmoothly(component, targetPosition));

            // Устанавливаем контейнер как родителя
            component.transform.SetParent(container);
        }
    }

    private IEnumerator ArrangeSubGroupWithReference(List<CircuitComponent> components, Transform container)
    {
        if (components.Count == 0) yield break;

        Debug.Log($"Arranging subgroup with reference Y position: {referenceYPosition}");

        components.Sort((a, b) => a.componentNumber.CompareTo(b.componentNumber));

        for (int i = 0; i < components.Count; i++)
        {
            CircuitComponent component = components[i];
            if (component == null) continue;

            // Вычисляем целевую позицию по Y относительно референсной позиции
            float targetY = referenceYPosition;

            // Вычитаем высоту всех предыдущих компонентов
            for (int j = 0; j < i; j++)
            {
                if (components[j] != null)
                {
                    Bounds bounds = CalculateComponentBounds(components[j]);
                    targetY -= bounds.size.y + verticalSpacing;
                }
            }

            // Привязываем к сетке
            Vector2 targetPosition = new Vector2(container.position.x, targetY);
            if (gridSystem != null)
            {
                targetPosition = gridSystem.GetNearestPoint(targetPosition);
            }

            // Перемещаем компонент
            yield return StartCoroutine(MoveComponentSmoothly(component, targetPosition));

            // Устанавливаем контейнер как родителя
            component.transform.SetParent(container);
        }
    }

    private IEnumerator MoveComponentSmoothly(CircuitComponent component, Vector2 targetPosition)
    {
        if (component == null) yield break;

        Vector2 startPosition = component.transform.position;
        float elapsed = 0f;

        Rigidbody2D rb = component.GetComponentInChildren<Rigidbody2D>();
        bool wasKinematic = false;

        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
        }

        while (elapsed < moveDuration)
        {
            float t = moveCurve.Evaluate(elapsed / moveDuration);
            component.transform.position = Vector2.Lerp(startPosition, targetPosition, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        component.transform.position = targetPosition;

        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
        }

        Physics2D.SyncTransforms();
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

        return totalHeight - verticalSpacing;
    }

    private Bounds CalculateComponentBounds(CircuitComponent component)
    {
        if (component == null)
            return new Bounds(Vector3.zero, Vector3.zero);

        // Кэшируем компоненты для более быстрого доступа
        DraggableComponent draggable = component.GetComponentInChildren<DraggableComponent>();
        if (draggable != null)
        {
            Renderer renderer = draggable.GetComponent<Renderer>();
            if (renderer != null) return renderer.bounds;

            Collider2D collider = draggable.GetComponent<Collider2D>();
            if (collider != null) return collider.bounds;
        }

        // Если не нашли, ищем любой рендерер или коллайдер в компоненте и его детях
        Renderer rendererInChildren = component.GetComponentInChildren<Renderer>();
        if (rendererInChildren != null)
        {
            Renderer[] renderers = component.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].enabled)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }
            return bounds;
        }

        Collider2D colliderInChildren = component.GetComponentInChildren<Collider2D>();
        if (colliderInChildren != null)
        {
            Collider2D[] colliders = component.GetComponentsInChildren<Collider2D>();
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

        return new Bounds(component.transform.position, new Vector3(1f, 1f, 0f));
    }

    private float FindMaxComponentHeight(List<CircuitComponent> components)
    {
        float maxHeight = 0f;

        foreach (CircuitComponent component in components)
        {
            if (component != null)
            {
                Bounds bounds = CalculateComponentBounds(component);
                if (bounds.size.y > maxHeight)
                {
                    maxHeight = bounds.size.y;
                }
            }
        }

        return maxHeight;
    }

    private void ClearOnlyEmptyGroups()
    {
        if (mainContainer == null) return;

        List<GameObject> containersToRemove = new List<GameObject>();

        foreach (Transform containerTransform in mainContainer.transform)
        {
            CircuitComponent[] components = containerTransform.GetComponentsInChildren<CircuitComponent>();
            if (components.Length == 0)
            {
                containersToRemove.Add(containerTransform.gameObject);
            }
        }

        foreach (GameObject container in containersToRemove)
        {
            DestroyImmediate(container);
        }
    }

    private HashSet<CircuitComponent> FindAlreadyGroupedComponents()
    {
        HashSet<CircuitComponent> groupedComponents = new HashSet<CircuitComponent>();

        if (mainContainer == null) return groupedComponents;

        foreach (Transform containerTransform in mainContainer.transform)
        {
            CircuitComponent[] componentsInContainer = containerTransform.GetComponentsInChildren<CircuitComponent>();
            foreach (CircuitComponent comp in componentsInContainer)
            {
                groupedComponents.Add(comp);
            }
        }

        return groupedComponents;
    }

    private float FindRightmostContainerPosition()
    {
        float rightmostX = 0f;

        if (mainContainer == null) return rightmostX;

        foreach (Transform containerTransform in mainContainer.transform)
        {
            // Находим самый правый компонент в контейнере
            CircuitComponent[] components = containerTransform.GetComponentsInChildren<CircuitComponent>();
            foreach (CircuitComponent comp in components)
            {
                if (comp != null)
                {
                    Bounds bounds = CalculateComponentBounds(comp);
                    float componentRightEdge = comp.transform.position.x + bounds.extents.x;
                    if (componentRightEdge > rightmostX)
                    {
                        rightmostX = componentRightEdge;
                    }
                }
            }
        }

        return rightmostX;
    }

    private string CreateSubGroupName(List<CircuitComponent> subGroup, int subGroupIndex)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"Sub-group {subGroupIndex + 1} [");

        List<string> componentIdentifiers = subGroup
            .Where(comp => comp != null)
            .Select(comp => $"{comp.componentType}{comp.componentNumber}")
            .ToList();

        sb.Append(string.Join(",", componentIdentifiers));
        sb.Append("]");

        return sb.ToString();
    }

    private void AddComponentsToSubGroup(List<CircuitComponent> subGroup, ComponentSubGroup subGroupObj)
    {
        foreach (CircuitComponent comp in subGroup)
        {
            if (comp != null)
            {
                string componentId = $"{comp.componentType}{comp.componentNumber}";
                subGroupObj.components.Add(comp);
                subGroupObj.componentIds.Add(componentId);
            }
        }
    }

    private void LogGroupInformation()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("=== COMPONENT GROUP INFORMATION ===");

        List<ComponentGroup> validGroups = componentGroups
            .Where(group => group.groupContainer != null)
            .ToList();

        foreach (ComponentGroup group in validGroups)
        {
            log.AppendLine($"Group {group.groupIndex + 1}:");

            foreach (ComponentSubGroup subGroup in group.subGroups)
            {
                if (subGroup.subGroupContainer == null) continue;

                log.AppendLine($"  {subGroup.subGroupContainer.name}");

                foreach (string componentId in subGroup.componentIds)
                {
                    log.AppendLine($"    - {componentId}");
                }
            }

            log.AppendLine();
        }

        componentGroups = validGroups;

        Debug.Log(log.ToString());
    }

    public void ManualTriggerSnap()
    {
        if (!isProcessing)
        {
            StartCoroutine(ArrangeComponentsVertically());
        }
    }

    void OnDrawGizmos()
    {
        if (!debugGrouping) return;

        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>();
        foreach (CircuitComponent component in allComponents)
        {
            if (component != null && component.gameObject.activeInHierarchy)
            {
                Gizmos.color = debugGroupingColor;
                Gizmos.DrawWireSphere(component.transform.position, groupingDistance);
            }
        }
    }
}

[System.Serializable]
public class ComponentGroup
{
    public int groupIndex;
    public GameObject groupContainer;
    public List<ComponentSubGroup> subGroups = new List<ComponentSubGroup>();
}

[System.Serializable]
public class ComponentSubGroup
{
    public int subGroupIndex;
    public GameObject subGroupContainer;
    public List<CircuitComponent> components = new List<CircuitComponent>();
    public List<string> componentIds = new List<string>();
}