using UnityEngine;

[System.Serializable]
public class ComponentInfo
{
    public string componentName;
    public GameObject prefab;
    public Sprite icon;
    public string category;

    // �������������� ���� ��� �������� ������������� ������ ����������
    public float resistance; // ��� ����������
    public float capacitance; // ��� �������������
    public float inductance; // ��� ������� �������������
    public string description;

    // ����� ��� ������������ �������
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