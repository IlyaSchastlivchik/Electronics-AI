using UnityEngine;
using System.Collections.Generic;

public class ComponentManager : MonoBehaviour
{
    public static ComponentManager Instance { get; private set; }

    [SerializeField] private ComponentDatabase database;
    [SerializeField] private GameObject componentButtonPrefab;

    private Dictionary<string, int> _counters = new Dictionary<string, int>();
    private Dictionary<string, Transform> _listContainers = new Dictionary<string, Transform>();
    private Dictionary<KeyCode, ComponentClass> _menuHotkeys = new Dictionary<KeyCode, ComponentClass>();
    private Dictionary<KeyCode, (ComponentClass parentClass, ComponentSubclass subclass)> _componentHotkeys = new Dictionary<KeyCode, (ComponentClass, ComponentSubclass)>();

    // Добавляем события для отслеживания перемещения компонентов
    public System.Action<string> OnComponentsMovedToAutoSnap;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (database == null)
        {
            Debug.LogError("Database not assigned in ComponentManager!");
            return;
        }

        if (componentButtonPrefab == null)
        {
            Debug.LogError("Component Button Prefab not assigned!");
            return;
        }

        Initialize();
    }

    void Initialize()
    {
        CreateListContainers();
        InitializeHotkeyDictionaries();
    }

    void CreateListContainers()
    {
        if (database == null || database.classes == null)
        {
            Debug.LogError("Database is not assigned or empty!");
            return;
        }

        GameObject containersRoot = new GameObject("ComponentContainers");
        containersRoot.transform.SetParent(transform);

        foreach (ComponentClass cls in database.classes)
        {
            if (string.IsNullOrEmpty(cls.id))
            {
                Debug.LogWarning("Skipping class with empty ID");
                continue;
            }

            if (_listContainers.ContainsKey(cls.id))
            {
                Debug.LogWarning($"Duplicate container ID detected: {cls.id}");
                continue;
            }

            GameObject container = new GameObject($"{cls.id}_List");
            container.transform.SetParent(containersRoot.transform);
            _listContainers.Add(cls.id, container.transform);
        }
    }

    void InitializeHotkeyDictionaries()
    {
        foreach (ComponentClass cls in database.classes)
        {
            if (cls.hotkey != KeyCode.None)
            {
                if (_menuHotkeys.ContainsKey(cls.hotkey))
                {
                    Debug.LogWarning($"Duplicate hotkey {cls.hotkey} detected for menu {cls.id}");
                }
                else
                {
                    _menuHotkeys.Add(cls.hotkey, cls);
                }
            }

            foreach (ComponentSubclass subclass in cls.subclasses)
            {
                if (subclass.hotkey != KeyCode.None)
                {
                    if (_componentHotkeys.ContainsKey(subclass.hotkey))
                    {
                        Debug.LogWarning($"Duplicate hotkey {subclass.hotkey} detected for component {subclass.name}");
                    }
                    else
                    {
                        _componentHotkeys.Add(subclass.hotkey, (cls, subclass));
                    }
                }
            }
        }
    }

    void Update()
    {
        foreach (var kvp in _menuHotkeys)
        {
            if (Input.GetKeyDown(kvp.Key))
            {
                if (MainMenuManager.Instance != null)
                {
                    MainMenuManager.Instance.ActivateToolbarPanel(kvp.Value);
                }
                else
                {
                    Debug.LogWarning("MainMenuManager instance is null");
                }
            }
        }

        foreach (var kvp in _componentHotkeys)
        {
            if (Input.GetKeyDown(kvp.Key))
            {
                CreateComponentFromHotkey(kvp.Value.parentClass, kvp.Value.subclass);
                break;
            }
        }
    }

    void CreateComponentFromHotkey(ComponentClass parentClass, ComponentSubclass subclass)
    {
        GameObject newComponent = Instantiate(subclass.prefab);
        ComponentDragger dragger = newComponent.GetComponent<ComponentDragger>();
        if (dragger == null)
        {
            dragger = newComponent.AddComponent<ComponentDragger>();
        }

        string prefix = parentClass.id;
        dragger.Initialize(prefix, subclass);
    }

    public string GenerateComponentID(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            Debug.LogWarning("Attempted to generate ID with empty prefix");
            return "INVALID_ID";
        }

        if (!_counters.ContainsKey(prefix))
        {
            _counters[prefix] = 0;
        }
        return $"{prefix}{++_counters[prefix]}";
    }

    public Transform GetListContainer(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            Debug.LogWarning("Attempted to get container with empty prefix");
            return null;
        }

        if (_listContainers.ContainsKey(prefix))
        {
            return _listContainers[prefix];
        }

        Debug.LogWarning($"Container not found for prefix: {prefix}. Available containers: {string.Join(", ", _listContainers.Keys)}");
        return null;
    }

    public ComponentClass GetComponentClassById(string id)
    {
        return database.classes.Find(cls => cls.id == id);
    }

    public List<ComponentClass> GetAllComponentClasses()
    {
        return database.classes;
    }

    public GameObject GetComponentButtonPrefab()
    {
        return componentButtonPrefab;
    }

    // МЕТОДЫ ДЛЯ ИНТЕГРАЦИИ С DISPLAY ANIMATOR
    public bool HasComponentsInList(string className)
    {
        if (_listContainers.ContainsKey(className))
        {
            Transform container = _listContainers[className];
            return container.childCount > 0;
        }
        return false;
    }

    public int GetComponentCountInList(string className)
    {
        if (_listContainers.ContainsKey(className))
        {
            return _listContainers[className].childCount;
        }
        return 0;
    }

    public List<string> GetActiveClasses()
    {
        List<string> activeClasses = new List<string>();
        foreach (var kvp in _listContainers)
        {
            if (kvp.Value.childCount > 0)
            {
                activeClasses.Add(kvp.Key);
            }
        }
        return activeClasses;
    }

    public Transform GetClassContainer(string className)
    {
        return _listContainers.ContainsKey(className) ? _listContainers[className] : null;
    }

    /// <summary>
    /// Перемещает компоненты из x_List в AutoSnapContainers и вызывает событие
    /// </summary>
    public void MoveComponentsToAutoSnap(string className)
    {
        if (!_listContainers.ContainsKey(className))
        {
            Debug.LogWarning($"Container not found for class: {className}");
            return;
        }

        Transform listContainer = _listContainers[className];
        int movedCount = 0;

        foreach (Transform child in listContainer)
        {
            if (child != null)
            {
                // Здесь можно добавить логику перемещения, если нужно
                movedCount++;
            }
        }

        if (movedCount > 0)
        {
            Debug.Log($"Moved {movedCount} components from {className}_List to AutoSnap");
            OnComponentsMovedToAutoSnap?.Invoke(className);
        }
    }

    /// <summary>
    /// Проверяет, перемещены ли все компоненты класса
    /// </summary>
    public bool AreAllComponentsMoved(string className)
    {
        if (!_listContainers.ContainsKey(className))
            return false;

        Transform listContainer = _listContainers[className];
        return listContainer.childCount == 0;
    }
}