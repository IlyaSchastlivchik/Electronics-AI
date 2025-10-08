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

    [Header("SubGroup Settings")]
    public int maxComponentsPerSubGroup = 5;
    public float subGroupSpacing = 3.0f;

    [Header("Group Spacing Settings")]
    public float groupSpacing = 15.0f;

    [Header("Animation Settings")]
    [Tooltip("Длительность анимации для одного компонента")]
    public float componentMoveDuration = 0.5f;

    [Tooltip("Задержка между началом анимации компонентов")]
    public float delayBetweenComponents = 0.2f;

    [Tooltip("Кривая анимации перемещения")]
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Camera System Integration")]
    public CameraManager cameraManager;

    [Header("Container Monitoring")]
    public float containerCheckInterval = 0.5f;
    public bool autoMonitorContainers = true;

    [Header("Debug Visualization")]
    public bool showGroupLabels = true;
    public bool debugGrouping = true;
    public Color debugGroupingColor = Color.blue;

    private bool isProcessing = false;
    private List<ComponentGroup> componentGroups = new List<ComponentGroup>();

    private Dictionary<string, Transform> classMarkers = new Dictionary<string, Transform>();
    private Dictionary<string, Transform> classContainers = new Dictionary<string, Transform>();
    private Dictionary<string, List<GameObject>> groupContainersByType = new Dictionary<string, List<GameObject>>();
    private GameObject markersContainer;
    private GameObject autoSnapContainersRoot;

    private int groupCounter = 0;
    private List<Coroutine> activeAnimationCoroutines = new List<Coroutine>();
    private Coroutine containerMonitorCoroutine;
    private HashSet<string> activeClasses = new HashSet<string>();

    void Start()
    {
        InitializeCameraManager();
        InitializeClassMarkers();
        CreateClassContainers();
        InitializeGroupContainers();

        if (autoMonitorContainers)
        {
            StartContainerMonitoring();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(hotkey) && !isProcessing)
        {
            StopAllActiveAnimations();
            StartCoroutine(ArrangeComponentsVertically());
        }
    }

    private void InitializeCameraManager()
    {
        if (cameraManager == null)
        {
            cameraManager = FindObjectOfType<CameraManager>();
            if (cameraManager == null)
            {
                Debug.LogWarning("CameraManager не найден в сцене!");
            }
            else
            {
                Debug.Log("CameraManager автоматически найден и присвоен.");
            }
        }
    }

    private void StartContainerMonitoring()
    {
        if (containerMonitorCoroutine != null)
        {
            StopCoroutine(containerMonitorCoroutine);
        }
        containerMonitorCoroutine = StartCoroutine(MonitorContainers());
    }

    private IEnumerator MonitorContainers()
    {
        while (true)
        {
            yield return new WaitForSeconds(containerCheckInterval);
            CheckAllContainersForSubgroups();
        }
    }

    private void CheckAllContainersForSubgroups()
    {
        if (autoSnapContainersRoot == null) return;

        HashSet<string> currentActiveClasses = new HashSet<string>();

        foreach (Transform classContainer in autoSnapContainersRoot.transform)
        {
            string containerName = classContainer.name;
            if (containerName.StartsWith("AutoSnapClass_"))
            {
                string className = containerName.Replace("AutoSnapClass_", "");

                // Проверяем есть ли подгруппы в этом контейнере
                bool hasSubgroups = CheckForSubgroups(classContainer, className);

                if (hasSubgroups)
                {
                    currentActiveClasses.Add(className);

                    // Если этот класс ранее не был активен - активируем камеру
                    if (!activeClasses.Contains(className))
                    {
                        ActivateClassCamera(className);
                    }
                }
            }
        }

        // Деактивируем камеры для классов, которые больше не активны
        foreach (string previouslyActiveClass in activeClasses)
        {
            if (!currentActiveClasses.Contains(previouslyActiveClass))
            {
                DeactivateClassCamera(previouslyActiveClass);
            }
        }

        activeClasses = currentActiveClasses;
    }

    private bool CheckForSubgroups(Transform classContainer, string className)
    {
        foreach (Transform child in classContainer)
        {
            if (child.name.StartsWith($"{className}_Subgroups_"))
            {
                // Проверяем есть ли дочерние объекты в подгруппе
                if (child.childCount > 0)
                {
                    return true;
                }

                // Или проверяем есть ли компоненты в подгруппах
                foreach (Transform subGroup in child)
                {
                    if (subGroup.childCount > 0)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private void ActivateClassCamera(string className)
    {
        if (cameraManager != null)
        {
            cameraManager.ActivateCameraForClass(className);
            Debug.Log($"Активирована камера для класса {className} (обнаружены подгруппы)");
        }
        else
        {
            Debug.LogWarning($"CameraManager не доступен для активации камеры класса {className}");
        }
    }

    private void DeactivateClassCamera(string className)
    {
        if (cameraManager != null)
        {
            cameraManager.DeactivateCameraForClass(className);
            Debug.Log($"Деактивирована камера для класса {className} (подгруппы отсутствуют)");
        }
    }

    // Остальные методы остаются без изменений до метода ArrangeComponentsForType

    private void InitializeClassMarkers()
    {
        classMarkers.Clear();

        markersContainer = GameObject.Find("ClassMarkers");
        if (markersContainer == null)
        {
            Debug.LogWarning("ClassMarkers container not found!");
            return;
        }

        foreach (Transform marker in markersContainer.transform)
        {
            if (marker.name.StartsWith("ClassMarker_"))
            {
                string className = marker.name.Replace("ClassMarker_", "");
                classMarkers[className] = marker;

                if (debugGrouping)
                {
                    Debug.Log($"Found class marker: {className} at position {marker.position}");
                }
            }
        }
    }

    private void CreateClassContainers()
    {
        classContainers.Clear();

        autoSnapContainersRoot = GameObject.Find("AutoSnapContainers");
        if (autoSnapContainersRoot == null)
        {
            autoSnapContainersRoot = new GameObject("AutoSnapContainers");
        }

        string[] knownClasses = { "R", "C", "L", "D", "U", "G", "Q", "J", "K", "S", "Z", "O", "X", "A", "P" };

        foreach (string className in knownClasses)
        {
            if (classMarkers.ContainsKey(className))
            {
                Transform markerTransform = classMarkers[className];

                Transform existingContainer = autoSnapContainersRoot.transform.Find($"AutoSnapClass_{className}");
                if (existingContainer != null)
                {
                    classContainers[className] = existingContainer;
                }
                else
                {
                    GameObject classContainer = new GameObject($"AutoSnapClass_{className}");
                    classContainer.transform.SetParent(autoSnapContainersRoot.transform);
                    classContainer.transform.position = markerTransform.position;

                    classContainers[className] = classContainer.transform;
                }
            }
        }
    }

    private void InitializeGroupContainers()
    {
        groupContainersByType.Clear();

        foreach (var className in classContainers.Keys)
        {
            groupContainersByType[className] = new List<GameObject>();

            Transform classContainer = classContainers[className];
            foreach (Transform child in classContainer)
            {
                if (child.name.StartsWith($"{className}_Subgroups_"))
                {
                    groupContainersByType[className].Add(child.gameObject);
                }
            }

            groupContainersByType[className].Sort((a, b) => {
                int aNum = ExtractGroupNumber(a.name);
                int bNum = ExtractGroupNumber(b.name);
                return aNum.CompareTo(bNum);
            });
        }
    }

    private int ExtractGroupNumber(string groupName)
    {
        string[] parts = groupName.Split('_');
        if (parts.Length >= 3 && int.TryParse(parts[2], out int number))
        {
            return number;
        }
        return 0;
    }

    private IEnumerator ArrangeComponentsVertically()
    {
        isProcessing = true;
        Debug.Log("Starting compact vertical arrangement with sequential animation...");

        groupCounter++;

        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();

        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>();

        HashSet<CircuitComponent> alreadyGroupedComponents = FindAlreadyGroupedComponents();

        List<CircuitComponent> activeComponents = allComponents
            .Where(comp => comp != null && comp.gameObject.activeInHierarchy && !alreadyGroupedComponents.Contains(comp))
            .GroupBy(comp => $"{comp.componentType}{comp.componentNumber}")
            .Select(group => group.First())
            .ToList();

        Debug.Log($"Found {activeComponents.Count} unique active components for group {groupCounter}");

        if (activeComponents.Count == 0)
        {
            Debug.Log("No new components to arrange");
            isProcessing = false;
            yield break;
        }

        var componentsByType = activeComponents
            .GroupBy(comp => comp.componentType)
            .ToDictionary(group => group.Key, group => group.ToList());

        List<Coroutine> animationCoroutines = new List<Coroutine>();

        foreach (var typeGroup in componentsByType)
        {
            string componentType = typeGroup.Key;
            List<CircuitComponent> components = typeGroup.Value;

            if (!classContainers.ContainsKey(componentType))
            {
                Debug.LogWarning($"No class container found for component type: {componentType}");
                continue;
            }

            animationCoroutines.Add(StartCoroutine(ArrangeComponentsForType(componentType, components, groupCounter)));
        }

        foreach (var coroutine in animationCoroutines)
        {
            yield return coroutine;
        }

        InitializeGroupContainers();

        // Принудительно проверяем контейнеры после завершения анимации
        CheckAllContainersForSubgroups();

        LogGroupInformation();
        Debug.Log($"Compact vertical arrangement with sequential animation completed for group {groupCounter}");
        isProcessing = false;
    }

    private IEnumerator ArrangeComponentsForType(string componentType, List<CircuitComponent> components, int currentGroupNumber)
    {
        Debug.Log($"Arranging {components.Count} components of type {componentType} for group {currentGroupNumber} with sequential animation");

        Transform classContainer = classContainers[componentType];
        Transform classMarker = classMarkers[componentType];

        Transform childMarker = classMarker.Find("ChildMarker");
        if (childMarker == null)
        {
            Debug.LogWarning($"ChildMarker not found for class {componentType}");
            yield break;
        }

        components.Sort((a, b) => a.componentNumber.CompareTo(b.componentNumber));

        List<List<CircuitComponent>> subGroups = SplitIntoSubGroups(components, maxComponentsPerSubGroup);

        float groupXPosition = CalculateGroupXPosition(componentType, currentGroupNumber);

        GameObject subgroupsContainer = new GameObject($"{componentType}_Subgroups_{currentGroupNumber}");
        subgroupsContainer.transform.SetParent(classContainer);
        subgroupsContainer.transform.localPosition = new Vector3(groupXPosition, 0, 0);

        if (!groupContainersByType.ContainsKey(componentType))
        {
            groupContainersByType[componentType] = new List<GameObject>();
        }
        groupContainersByType[componentType].Add(subgroupsContainer);

        // НЕМЕДЛЕННО активируем камеру при создании контейнера подгрупп
        ActivateClassCamera(componentType);

        // Запускаем анимацию для каждой подгруппы последовательно
        for (int i = 0; i < subGroups.Count; i++)
        {
            var subGroup = subGroups[i];

            GameObject subGroupContainer = new GameObject($"Sub-group_{i + 1}");
            subGroupContainer.transform.SetParent(subgroupsContainer.transform);

            float subGroupX = i * subGroupSpacing;
            subGroupContainer.transform.localPosition = new Vector3(subGroupX, 0, 0);

            // Запускаем последовательную анимацию для компонентов в подгруппе
            yield return StartCoroutine(ArrangeSubGroupSequentially(subGroup, subGroupContainer));
        }

        Debug.Log($"Completed arranging components for type {componentType}");
    }

    // Остальные методы остаются без изменений...

    private IEnumerator ArrangeSubGroupSequentially(List<CircuitComponent> subGroup, GameObject subGroupContainer)
    {
        for (int j = 0; j < subGroup.Count; j++)
        {
            CircuitComponent component = subGroup[j];
            if (component == null) continue;

            float componentHeight = CalculateComponentBounds(component).size.y;
            float localY = -j * (componentHeight + verticalSpacing);

            component.transform.SetParent(subGroupContainer.transform);

            // Запускаем анимацию для одного компонента и ждем ее завершения
            Coroutine animationCoroutine = StartCoroutine(AnimateComponentToPosition(component, new Vector3(0, localY, 0)));
            activeAnimationCoroutines.Add(animationCoroutine);

            // Ждем завершения анимации текущего компонента
            yield return animationCoroutine;

            // Добавляем задержку перед началом анимации следующего компонента
            if (j < subGroup.Count - 1)
            {
                yield return new WaitForSeconds(delayBetweenComponents);
            }
        }
    }

    private IEnumerator AnimateComponentToPosition(CircuitComponent component, Vector3 targetLocalPosition)
    {
        if (component == null) yield break;

        Transform componentRoot = component.transform;
        Vector3 startLocalPosition = componentRoot.localPosition;
        float elapsed = 0f;

        Rigidbody2D rb = component.GetComponentInChildren<Rigidbody2D>();
        bool wasKinematic = false;

        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
        }

        if (debugGrouping)
        {
            Debug.Log($"Starting animation for {component.componentType}{component.componentNumber} from {startLocalPosition} to {targetLocalPosition}");
        }

        while (elapsed < componentMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / componentMoveDuration);

            float curveValue = moveCurve.Evaluate(t);

            componentRoot.localPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, curveValue);
            yield return null;
        }

        componentRoot.localPosition = targetLocalPosition;
        ResetLocalPositionsUntilDraggableCircle(componentRoot);

        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
        }

        Physics2D.SyncTransforms();

        if (debugGrouping)
        {
            Debug.Log($"Component {component.componentType}{component.componentNumber} animation completed");
        }
    }

    private float CalculateGroupXPosition(string componentType, int currentGroupNumber)
    {
        if (!groupContainersByType.ContainsKey(componentType) || groupContainersByType[componentType].Count == 0)
        {
            return 0f;
        }

        GameObject lastGroup = groupContainersByType[componentType][groupContainersByType[componentType].Count - 1];

        float lastGroupX = lastGroup.transform.localPosition.x;

        int lastGroupSubgroupCount = lastGroup.transform.childCount;
        float lastGroupWidth = (lastGroupSubgroupCount - 1) * subGroupSpacing;

        float newGroupX = lastGroupX + lastGroupWidth + groupSpacing;

        Debug.Log($"Calculating group position for {componentType}_Subgroups_{currentGroupNumber}: " +
                 $"LastGroupX={lastGroupX}, LastGroupWidth={lastGroupWidth}, NewGroupX={newGroupX}");

        return newGroupX;
    }

    private void StopAllActiveAnimations()
    {
        foreach (var coroutine in activeAnimationCoroutines)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        activeAnimationCoroutines.Clear();
    }

    private void ResetLocalPositionsUntilDraggableCircle(Transform parent)
    {
        foreach (Transform child in parent)
        {
            child.localPosition = Vector3.zero;

            DraggableComponent draggable = child.GetComponent<DraggableComponent>();
            if (draggable != null)
            {
                continue;
            }
            else
            {
                ResetImmediateChildrenOnly(child);
            }
        }
    }

    private void ResetImmediateChildrenOnly(Transform parent)
    {
        foreach (Transform child in parent)
        {
            child.localPosition = Vector3.zero;
        }
    }

    private Bounds CalculateComponentBounds(CircuitComponent component)
    {
        if (component == null)
            return new Bounds(Vector3.zero, Vector3.zero);

        DraggableComponent draggable = component.GetComponentInChildren<DraggableComponent>();
        if (draggable != null)
        {
            Renderer renderer = draggable.GetComponent<Renderer>();
            if (renderer != null) return renderer.bounds;

            Collider2D collider = draggable.GetComponent<Collider2D>();
            if (collider != null) return collider.bounds;
        }

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

    private HashSet<CircuitComponent> FindAlreadyGroupedComponents()
    {
        HashSet<CircuitComponent> groupedComponents = new HashSet<CircuitComponent>();

        if (autoSnapContainersRoot == null) return groupedComponents;

        foreach (Transform containerTransform in autoSnapContainersRoot.transform)
        {
            CircuitComponent[] componentsInContainer = containerTransform.GetComponentsInChildren<CircuitComponent>();
            foreach (CircuitComponent comp in componentsInContainer)
            {
                groupedComponents.Add(comp);
            }
        }

        return groupedComponents;
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

    private void LogGroupInformation()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("=== COMPONENT GROUP INFORMATION ===");

        foreach (var typeGroup in groupContainersByType)
        {
            string componentType = typeGroup.Key;
            List<GameObject> groups = typeGroup.Value;

            log.AppendLine($"Component Type: {componentType}");
            log.AppendLine($"Number of Groups: {groups.Count}");

            foreach (GameObject group in groups)
            {
                log.AppendLine($"  Group: {group.name} at X={group.transform.localPosition.x}");

                foreach (Transform subGroup in group.transform)
                {
                    log.AppendLine($"    SubGroup: {subGroup.name} at X={subGroup.transform.localPosition.x}");
                    log.AppendLine($"      Components: {subGroup.childCount}");
                }
            }
            log.AppendLine();
        }

        Debug.Log(log.ToString());
    }

    public void ManualTriggerSnap()
    {
        if (!isProcessing)
        {
            StopAllActiveAnimations();
            StartCoroutine(ArrangeComponentsVertically());
        }
    }

    void OnDrawGizmos()
    {
        if (!debugGrouping) return;

        foreach (var marker in classMarkers)
        {
            if (marker.Value != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(marker.Value.position, 1f);

                Transform childMarker = marker.Value.Find("ChildMarker");
                if (childMarker != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(childMarker.position, 0.5f);
                    Gizmos.DrawLine(marker.Value.position, childMarker.position);
                }
            }
        }
    }

    void OnDestroy()
    {
        if (containerMonitorCoroutine != null)
        {
            StopCoroutine(containerMonitorCoroutine);
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