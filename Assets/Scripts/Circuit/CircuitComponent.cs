using UnityEngine;

public class CircuitComponent : MonoBehaviour
{
    public string componentId;

    void Start()
    {
        componentId = name;
    }
}