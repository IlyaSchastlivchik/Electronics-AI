using UnityEngine;

public class SnapGridSystem : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("–ассто€ние между точками сетки")]
    public float gridSpacing = 1f;

    [Tooltip(" оличество точек по горизонтали и вертикали")]
    public int gridSize = 10;

    [Header("Debug")]
    public bool drawGizmos = true;
    public Color gizmoColor = Color.gray;
    public float gizmoRadius = 0.1f;

    private Vector3[] gridPoints;

    private void Awake()
    {
        GenerateGrid();
    }

    public void GenerateGrid()
    {
        gridPoints = new Vector3[gridSize * gridSize];
        float halfSize = (gridSize * gridSpacing) / 2f;

        for (int x = 0, i = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++, i++)
            {
                gridPoints[i] = transform.position + new Vector3(
                    x * gridSpacing - halfSize,
                    y * gridSpacing - halfSize,
                    0
                );
            }
        }
    }

    public Vector3 GetNearestPoint(Vector3 position)
    {
        Vector3 nearestPoint = gridPoints[0];
        float minDistance = Vector3.Distance(position, nearestPoint);

        foreach (Vector3 point in gridPoints)
        {
            float distance = Vector3.Distance(position, point);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPoint = point;
            }
        }

        return nearestPoint;
    }

    public Vector3[] GetAllPoints() => gridPoints;

    private void OnDrawGizmos()
    {
        if (!drawGizmos || gridPoints == null) return;

        Gizmos.color = gizmoColor;
        foreach (Vector3 point in gridPoints)
        {
            Gizmos.DrawSphere(point, gizmoRadius);
        }
    }
}