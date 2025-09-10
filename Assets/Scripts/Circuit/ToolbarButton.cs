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
        ComponentDragger dragger = newComponent.AddComponent<ComponentDragger>();
        dragger.Initialize(_componentSubclass.name);

        if (_dropdownInstance != null)
        {
            Destroy(_dropdownInstance);
            _dropdownInstance = null;
        }

        // Закрываем все панели инструментов после создания компонента
        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.CloseAllToolbarPanels();
        }
    }
}