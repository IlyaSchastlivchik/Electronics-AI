using UnityEngine;
using UnityEngine.UI; // Добавьте эту строку

public class ToolbarButton : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject dropdownPrefab;

    private ComponentSubclass _componentSubclass;
    private GameObject _dropdownInstance;

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

    public void CreateComponent(GameObject prefab)
    {
        GameObject newComponent = Instantiate(prefab);
        ComponentDragger dragger = newComponent.AddComponent<ComponentDragger>();
        dragger.Initialize(_componentSubclass.name); // Используем имя подкласса как префикс

        if (_dropdownInstance != null)
        {
            Destroy(_dropdownInstance);
            _dropdownInstance = null;
        }
    }
}