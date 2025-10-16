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
    public float componentMoveDuration = 0.5f;
    public float delayBetweenComponents = 0.2f;
    public float delayBetweenClasses = 2.0f; // ЗАДЕРЖКА ДЛЯ ПРОСМОТРА УВЕЛИЧЕННОГО ОКНА
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("References")]
    public CameraManager cameraManager;
    public ComponentManager componentManager;
    public DisplayAnimator displayAnimator;

    [Header("Debug")]
    public bool debugMode = true;

    // СОБЫТИЯ для синхронизации
    public System.Action<string> OnReadyForNextClass;
    public System.Action<string> OnClassArrangementCompleted;

    private bool isProcessing = false;
    private Dictionary<string, Transform> classMarkers = new Dictionary<string, Transform>();
    private Dictionary<string, Transform> classContainers = new Dictionary<string, Transform>();
    private Dictionary<string, List<GameObject>> groupContainersByType = new Dictionary<string, List<GameObject>>();
    private GameObject markersContainer;
    private GameObject autoSnapContainersRoot;

    private int groupCounter = 0;
    private List<Coroutine> activeAnimationCoroutines = new List<Coroutine>();
    private Dictionary<string, int> componentCountByClass = new Dictionary<string, int>();
    private Dictionary<string, int> subgroupCountByClass = new Dictionary<string, int>();
    private string currentProcessingClass = "";

    // Синхронизация
    private List<string> pendingClasses = new List<string>();
    private Dictionary<string, bool> displayExpandedStatus = new Dictionary<string, bool>();
    private Dictionary<string, bool> arrangementCompleteStatus = new Dictionary<string, bool>();
    private Coroutine synchronizedArrangementCoroutine;

    void Start()
    {
        InitializeReferences();
        InitializeClassMarkers();
        CreateClassContainers();
        InitializeGroupContainers();

        // Подписываемся на события DisplayAnimator
        if (displayAnimator != null)
        {
            displayAnimator.OnDisplayExpanded += OnDisplayExpanded;
            displayAnimator.OnDisplayCollapsed += OnDisplayCollapsed;
        }
    }

    void OnDestroy()
    {
        if (displayAnimator != null)
        {
            displayAnimator.OnDisplayExpanded -= OnDisplayExpanded;
            displayAnimator.OnDisplayCollapsed -= OnDisplayCollapsed;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(hotkey) && !isProcessing)
        {
            StopAllActiveAnimations();
        }
    }

    private void InitializeReferences()
    {
        if (cameraManager == null)
            cameraManager = FindObjectOfType<CameraManager>();
        if (componentManager == null)
            componentManager = FindObjectOfType<ComponentManager>();
        if (displayAnimator == null)
            displayAnimator = FindObjectOfType<DisplayAnimator>();
    }

    /// <summary>
    /// Запуск синхронизированной расстановки
    /// </summary>
    public IEnumerator StartSynchronizedArrangement(List<string> activeClasses)
    {
        if (isProcessing) yield break;

        isProcessing = true;

        if (debugMode)
            Debug.Log("=== STARTING SYNCHRONIZED ARRANGEMENT ===");

        // Инициализация состояний синхронизации
        pendingClasses.Clear();
        displayExpandedStatus.Clear();
        arrangementCompleteStatus.Clear();

        foreach (string className in activeClasses)
        {
            pendingClasses.Add(className);
            displayExpandedStatus[className] = false;
            arrangementCompleteStatus[className] = false;
        }

        groupCounter++;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();

        // Запускаем обработку первого класса
        if (pendingClasses.Count > 0)
        {
            string firstClass = pendingClasses[0];
            OnReadyForNextClass?.Invoke(firstClass);

            if (debugMode)
                Debug.Log($"Started synchronized arrangement for first class: {firstClass}");
        }

        // Ждем завершения всех классов
        yield return new WaitUntil(() => pendingClasses.Count == 0);

        InitializeGroupContainers();
        LogGroupInformation();

        if (debugMode)
            Debug.Log("=== SYNCHRONIZED ARRANGEMENT COMPLETED ===");

        isProcessing = false;
    }

    // Обработчик события расширения дисплея
    private void OnDisplayExpanded(string className)
    {
        if (debugMode)
            Debug.Log($"Display expanded for class: {className}, starting component arrangement...");

        displayExpandedStatus[className] = true;

        // Запускаем расстановку компонентов
        StartCoroutine(ArrangeComponentsForClassSynchronized(className));
    }

    // Обработчик события схлопывания дисплея
    private void OnDisplayCollapsed(string className)
    {
        if (debugMode)
            Debug.Log($"Display collapsed for class: {className}");

        // Помечаем класс как завершенный
        if (pendingClasses.Contains(className))
        {
            pendingClasses.Remove(className);
        }

        // Запускаем следующий класс если есть
        if (pendingClasses.Count > 0)
        {
            string nextClass = pendingClasses[0];
            OnReadyForNextClass?.Invoke(nextClass);

            if (debugMode)
                Debug.Log($"Moving to next class: {nextClass}");
        }
    }

    /// <summary>
    /// Синхронизированная расстановка компонентов для класса
    /// </summary>
    private IEnumerator ArrangeComponentsForClassSynchronized(string className)
    {
        if (debugMode)
            Debug.Log($"Starting synchronized arrangement for class: {className}");

        currentProcessingClass = className;

        // Находим компоненты указанного класса
        var activeComponents = FindComponentsForClass(className);

        if (debugMode)
            Debug.Log($"Found {activeComponents.Count} components for class {className}");

        if (activeComponents.Count > 0)
        {
            yield return StartCoroutine(ArrangeComponentsForType(className, activeComponents, groupCounter));
        }

        // Помечаем расстановку как завершенную
        arrangementCompleteStatus[className] = true;
        OnClassArrangementCompleted?.Invoke(className);

        if (debugMode)
            Debug.Log($"Synchronized arrangement completed for class: {className}");

        currentProcessingClass = "";
    }

    /// <summary>
    /// Уведомление о завершении анимации дисплея
    /// </summary>
    public void NotifyDisplayAnimationComplete(string className)
    {
        if (debugMode)
            Debug.Log($"Display animation complete for: {className}");

        // Этот метод теперь вызывается через события
    }

    /// <summary>
    /// Проверка завершения расстановки для класса
    /// </summary>
    public bool IsClassArrangementComplete(string className)
    {
        return arrangementCompleteStatus.ContainsKey(className) && arrangementCompleteStatus[className];
    }

    private List<CircuitComponent> FindComponentsForClass(string className)
    {
        CircuitComponent[] allComponents = FindObjectsOfType<CircuitComponent>();
        HashSet<CircuitComponent> alreadyGroupedComponents = FindAlreadyGroupedComponents();

        return allComponents
            .Where(comp => comp != null &&
                   comp.gameObject.activeInHierarchy &&
                   !alreadyGroupedComponents.Contains(comp) &&
                   comp.componentType == className)
            .GroupBy(comp => $"{comp.componentType}{comp.componentNumber}")
            .Select(group => group.First())
            .ToList();
    }

    private IEnumerator ArrangeComponentsForType(string componentType, List<CircuitComponent> components, int currentGroupNumber)
    {
        if (debugMode)
            Debug.Log($"Arranging {components.Count} components of type {componentType}");

        if (!classContainers.ContainsKey(componentType))
        {
            Debug.LogWarning($"Class container not found for {componentType}");
            yield break;
        }

        Transform classContainer = classContainers[componentType];

        components.Sort((a, b) => a.componentNumber.CompareTo(b.componentNumber));
        List<List<CircuitComponent>> subGroups = SplitIntoSubGroups(components, maxComponentsPerSubGroup);

        float groupXPosition = CalculateGroupXPosition(componentType, currentGroupNumber);

        GameObject subgroupsContainer = new GameObject($"{componentType}_Subgroups_{currentGroupNumber}");
        subgroupsContainer.transform.SetParent(classContainer);
        subgroupsContainer.transform.localPosition = new Vector3(groupXPosition, 0, 0);

        if (!groupContainersByType.ContainsKey(componentType))
            groupContainersByType[componentType] = new List<GameObject>();

        groupContainersByType[componentType].Add(subgroupsContainer);

        // Обновляем счетчики
        componentCountByClass[componentType] = components.Count;
        subgroupCountByClass[componentType] = subGroups.Count;

        // Анимация подгрупп
        for (int i = 0; i < subGroups.Count; i++)
        {
            var subGroup = subGroups[i];
            GameObject subGroupContainer = new GameObject($"Sub-group_{i + 1}");
            subGroupContainer.transform.SetParent(subgroupsContainer.transform);
            subGroupContainer.transform.localPosition = new Vector3(i * subGroupSpacing, 0, 0);

            yield return StartCoroutine(ArrangeSubGroupSequentially(subGroup, subGroupContainer));
        }

        // ВЫЗЫВАЕМ СОБЫТИЕ ПЕРЕМЕЩЕНИЯ КОМПОНЕНТОВ
        if (componentManager != null)
        {
            componentManager.MoveComponentsToAutoSnap(componentType);

            if (debugMode)
                Debug.Log($"Notified ComponentManager about components move: {componentType}");
        }

        // ГАРАНТИРУЕМ ЗАВЕРШЕНИЕ ВСЕХ АНИМАЦИЙ
        yield return new WaitForSeconds(componentMoveDuration + 0.1f);

        if (debugMode)
            Debug.Log($"Completed arranging components for type {componentType}");
    }

    private IEnumerator ArrangeSubGroupSequentially(List<CircuitComponent> subGroup, GameObject subGroupContainer)
    {
        for (int j = 0; j < subGroup.Count; j++)
        {
            CircuitComponent component = subGroup[j];
            if (component == null) continue;

            float componentHeight = CalculateComponentBounds(component).size.y;
            float localY = -j * (componentHeight + verticalSpacing);

            component.transform.SetParent(subGroupContainer.transform);

            Coroutine animationCoroutine = StartCoroutine(AnimateComponentToPosition(component, new Vector3(0, localY, 0)));
            activeAnimationCoroutines.Add(animationCoroutine);
            yield return animationCoroutine;

            if (j < subGroup.Count - 1)
                yield return new WaitForSeconds(delayBetweenComponents);
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

        while (elapsed < componentMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = moveCurve.Evaluate(elapsed / componentMoveDuration);
            componentRoot.localPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, t);
            yield return null;
        }

        componentRoot.localPosition = targetLocalPosition;
        ResetLocalPositionsUntilDraggableCircle(componentRoot);

        if (rb != null)
            rb.isKinematic = wasKinematic;

        Physics2D.SyncTransforms();
    }

    // Вспомогательные методы
    private void InitializeClassMarkers()
    {
        classMarkers.Clear();
        markersContainer = GameObject.Find("ClassMarkers");
        if (markersContainer == null) return;

        foreach (Transform marker in markersContainer.transform)
        {
            if (marker.name.StartsWith("ClassMarker_"))
            {
                string className = marker.name.Replace("ClassMarker_", "");
                classMarkers[className] = marker;
            }
        }
    }

    private void CreateClassContainers()
    {
        classContainers.Clear();
        autoSnapContainersRoot = GameObject.Find("AutoSnapContainers");
        if (autoSnapContainersRoot == null)
            autoSnapContainersRoot = new GameObject("AutoSnapContainers");

        string[] knownClasses = { "R", "C", "D", "L", "U", "G", "Q", "J", "K", "S", "Z", "O", "X", "A", "P", "M" };

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
                    groupContainersByType[className].Add(child.gameObject);
            }
        }
    }

    private float CalculateGroupXPosition(string componentType, int currentGroupNumber)
    {
        if (!groupContainersByType.ContainsKey(componentType) || groupContainersByType[componentType].Count == 0)
            return 0f;

        GameObject lastGroup = groupContainersByType[componentType].Last();
        float lastGroupX = lastGroup.transform.localPosition.x;
        int lastGroupSubgroupCount = lastGroup.transform.childCount;
        float lastGroupWidth = (lastGroupSubgroupCount - 1) * subGroupSpacing;

        return lastGroupX + lastGroupWidth + groupSpacing;
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
                groupedComponents.Add(comp);
        }
        return groupedComponents;
    }

    private List<List<CircuitComponent>> SplitIntoSubGroups(List<CircuitComponent> group, int maxSize)
    {
        List<List<CircuitComponent>> subGroups = new List<List<CircuitComponent>>();
        group.Sort((a, b) => a.componentNumber.CompareTo(b.componentNumber));

        for (int i = 0; i < group.Count; i += maxSize)
            subGroups.Add(group.GetRange(i, Mathf.Min(maxSize, group.Count - i)));

        return subGroups;
    }

    private void ResetLocalPositionsUntilDraggableCircle(Transform parent)
    {
        foreach (Transform child in parent)
        {
            child.localPosition = Vector3.zero;
            DraggableComponent draggable = child.GetComponent<DraggableComponent>();
            if (draggable == null)
                ResetImmediateChildrenOnly(child);
        }
    }

    private void ResetImmediateChildrenOnly(Transform parent)
    {
        foreach (Transform child in parent)
            child.localPosition = Vector3.zero;
    }

    private void StopAllActiveAnimations()
    {
        foreach (var coroutine in activeAnimationCoroutines)
            if (coroutine != null) StopCoroutine(coroutine);
        activeAnimationCoroutines.Clear();
    }

    private void LogGroupInformation()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("=== COMPONENT GROUP INFORMATION ===");

        foreach (var typeGroup in groupContainersByType)
        {
            string componentType = typeGroup.Key;
            int componentCount = componentCountByClass.ContainsKey(componentType) ? componentCountByClass[componentType] : 0;
            int subgroupCount = subgroupCountByClass.ContainsKey(componentType) ? subgroupCountByClass[componentType] : 0;

            log.AppendLine($"Component Type: {componentType}");
            log.AppendLine($"Number of Groups: {typeGroup.Value.Count}");
            log.AppendLine($"Total Components: {componentCount}");
            log.AppendLine($"Total Subgroups: {subgroupCount}");

            foreach (GameObject group in typeGroup.Value)
            {
                log.AppendLine($"  Group: {group.name} at X={group.transform.localPosition.x}");
                foreach (Transform subGroup in group.transform)
                    log.AppendLine($"    SubGroup: {subGroup.name} at X={subGroup.transform.localPosition.x}, Components: {subGroup.childCount}");
            }
            log.AppendLine();
        }
        Debug.Log(log.ToString());
    }

    public void ManualTriggerSnap()
    {
        if (!isProcessing)
        {
            // Запускаем через DisplayAnimator для синхронизации
            if (displayAnimator != null)
            {
                displayAnimator.StartFullAnimationSequence();
            }
        }
    }

    public bool HasComponentsForClass(string className)
    {
        return groupContainersByType.ContainsKey(className) &&
               groupContainersByType[className].Count > 0;
    }

    public int GetComponentCountForClass(string className)
    {
        return componentCountByClass.ContainsKey(className) ? componentCountByClass[className] : 0;
    }

    public string GetCurrentProcessingClass() => currentProcessingClass;
    public bool IsProcessingClass() => !string.IsNullOrEmpty(currentProcessingClass);
}