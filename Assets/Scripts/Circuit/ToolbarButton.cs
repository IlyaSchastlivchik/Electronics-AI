using UnityEngine;
using UnityEngine.UI;

public class ToolbarButton : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject dropdownPrefab;

    private ComponentClass _componentClass;
    private GameObject _dropdownInstance;

    void Awake()
    {
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }
    }

    public void Initialize(ComponentClass componentClass) // ������ ComponentManager ��������
    {
        _componentClass = componentClass;
        iconImage.sprite = componentClass.toolbarIcon; // ���������� toolbarIcon
        GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        if (_componentClass.subclasses.Count > 1)
        {
            ToggleDropdownMenu();
        }
        else if (_componentClass.subclasses.Count == 1)
        {
            CreateComponent(_componentClass.subclasses[0].prefab);
        }
    }

    private void ToggleDropdownMenu()
    {
        if (_dropdownInstance == null)
        {
            CreateDropdownMenu();
        }
        else
        {
            Destroy(_dropdownInstance);
            _dropdownInstance = null;
        }
    }

    private void CreateDropdownMenu()
    {
        _dropdownInstance = Instantiate(dropdownPrefab, transform.parent.parent);
        _dropdownInstance.transform.position = transform.position + Vector3.down * 80;

        // �������� ��������� DropdownController
        DropdownController controller = _dropdownInstance.GetComponent<DropdownController>();
        if (controller != null)
        {
            controller.Initialize(_componentClass, this);
        }
        else
        {
            Debug.LogError("DropdownController component missing on dropdown prefab!");
        }
    }

    public void CreateComponent(GameObject prefab)
    {
        GameObject newComponent = Instantiate(prefab);
        ComponentDragger dragger = newComponent.AddComponent<ComponentDragger>();
        dragger.Initialize(_componentClass.id);

        if (_dropdownInstance != null)
        {
            Destroy(_dropdownInstance);
            _dropdownInstance = null;
        }
    }
}