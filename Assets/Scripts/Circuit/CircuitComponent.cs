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

        // ������������� ���������� ��� � ����� ���������� �� ������ �����
        ParseComponentName();

        // ��������� ��������� �����
        UpdateLabel();
    }

    private void ParseComponentName()
    {
        // ���� ��� � ����� ��� �����������, ������ �� ������
        if (!string.IsNullOrEmpty(componentType) && componentNumber != 0)
            return;

        // �������� ������� ��� � ����� �� ����� �������
        string objectName = gameObject.name;

        // ���� ������ ����� � �����
        int firstDigitIndex = -1;
        for (int i = 0; i < objectName.Length; i++)
        {
            if (char.IsDigit(objectName[i]))
            {
                firstDigitIndex = i;
                break;
            }
        }

        if (firstDigitIndex > 0)
        {
            // ��������� ��� (��� ������� �� ������ �����)
            componentType = objectName.Substring(0, firstDigitIndex);

            // ��������� ����� (��� ����� ����� ����)
            string numberPart = objectName.Substring(firstDigitIndex);
            if (int.TryParse(numberPart, out int number))
            {
                componentNumber = number;
            }
            else
            {
                componentNumber = 0;
            }
        }
        else
        {
            // ���� �� ������� ������� ��� � �����, ���������� �������� �� ���������
            componentType = "C";
            componentNumber = 0;
        }

        // ��������� ID �� ������ ���� � ������
        componentId = $"{componentType}{componentNumber}";
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

    public void RenumberComponent(int newNumber)
    {
        componentNumber = newNumber;
        componentId = $"{componentType}{componentNumber}";
        UpdateLabel();
    }
}