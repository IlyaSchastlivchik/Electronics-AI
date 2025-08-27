using UnityEngine;

public class CircuitPin : MonoBehaviour
{
    public string pinType = "default";
    public CircuitComponent parentComponent;

    void Start()
    {
        parentComponent = GetComponentInParent<CircuitComponent>();
    }
}