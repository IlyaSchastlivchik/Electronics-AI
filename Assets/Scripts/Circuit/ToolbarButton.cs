using UnityEngine;
using UnityEngine.UI;

public class ToolbarButton : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject dropdownPrefab;

    private ComponentSubclass _componentSubclass;
    private GameObject _dropdownInstance;

    void Awake()
    {
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }
    }

    public void Initialize(ComponentSubclass subclass)
    {
        _componentSubclass = subclass;
        iconImage.sprite = subclass.icon;
        GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        CreateComponent(_componentSubclass.prefab);
    }

    private void CreateComponent(GameObject prefab)
    {
        GameObject newComponent = Instantiate(prefab);
        ComponentDragger dragger = newComponent.GetComponent<ComponentDragger>();
        if (dragger == null)
        {
            dragger = newComponent.AddComponent<ComponentDragger>();
        }

        // Get the parent class ID from the ToolbarPanel
        ToolbarPanel parentPanel = GetComponentInParent<ToolbarPanel>();
        string classId = "";

        if (parentPanel != null)
        {
            // We need a way to get the class ID from the parent panel
            // You may need to add a public method in ToolbarPanel to expose this
            classId = GetClassIdFromParentPanel(parentPanel);
        }

        // If we couldn't get the class ID, use a fallback
        if (string.IsNullOrEmpty(classId))
        {
            classId = _componentSubclass.name.Replace(" ", "");
            Debug.LogWarning($"Using fallback class ID: {classId}");
        }

        // Initialize with both parameters
        dragger.Initialize(classId, _componentSubclass);

        if (_dropdownInstance != null)
        {
            Destroy(_dropdownInstance);
            _dropdownInstance = null;
        }

        // Close all toolbar panels after creating component
        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.CloseAllToolbarPanels();
        }
    }

    private string GetClassIdFromParentPanel(ToolbarPanel panel)
    {
        // This is a workaround since we don't have direct access to the class ID
        // You might need to modify ToolbarPanel to expose the class ID
        // For now, we'll use reflection as a last resort
        var field = panel.GetType().GetField("_componentClass",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            var componentClass = field.GetValue(panel) as ComponentClass;
            if (componentClass != null)
            {
                return componentClass.id;
            }
        }

        return null;
    }
}