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

        // Автоматически определяем тип и номер компонента на основе имени
        ParseComponentName();

        // Обновляем текстовую метку
        UpdateLabel();
    }

    private void ParseComponentName()
    {
        // Если тип и номер уже установлены, ничего не делаем
        if (!string.IsNullOrEmpty(componentType) && componentNumber != 0)
            return;

        // Пытаемся извлечь тип и номер из имени объекта
        string objectName = gameObject.name;

        // Ищем первую цифру в имени
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
            // Извлекаем тип (все символы до первой цифры)
            componentType = objectName.Substring(0, firstDigitIndex);

            // Извлекаем номер (все цифры после типа)
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
            // Если не удалось извлечь тип и номер, используем значения по умолчанию
            componentType = "C";
            componentNumber = 0;
        }

        // Обновляем ID на основе типа и номера
        componentId = $"{componentType}{componentNumber}";
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

    public void RenumberComponent(int newNumber)
    {
        componentNumber = newNumber;
        componentId = $"{componentType}{componentNumber}";
        UpdateLabel();
    }
}