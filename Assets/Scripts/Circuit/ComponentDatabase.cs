using UnityEngine;
using System.Collections.Generic;

// ���� ���� ������ ���� ������ ��� ComponentDatabase.cs
[CreateAssetMenu(fileName = "ComponentDatabase", menuName = "Circuit/Component Database")]
public class ComponentDatabase : ScriptableObject
{
    public List<ComponentClass> classes = new List<ComponentClass>();
}

[System.Serializable]
public class ComponentClass
{
    public string id;
    public string displayName;
    public Sprite toolbarIcon;
    public KeyCode hotkey; // ������� ������� ��� ��������� ����
    public GameObject toolbarPanelPrefab; // ������ �� ������ ������ ������������
    public List<ComponentSubclass> subclasses = new List<ComponentSubclass>();
}

[System.Serializable]
public class ComponentSubclass
{
    public string name;
    public Sprite icon;
    public GameObject prefab;
    public KeyCode hotkey; // ������� ������� ��� �������� ����������
}