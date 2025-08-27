using UnityEngine;

public class GridPointRenderer : MonoBehaviour
{
    [Header("Manual Setup")]
    [Tooltip("Перетащите сюда объект с SnapGridSystem")]
    public SnapGridSystem gridSystem;

    [Tooltip("Перетащите сюда префаб точки")]
    public GameObject pointPrefab;

    [Header("Settings")]
    public float pointScale = 0.5f;
    public bool alwaysUpdate = false;

    private GameObject[] points;

    private void Start()
    {
        if (gridSystem == null)
        {
            Debug.LogError("Не назначен SnapGridSystem!");
            return;
        }

        CreatePoints();
    }

    private void Update()
    {
        if (alwaysUpdate)
        {
            UpdatePoints();
        }
    }

    private void CreatePoints()
    {
        ClearPoints();

        Vector3[] gridPoints = gridSystem.GetAllPoints();
        points = new GameObject[gridPoints.Length];

        for (int i = 0; i < gridPoints.Length; i++)
        {
            points[i] = Instantiate(pointPrefab, transform);
            points[i].transform.position = gridPoints[i];
            points[i].transform.localScale = Vector3.one * pointScale;
        }
    }

    private void UpdatePoints()
    {
        Vector3[] gridPoints = gridSystem.GetAllPoints();

        if (points == null || points.Length != gridPoints.Length)
        {
            CreatePoints();
            return;
        }

        for (int i = 0; i < gridPoints.Length; i++)
        {
            points[i].transform.position = gridPoints[i];
        }
    }

    private void ClearPoints()
    {
        if (points == null) return;

        foreach (GameObject point in points)
        {
            if (point != null) Destroy(point);
        }
    }

    private void OnDestroy()
    {
        ClearPoints();
    }
}