using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ComponentDatabase", menuName = "Circuit/Component Database")]
public class ComponentDatabase : ScriptableObject
{
    public List<ComponentClass> classes = new List<ComponentClass>();
}

[System.Serializable]
public class ComponentClass
{
    public string id; // Убедитесь, что используется id, а не className
    public string displayName;
    public Sprite toolbarIcon; // Изменили icon на toolbarIcon
    public List<ComponentSubclass> subclasses = new List<ComponentSubclass>();
}

[System.Serializable]
public class ComponentSubclass
{
    public string name;
    public Sprite icon;
    public GameObject prefab;
}