using UnityEngine;
using TMPro;

public class CircuitComponent : MonoBehaviour
{
    [Header("Component Identification")]
    public string componentId;
    public string componentType;
    public int componentNumber;

    [Header("Visual Settings")]
    public TextMeshPro labelText;

    void Start()
    {
        // ������������� ������� ��������� ��������� ���� �� ����������
        if (labelText == null)
        {
            labelText = GetComponentInChildren<TextMeshPro>();
        }

        // ���� ID �� ����������, ���������� ��� �������
        if (string.IsNullOrEmpty(componentId))
        {
            componentId = name;
        }

        // ��������� ��������� �����
        UpdateLabel();
    }

    public void UpdateLabel()
    {
        if (labelText != null && !string.IsNullOrEmpty(componentId))
        {
            labelText.text = componentId;

            // ������������� ����� ��� �����������
            PositionLabelAboveComponent();
        }
    }

    private void PositionLabelAboveComponent()
    {
        if (TryGetComponent<Collider2D>(out var collider))
        {
            Bounds bounds = collider.bounds;
            labelText.transform.position = new Vector3(
                bounds.center.x,
                bounds.max.y + 0.2f,
                labelText.transform.position.z
            );
        }
    }

    // ����� ��� ��������� ���� ���������� �����
    public void SetComponentData(string id, string type, int number)
    {
        componentId = id;
        componentType = type;
        componentNumber = number;

        // ��������� ��� ������� � ��������
        if (!string.IsNullOrEmpty(id))
        {
            name = id;
        }

        UpdateLabel();
    }

    // ����� ��� ��������� ������� ����� ����������
    public string GetFullName()
    {
        return $"{componentType} {componentNumber} ({componentId})";
    }
}