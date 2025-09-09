using UnityEngine;
using System.Collections.Generic;

// Этот файл должен быть создан как ComponentDatabase.cs
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
    public KeyCode hotkey; // Горячая клавиша для активации меню
    public GameObject toolbarPanelPrefab; // Ссылка на префаб панели инструментов
    public List<ComponentSubclass> subclasses = new List<ComponentSubclass>();
}

[System.Serializable]
public class ComponentSubclass
{
    public string name;
    public Sprite icon;
    public GameObject prefab;
    public KeyCode hotkey; // Горячая клавиша для создания компонента
}