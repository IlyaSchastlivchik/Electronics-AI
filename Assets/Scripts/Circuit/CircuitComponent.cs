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
        // Автоматически находим текстовый компонент если не установлен
        if (labelText == null)
        {
            labelText = GetComponentInChildren<TextMeshPro>();
        }

        // Если ID не установлен, используем имя объекта
        if (string.IsNullOrEmpty(componentId))
        {
            componentId = name;
        }

        // Обновляем текстовую метку
        UpdateLabel();
    }

    public void UpdateLabel()
    {
        if (labelText != null && !string.IsNullOrEmpty(componentId))
        {
            labelText.text = componentId;

            // Позиционируем текст над компонентом
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

    // Метод для установки всех параметров сразу
    public void SetComponentData(string id, string type, int number)
    {
        componentId = id;
        componentType = type;
        componentNumber = number;

        // Обновляем имя объекта в иерархии
        if (!string.IsNullOrEmpty(id))
        {
            name = id;
        }

        UpdateLabel();
    }

    // Метод для получения полного имени компонента
    public string GetFullName()
    {
        return $"{componentType} {componentNumber} ({componentId})";
    }
}