using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Canvas Reference")]
    public Canvas targetCanvas;

    [Header("Hotkey Settings")]
    public bool enableHotkeys = true;
    public KeyCode globalDisableKey = KeyCode.F12;

    private Dictionary<string, ClassCameraController> cameraDictionary = new Dictionary<string, ClassCameraController>();
    private Dictionary<int, ClassCameraController> hotkeyDictionary = new Dictionary<int, ClassCameraController>();
    private bool allCamerasDisabled = false;

    void Start()
    {
        InitializeAllCameras();
    }

    void Update()
    {
        if (enableHotkeys && Input.GetKey(KeyCode.F) && Input.GetKeyDown(KeyCode.Escape))
            DeactivateAllCameras();

        if (Input.GetKeyDown(globalDisableKey))
            ToggleAllCameras();
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

        ClassCameraController[] cameraControllers = FindObjectsOfType<ClassCameraController>();

        foreach (var controller in cameraControllers)
        {
            string className = controller.GetComponentClass();
            int hotkeyNum = controller.hotkeyNumber;

            if (!string.IsNullOrEmpty(className))
            {
                if (!cameraDictionary.ContainsKey(className))
                    cameraDictionary.Add(className, controller);

                if (hotkeyNum >= 1 && hotkeyNum <= 15)
                {
                    if (!hotkeyDictionary.ContainsKey(hotkeyNum))
                        hotkeyDictionary.Add(hotkeyNum, controller);
                }
            }
        }

        Debug.Log($"Всего зарегистрировано камер: {cameraDictionary.Count}");
        DeactivateAllCameras();
    }

    public void ActivateCameraForClass(string className)
    {
        if (allCamerasDisabled)
        {
            Debug.Log("Все камеры временно отключены глобально");
            return;
        }

        if (cameraDictionary.ContainsKey(className))
        {
            // Деактивируем все камеры перед активацией нужной
            DeactivateAllCameras();
            cameraDictionary[className].ActivateCamera();
            Debug.Log($"Активирована камера для класса {className}");
        }
        else
        {
            Debug.LogWarning($"Камера для класса {className} не найдена!");
        }
    }

    /// <summary>
    /// Активирует камеру без деактивации других камер
    /// </summary>
    public void ActivateCameraForClassWithoutDeactivation(string className)
    {
        if (allCamerasDisabled)
        {
            Debug.Log("Все камеры временно отключены глобально");
            return;
        }

        if (cameraDictionary.ContainsKey(className))
        {
            // НЕ деактивируем другие камеры
            cameraDictionary[className].ActivateCamera();
            Debug.Log($"Активирована камера для класса {className} (без деактивации других)");
        }
        else
        {
            Debug.LogWarning($"Камера для класса {className} не найдена!");
        }
    }

    public void DeactivateCameraForClass(string className)
    {
        if (cameraDictionary.ContainsKey(className))
        {
            cameraDictionary[className].DeactivateCamera();
            Debug.Log($"Деактивирована камера для класса {className}");
        }
    }

    public void DeactivateAllCameras()
    {
        foreach (var controller in cameraDictionary.Values)
        {
            if (controller != null)
                controller.DeactivateCamera();
        }
        Debug.Log("Все камеры деактивированы");
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
            Debug.Log("Глобальное отключение камер снято");
        }
    }

    public void ReleaseAllResources()
    {
        foreach (var controller in cameraDictionary.Values)
        {
            if (controller != null)
                controller.ReleaseResources();
        }
        Debug.Log("Все ресурсы камер освобождены");
    }

    public void NotifyComponentsAdded(string className, int componentCount)
    {
        if (componentCount > 0 && cameraDictionary.ContainsKey(className))
            ActivateCameraForClass(className);
        else if (componentCount > 0)
            Debug.LogWarning($"Камера для класса {className} не найдена!");
    }

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