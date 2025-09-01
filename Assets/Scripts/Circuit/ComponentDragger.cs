using UnityEngine;

public class ComponentDragger : MonoBehaviour
{
    private bool _isDragging = true;
    private string _componentPrefix;

    public void Initialize(string prefix)
    {
        _componentPrefix = prefix;
        name = ComponentManager.Instance.GenerateComponentID(_componentPrefix);
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
        Transform container = ComponentManager.Instance.GetListContainer(_componentPrefix);
        if (container != null) transform.SetParent(container);

        CreatePins();
        gameObject.AddComponent<CircuitComponent>(); // ���������, ��� ����� CircuitComponent ����������
    }

    void CreatePins()
    {
        Transform pinsRoot = new GameObject("Pins").transform;
        pinsRoot.SetParent(transform);
        pinsRoot.localPosition = Vector3.zero;

        // ������ ��� ��������� (2 ����)
        CreatePin(pinsRoot, new Vector2(-0.5f, 0));
        CreatePin(pinsRoot, new Vector2(0.5f, 0));
    }

    void CreatePin(Transform parent, Vector2 localPosition)
    {
        GameObject pin = new GameObject($"Pin_{parent.childCount + 1}");
        pin.transform.SetParent(parent);
        pin.transform.localPosition = localPosition;

        pin.AddComponent<CircleCollider2D>().radius = 0.1f;
        pin.AddComponent<CircuitPin>(); // ���������, ��� ����� CircuitPin ����������
    }
}