using UnityEngine;

[System.Serializable]
public class ComponentInfo
{
    public string componentName;
    public GameObject prefab;
    public Sprite icon;
    public string category;

    // Дополнительные поля для хранения специфических данных компонента
    public float resistance; // Для резисторов
    public float capacitance; // Для конденсаторов
    public float inductance; // Для катушек индуктивности
    public string description;

    // Метод для клонирования объекта
    public ComponentInfo Clone()
    {
        return new ComponentInfo
        {
            componentName = this.componentName,
            prefab = this.prefab,
            icon = this.icon,
            category = this.category,
            resistance = this.resistance,
            capacitance = this.capacitance,
            inductance = this.inductance,
            description = this.description
        };
    }
}