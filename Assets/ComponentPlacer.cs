using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ComponentPlacer : MonoBehaviour
{
    [Header("Required References")]
    public Camera mainCamera;
    public Transform componentsRoot;
    public Transform[] listContainers;

    [Header("Component Prefabs")]
    public GameObject resistorPrefab;
    public GameObject capacitorPrefab;
    public GameObject transistorPrefab;
    public GameObject icPrefab;

    [Header("Placement Settings")]
    public float gridSize = 0.5f;
    public LayerMask placementLayerMask;
    public Color validPlacementColor = Color.white;
    public Color invalidPlacementColor = Color.red;

    [Header("Audio Feedback")]
    public AudioClip placementSound;
    public AudioClip errorSound;

    private GameObject currentDraggedComponent;
    private string selectedComponentType;
    private Dictionary<string, GameObject> prefabMap;
    private Dictionary<string, int> componentCounters = new Dictionary<string, int>();
    private AudioSource audioSource;
    private SpriteRenderer currentComponentRenderer;

    void Start()
    {
        InitializePrefabMap();
        InitializeAudioSource();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        HandleComponentDragging();
        HandleComponentPlacement();
        UpdateVisualFeedback();
    }

    private void InitializePrefabMap()
    {
        prefabMap = new Dictionary<string, GameObject>
        {
            { "Resistor", resistorPrefab },
            { "Capacitor", capacitorPrefab },
            { "Transistor", transistorPrefab },
            { "IC", icPrefab }
        };
    }

    private void InitializeAudioSource()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 0.5f;
    }

    public void SelectComponent(string componentType)
    {
        if (prefabMap.ContainsKey(componentType) && prefabMap[componentType] != null)
        {
            // Создаем новый компонент для перетаскивания
            currentDraggedComponent = Instantiate(prefabMap[componentType], componentsRoot);
            currentDraggedComponent.name = "DraggingComponent";
            selectedComponentType = componentType;

            // Настраиваем тег
            currentDraggedComponent.tag = componentType;

            // Получаем SpriteRenderer для визуальной обратной связи
            currentComponentRenderer = currentDraggedComponent.GetComponentInChildren<SpriteRenderer>();

            // Отключаем физику во время перетаскивания
            SetComponentPhysicsEnabled(currentDraggedComponent, false);

            // Включаем все коллайдеры пинов
            EnableAllPinColliders(currentDraggedComponent, false);
        }
    }

    private void HandleComponentDragging()
    {
        if (currentDraggedComponent != null)
        {
            // Получаем позицию мыши в мировых координатах
            Vector3 mousePosition = GetMouseWorldPosition();

            // Привязка к сетке
            Vector3 snappedPosition = SnapToGrid(mousePosition);

            // Обновляем позицию компонента
            currentDraggedComponent.transform.position = snappedPosition;
        }
    }

    private void HandleComponentPlacement()
    {
        if (currentDraggedComponent != null && Input.GetMouseButtonDown(0))
        {
            // Проверяем, можно ли разместить здесь компонент
            if (CanPlaceComponent(currentDraggedComponent.transform.position))
            {
                PlaceComponent();
                PlaySound(placementSound);
            }
            else
            {
                PlaySound(errorSound);
                Debug.Log("Cannot place component here! Position is occupied.");
            }
        }

        // Отмена перетаскивания правой кнопкой мыши или ESC
        if (currentDraggedComponent != null && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
        {
            CancelDragging();
        }
    }

    private void UpdateVisualFeedback()
    {
        if (currentDraggedComponent != null && currentComponentRenderer != null)
        {
            bool canPlace = CanPlaceComponent(currentDraggedComponent.transform.position);
            currentComponentRenderer.color = canPlace ? validPlacementColor : invalidPlacementColor;
        }
    }

    private void PlaceComponent()
    {
        // Генерируем ID для компонента
        GenerateComponentId();

        // Помещаем в соответствующую папку
        PlaceInCorrectFolder();

        // Включаем физику для взаимодействия
        SetComponentPhysicsEnabled(currentDraggedComponent, true);

        // Включаем коллайдеры пинов
        EnableAllPinColliders(currentDraggedComponent, true);

        // Сбрасываем цвет
        if (currentComponentRenderer != null)
        {
            currentComponentRenderer.color = validPlacementColor;
        }

        // Сбрасываем ссылки
        currentDraggedComponent = null;
        selectedComponentType = null;
        currentComponentRenderer = null;
    }

    private void GenerateComponentId()
    {
        CircuitComponent circuitComp = currentDraggedComponent.GetComponent<CircuitComponent>();
        if (circuitComp == null)
        {
            circuitComp = currentDraggedComponent.AddComponent<CircuitComponent>();
        }

        // Увеличиваем счетчик для этого типа
        if (!componentCounters.ContainsKey(selectedComponentType))
            componentCounters[selectedComponentType] = 0;

        componentCounters[selectedComponentType]++;

        // Создаем ID (R1, C2, etc)
        string componentId = $"{selectedComponentType[0]}{componentCounters[selectedComponentType]}";

        // Устанавливаем данные компонента
        circuitComp.SetComponentData(componentId, selectedComponentType, componentCounters[selectedComponentType]);
    }

    private void PlaceInCorrectFolder()
    {
        string folderName = selectedComponentType + "_List";
        bool folderFound = false;

        foreach (Transform folder in listContainers)
        {
            if (folder.name == folderName)
            {
                currentDraggedComponent.transform.SetParent(folder);
                folderFound = true;
                break;
            }
        }

        if (!folderFound)
        {
            Debug.LogWarning($"Folder {folderName} not found! Placing in root.");
        }
    }

    private bool CanPlaceComponent(Vector3 position)
    {
        // Проверяем коллизии с другими компонентами
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, 0.3f, placementLayerMask);

        foreach (Collider2D collider in colliders)
        {
            // Игнорируем пины, провода и самого себя
            if (collider.CompareTag("Pin") || collider.CompareTag("Wire") ||
                collider.transform.IsChildOf(currentDraggedComponent.transform))
                continue;

            // Если есть другой компонент - нельзя разместить
            if (collider.CompareTag("Resistor") || collider.CompareTag("Capacitor") ||
                collider.CompareTag("Transistor") || collider.CompareTag("IC"))
                return false;
        }

        return true;
    }

    public void CancelDragging()
    {
        if (currentDraggedComponent != null)
        {
            Destroy(currentDraggedComponent);
            currentDraggedComponent = null;
            selectedComponentType = null;
            currentComponentRenderer = null;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -mainCamera.transform.position.z;
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x / gridSize) * gridSize,
            Mathf.Round(position.y / gridSize) * gridSize,
            0
        );
    }

    private void SetComponentPhysicsEnabled(GameObject component, bool enabled)
    {
        Collider2D collider = component.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = enabled;
        }
    }

    private void EnableAllPinColliders(GameObject component, bool enabled)
    {
        CircuitPin[] pins = component.GetComponentsInChildren<CircuitPin>();
        foreach (CircuitPin pin in pins)
        {
            Collider2D pinCollider = pin.GetComponent<Collider2D>();
            if (pinCollider != null)
            {
                pinCollider.enabled = enabled;
            }
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Публичные методы для вызова из UI
    public void OnResistorButtonClick() => SelectComponent("Resistor");
    public void OnCapacitorButtonClick() => SelectComponent("Capacitor");
    public void OnTransistorButtonClick() => SelectComponent("Transistor");
    public void OnICButtonClick() => SelectComponent("IC");

    // Метод для сброса счетчиков (например, при загрузке новой схемы)
    public void ResetCounters()
    {
        componentCounters.Clear();
    }

    // Метод для получения статистики
    public Dictionary<string, int> GetComponentStatistics()
    {
        return new Dictionary<string, int>(componentCounters);
    }
}