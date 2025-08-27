using UnityEngine;
using System.Collections.Generic;

public class ComponentManager : MonoBehaviour
{
    public static ComponentManager Instance { get; private set; }

    [SerializeField] private ComponentDatabase database;
    [SerializeField] private GameObject componentButtonPrefab;
    [SerializeField] private Transform toolbarPanel;

    private Dictionary<string, int> _counters = new Dictionary<string, int>();
    private Dictionary<string, Transform> _listContainers = new Dictionary<string, Transform>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Проверяем необходимые ссылки перед инициализацией
        if (database == null)
        {
            Debug.LogError("Database not assigned in ComponentManager!");
            return;
        }

        if (toolbarPanel == null)
        {
            Debug.LogError("Toolbar Panel not assigned!");
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
        GenerateToolbarButtons();
    }

    void CreateListContainers()
    {
        if (database == null || database.classes == null)
        {
            Debug.LogError("Database is not assigned or empty!");
            return;
        }

        GameObject containersRoot = new GameObject("ComponentContainers");
        DontDestroyOnLoad(containersRoot);

        foreach (ComponentClass cls in database.classes)
        {
            if (string.IsNullOrEmpty(cls.id))
            {
                Debug.LogWarning("Skipping class with empty ID");
                continue;
            }

            // Проверяем, нет ли уже контейнера с таким ID
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

    void GenerateToolbarButtons()
    {
        if (database.classes == null)
        {
            Debug.LogError("Database classes list is null!");
            return;
        }

        foreach (ComponentClass cls in database.classes)
        {
            // Создаем кнопку только для классов с валидными данными
            if (string.IsNullOrEmpty(cls.id)) continue;

            GameObject button = Instantiate(componentButtonPrefab, toolbarPanel);
            button.name = $"{cls.id}Button";

            ToolbarButton buttonScript = button.GetComponent<ToolbarButton>();
            if (buttonScript != null)
            {
                buttonScript.Initialize(cls);
            }
            else
            {
                Debug.LogError($"ToolbarButton component missing on button prefab for {cls.id}");
            }
        }
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
}