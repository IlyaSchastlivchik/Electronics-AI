using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Canvas Reference")]
    public Canvas targetCanvas; // Перетащите сюда ваш основной Canvas

    [Header("Hotkey Settings")]
    public bool enableHotkeys = true;

    [Header("Global Control Settings")]
    public KeyCode globalDisableKey = KeyCode.F12; // Глобальная клавиша отключения всех камер

    private Dictionary<string, ClassCameraController> cameraDictionary = new Dictionary<string, ClassCameraController>();
    private Dictionary<int, ClassCameraController> hotkeyDictionary = new Dictionary<int, ClassCameraController>();
    private bool allCamerasDisabled = false;

    void Start()
    {
        InitializeAllCameras();
    }

    void Update()
    {
        // Глобальная обработка горячих клавиш для быстрого отключения всех камер
        if (enableHotkeys && Input.GetKey(KeyCode.F) && Input.GetKeyDown(KeyCode.Escape))
            DeactivateAllCameras();

        // Глобальное отключение/включение всех камер и дисплеев
        if (Input.GetKeyDown(globalDisableKey))
        {
            ToggleAllCameras();
        }
    }

    private void InitializeAllCameras()
    {
        cameraDictionary.Clear();
        hotkeyDictionary.Clear();

        if (targetCanvas == null)
        {
            Debug.LogError("Target Canvas not assigned in CameraManager!");
            return;
        }

        // Находим все контроллеры камер в сцене
        ClassCameraController[] cameraControllers = FindObjectsOfType<ClassCameraController>();

        foreach (var controller in cameraControllers)
        {
            string className = controller.GetComponentClass();
            int hotkeyNum = controller.hotkeyNumber;

            if (!string.IsNullOrEmpty(className))
            {
                // Регистрируем по классу компонента
                if (!cameraDictionary.ContainsKey(className))
                    cameraDictionary.Add(className, controller);
                else
                    Debug.LogWarning($"Дублирующаяся камера для класса {className}!");

                // Регистрируем по горячей клавише
                if (hotkeyNum >= 1 && hotkeyNum <= 15)
                {
                    if (!hotkeyDictionary.ContainsKey(hotkeyNum))
                        hotkeyDictionary.Add(hotkeyNum, controller);
                    else
                        Debug.LogWarning($"Дублирование горячей клавиши F+{hotkeyNum}!");
                }
            }
        }

        Debug.Log($"Всего зарегистрировано камер: {cameraDictionary.Count}");
        DeactivateAllCameras(); // Деактивируем все камеры при старте
    }

    public void ActivateCameraForClass(string className)
    {
        if (allCamerasDisabled)
        {
            Debug.Log("Все камеры временно отключены глобально. Используйте " + globalDisableKey + " для включения.");
            return;
        }

        if (cameraDictionary.ContainsKey(className))
        {
            // Деактивируем все камеры перед активацией нужной (режим эксклюзивности)
            DeactivateAllCameras();
            cameraDictionary[className].ActivateCamera();
        }
        else
            Debug.LogWarning($"Камера для класса {className} не найдена!");
    }

    public void DeactivateCameraForClass(string className)
    {
        if (cameraDictionary.ContainsKey(className))
            cameraDictionary[className].DeactivateCamera();
    }

    public void DeactivateAllCameras()
    {
        foreach (var controller in cameraDictionary.Values)
            if (controller != null)
                controller.DeactivateCamera();
    }

    public void ToggleAllCameras()
    {
        allCamerasDisabled = !allCamerasDisabled;

        if (allCamerasDisabled)
        {
            DeactivateAllCameras();
            Debug.Log("Все камеры и дисплеи отключены глобально");
        }
        else
        {
            Debug.Log("Глобальное отключение камер снято. Камеры можно активировать индивидуально.");
        }
    }

    // Метод для принудительного освобождения всех ресурсов
    public void ReleaseAllResources()
    {
        foreach (var controller in cameraDictionary.Values)
        {
            if (controller != null)
            {
                controller.ReleaseResources();
            }
        }
        Debug.Log("Все ресурсы камер освобождены");
    }

    // Метод для вызова из CompactVerticalAutoSnap
    public void NotifyComponentsAdded(string className, int componentCount)
    {
        if (componentCount > 0 && cameraDictionary.ContainsKey(className))
            ActivateCameraForClass(className);
        else if (componentCount > 0)
            Debug.LogWarning($"Камера для класса {className} не найдена!");
    }

    // Метод для получения статуса всех камер
    public void LogCameraStatus()
    {
        Debug.Log("=== СТАТУС КАМЕР ===");
        foreach (var kvp in cameraDictionary)
        {
            string status = kvp.Value.IsActive() ? "АКТИВНА" : "ВЫКЛЮЧЕНА";
            Debug.Log($"Камера {kvp.Key}: {status}");
        }
    }

    void OnApplicationQuit()
    {
        ReleaseAllResources();
    }

    void OnDestroy()
    {
        ReleaseAllResources();
    }
}