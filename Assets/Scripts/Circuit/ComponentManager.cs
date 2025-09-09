using UnityEngine;
using System.Collections.Generic;

public class ComponentManager : MonoBehaviour
{
    public static ComponentManager Instance { get; private set; }

    [SerializeField] private ComponentDatabase database;

    // ������ ������ �� toolbarPanel, ��� ��� ������ ������ ��������� �����������
    // [SerializeField] private Transform toolbarPanel;

    [SerializeField] private GameObject componentButtonPrefab;

    private Dictionary<string, int> _counters = new Dictionary<string, int>();
    private Dictionary<string, Transform> _listContainers = new Dictionary<string, Transform>();
    private Dictionary<KeyCode, ComponentClass> _menuHotkeys = new Dictionary<KeyCode, ComponentClass>();
    private Dictionary<KeyCode, ComponentSubclass> _componentHotkeys = new Dictionary<KeyCode, ComponentSubclass>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // �������� ���������� �����������
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

        // ������ �� ���������� ������ ����� - ��� ����� ������ MainMenuManager
        // GenerateToolbarButtons();
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

            // ���������, ��� ��� ���������� ID
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
        // �������������� ������� ������� ������
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

            // ��������� ������� ������� ��� �����������
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
                        _componentHotkeys.Add(subclass.hotkey, subclass);
                    }
                }
            }
        }
    }

    void Update()
    {
        // ��������� ������� ������ ��� ����
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

        // ��������� ������� ������ ��� �����������
        foreach (var kvp in _componentHotkeys)
        {
            if (Input.GetKeyDown(kvp.Key))
            {
                CreateComponentFromHotkey(kvp.Value);
                break;
            }
        }
    }

    void CreateComponentFromHotkey(ComponentSubclass subclass)
    {
        GameObject newComponent = Instantiate(subclass.prefab);
        ComponentDragger dragger = newComponent.AddComponent<ComponentDragger>();

        // ���������� ��� ��������� ��� �������
        string prefix = subclass.name.Replace(" ", "");
        dragger.Initialize(prefix);
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

        Debug.LogWarning($"Container not found for prefix: {prefix}");
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

    // ����� ��� ��������� ������� ������ ����������
    public GameObject GetComponentButtonPrefab()
    {
        return componentButtonPrefab;
    }
}