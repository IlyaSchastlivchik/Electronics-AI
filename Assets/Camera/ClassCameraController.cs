using UnityEngine;

public class ClassCameraController : MonoBehaviour
{
    [Header("Class Settings")]
    public string componentClass; // "R", "C", "L" и т.д.

    [Header("Hotkey Settings")]
    public KeyCode modifierKey = KeyCode.F; // Модификатор F
    public int hotkeyNumber = 1; // Номер горячей клавиши (1-15)

    [Header("Camera Reference")]
    public Camera classCamera;

    [Header("Render Texture & Display Settings")]
    public RenderTexture classRenderTexture; // Ссылка на Render Texture
    public UnityEngine.UI.RawImage classDisplayRawImage; // Ссылка на Raw Image в Canvas
    public string displayNamePattern = "{0}_Display"; // Паттерн для поиска Display по имени

    private bool isActive = false;
    private bool modifierHeld = false;
    private float modifierHoldStartTime = 0f;
    private const float modifierHoldTime = 0.3f; // Время удержания модификатора

    // Массив для сопоставления номера клавише
    private KeyCode[] numberKeys = {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
        KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0,
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T
    };

    void Start()
    {
        // Автоматически находим камеру на этом же GameObject
        if (classCamera == null)
            classCamera = GetComponent<Camera>();

        // Автоматически ищем Render Texture если не назначен
        if (classRenderTexture == null)
        {
            string rtName = componentClass + "_RenderTexture";
            RenderTexture[] allRTs = Resources.FindObjectsOfTypeAll<RenderTexture>();
            foreach (var rt in allRTs)
            {
                if (rt.name == rtName)
                {
                    classRenderTexture = rt;
                    break;
                }
            }
        }

        // Автоматически ищем Raw Image если не назначен
        if (classDisplayRawImage == null)
        {
            string displayName = string.Format(displayNamePattern, componentClass);
            GameObject displayObj = GameObject.Find(displayName);
            if (displayObj != null)
            {
                classDisplayRawImage = displayObj.GetComponent<UnityEngine.UI.RawImage>();
            }
        }

        if (classCamera != null)
        {
            DeactivateCamera(); // Изначально камера выключена
            Debug.Log($"Камера {componentClass} инициализирована. Горячая клавиша: F+{(hotkeyNumber <= 10 ? hotkeyNumber.ToString() : numberKeys[hotkeyNumber - 1].ToString())}");

            if (classDisplayRawImage != null)
                Debug.Log($"Найден Display: {classDisplayRawImage.gameObject.name}");
            else
                Debug.LogWarning($"Display для класса {componentClass} не найден!");

            if (classRenderTexture != null)
                Debug.Log($"Найден Render Texture: {classRenderTexture.name}");
            else
                Debug.LogWarning($"Render Texture для класса {componentClass} не найден!");
        }
        else
            Debug.LogError($"Camera not found on {gameObject.name}!");
    }

    void Update()
    {
        HandleHotkeyInput();
    }

    private void HandleHotkeyInput()
    {
        // Обработка нажатия модификатора F
        if (Input.GetKeyDown(modifierKey))
        {
            modifierHeld = true;
            modifierHoldStartTime = Time.time;
        }

        // Обработка отпускания модификатора F
        if (Input.GetKeyUp(modifierKey))
            modifierHeld = false;

        // Проверка комбинации F + номер
        if (modifierHeld && hotkeyNumber >= 1 && hotkeyNumber <= 15)
        {
            KeyCode targetKey = numberKeys[hotkeyNumber - 1];
            if (Input.GetKeyDown(targetKey))
            {
                // Проверяем, что модификатор удерживается достаточно долго
                if (Time.time - modifierHoldStartTime >= modifierHoldTime)
                {
                    ToggleCamera(); // Вызов переключения камеры
                    modifierHeld = false; // Сбрасываем после срабатывания
                }
            }
        }

        // Автоматическое отключение если модификатор отпустили слишком быстро
        if (modifierHeld && (Time.time - modifierHoldStartTime) > modifierHoldTime + 0.5f)
            modifierHeld = false;
    }

    public void ActivateCamera()
    {
        if (classCamera != null && !classCamera.enabled)
        {
            // Включаем Render Texture если он есть
            if (classRenderTexture != null)
            {
                classCamera.targetTexture = classRenderTexture;
                classRenderTexture.Create(); // Убеждаемся что Render Texture создан
            }

            classCamera.enabled = true;
            isActive = true;

            // Включаем Raw Image если он есть
            if (classDisplayRawImage != null)
            {
                classDisplayRawImage.enabled = true;
                // Назначаем Render Texture если он есть
                if (classRenderTexture != null)
                {
                    classDisplayRawImage.texture = classRenderTexture;
                }
            }

            Debug.Log($"Активирована камера и отображение для класса {componentClass}");
        }
    }

    public void DeactivateCamera()
    {
        if (classCamera != null && classCamera.enabled)
        {
            classCamera.enabled = false;
            isActive = false;

            // Освобождаем Render Texture
            if (classRenderTexture != null)
            {
                classCamera.targetTexture = null;
                classRenderTexture.Release(); // Освобождаем ресурсы Render Texture
            }

            // Отключаем Raw Image
            if (classDisplayRawImage != null)
            {
                classDisplayRawImage.enabled = false;
                classDisplayRawImage.texture = null;
            }
        }
    }

    public void ToggleCamera()
    {
        // Реализация переключения состояния камеры
        if (isActive)
            DeactivateCamera();
        else
            ActivateCamera();
    }

    public string GetComponentClass() => componentClass;
    public bool IsActive() => isActive && classCamera != null && classCamera.enabled;

    // Метод для принудительного освобождения ресурсов
    public void ReleaseResources()
    {
        if (classRenderTexture != null)
        {
            classRenderTexture.Release();
        }
    }

    // Метод для поиска и назначения Display по имени
    public bool TryFindAndAssignDisplay()
    {
        string displayName = string.Format(displayNamePattern, componentClass);
        GameObject displayObj = GameObject.Find(displayName);
        if (displayObj != null)
        {
            classDisplayRawImage = displayObj.GetComponent<UnityEngine.UI.RawImage>();
            return classDisplayRawImage != null;
        }
        return false;
    }
}