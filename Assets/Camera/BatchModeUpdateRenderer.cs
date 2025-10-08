using UnityEngine;

public class BatchModeUpdateRenderer : MonoBehaviour
{
    Camera m_Camera;

    void Start()
    {
        // �������� ������ �� ��������� ������
        m_Camera = GetComponent<Camera>();
    }

    void Update()
    {
        // ���� ���������� �������� � batch mode � ������ ����������,
        // �������� � ������ ����
        if (Application.isBatchMode && m_Camera)
            m_Camera.Render();
    }
}