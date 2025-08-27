using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody2D))]
public class DraggableComponent : MonoBehaviour, IDragHandler, IEndDragHandler
{
    private SnapGridSystem gridSystem;
    private bool isDragging = false;
    private Vector3 offset;
    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        gridSystem = FindObjectOfType<SnapGridSystem>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            offset = transform.position - mouseWorldPos;
            offset.z = 0;
            isDragging = true;
        }

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        rb.MovePosition(mousePos + offset);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        SnapToGrid();
    }

    private void SnapToGrid()
    {
        if (gridSystem != null)
        {
            Vector3 nearestPoint = gridSystem.GetNearestPoint(transform.position);
            rb.MovePosition(nearestPoint);
        }
    }
}