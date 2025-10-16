using UnityEngine;

public class ComponentDragger : MonoBehaviour
{
    private bool _isDragging = true;
    private string _componentPrefix;
    private ComponentSubclass _componentSubclass;

    public void Initialize(string prefix, ComponentSubclass subclass)
    {
        _componentPrefix = prefix;
        _componentSubclass = subclass;

        // Генерируем ID используя префикс класса
        name = ComponentManager.Instance.GenerateComponentID(_componentPrefix);

        Debug.Log($"ComponentDragger initialized with prefix: '{_componentPrefix}', name: {name}");
    }

    void Update()
    {
        if (_isDragging)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(
                Mathf.Round(mousePos.x / 0.5f) * 0.5f,
                Mathf.Round(mousePos.y / 0.5f) * 0.5f,
                transform.position.z
            );

            if (Input.GetMouseButtonDown(0))
            {
                _isDragging = false;
                FinalizeComponent();
            }
        }
    }

    void FinalizeComponent()
    {
        Debug.Log($"Finalizing component with prefix: '{_componentPrefix}', looking for container: {_componentPrefix}_List");

        // Получаем контейнер используя префикс класса
        Transform container = ComponentManager.Instance.GetListContainer(_componentPrefix);
        if (container != null)
        {
            transform.SetParent(container);
            Debug.Log($"Successfully parented to container: {container.name}");
        }
        else
        {
            Debug.LogError($"Failed to find container for prefix: {_componentPrefix}");
        }

        CreatePins();

        // Добавляем компонент CircuitComponent
        CircuitComponent circuitComponent = gameObject.AddComponent<CircuitComponent>();

        // Устанавливаем данные компонента
        if (_componentSubclass != null)
        {
            string componentName = name;
            string type = _componentPrefix; // Используем префикс класса как тип
            string numberPart = componentName.Substring(_componentPrefix.Length);

            if (int.TryParse(numberPart, out int number))
            {
                circuitComponent.SetComponentData(componentName, type, number);
            }
            else
            {
                circuitComponent.SetComponentData(componentName, type, 1);
            }
        }

        // Закрываем все панели инструментов после размещения компонента
        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.CloseAllToolbarPanels();
        }
    }

    void CreatePins()
    {
        Transform pinsRoot = new GameObject("Pins").transform;
        pinsRoot.SetParent(transform);
        pinsRoot.localPosition = Vector3.zero;

        CreatePin(pinsRoot, new Vector2(-0.5f, 0));
        CreatePin(pinsRoot, new Vector2(0.5f, 0));
    }

    void CreatePin(Transform parent, Vector2 localPosition)
    {
        GameObject pin = new GameObject($"Pin_{parent.childCount + 1}");
        pin.transform.SetParent(parent);
        pin.transform.localPosition = localPosition;

        pin.AddComponent<CircleCollider2D>().radius = 0.1f;
        pin.AddComponent<CircuitPin>();
    }
}