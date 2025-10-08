using UnityEngine;

public class BatchModeUpdateRenderer : MonoBehaviour
{
    Camera m_Camera;

    void Start()
    {
        // Получаем ссылку на компонент камеры
        m_Camera = GetComponent<Camera>();
    }

    void Update()
    {
        // Если приложение работает в batch mode и камера существует,
        // рендерим её каждый кадр
        if (Application.isBatchMode && m_Camera)
            m_Camera.Render();
    }
}