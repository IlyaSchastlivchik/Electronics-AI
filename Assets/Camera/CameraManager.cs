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

        Debug.Log($"����� ���������������� �����: {cameraDictionary.Count}");
        DeactivateAllCameras();
    }

    public void ActivateCameraForClass(string className)
    {
        if (allCamerasDisabled)
        {
            Debug.Log("��� ������ �������� ��������� ���������");
            return;
        }

        if (cameraDictionary.ContainsKey(className))
        {
            // ������������ ��� ������ ����� ���������� ������
            DeactivateAllCameras();
            cameraDictionary[className].ActivateCamera();
            Debug.Log($"������������ ������ ��� ������ {className}");
        }
        else
        {
            Debug.LogWarning($"������ ��� ������ {className} �� �������!");
        }
    }

    /// <summary>
    /// ���������� ������ ��� ����������� ������ �����
    /// </summary>
    public void ActivateCameraForClassWithoutDeactivation(string className)
    {
        if (allCamerasDisabled)
        {
            Debug.Log("��� ������ �������� ��������� ���������");
            return;
        }

        if (cameraDictionary.ContainsKey(className))
        {
            // �� ������������ ������ ������
            cameraDictionary[className].ActivateCamera();
            Debug.Log($"������������ ������ ��� ������ {className} (��� ����������� ������)");
        }
        else
        {
            Debug.LogWarning($"������ ��� ������ {className} �� �������!");
        }
    }

    public void DeactivateCameraForClass(string className)
    {
        if (cameraDictionary.ContainsKey(className))
        {
            cameraDictionary[className].DeactivateCamera();
            Debug.Log($"�������������� ������ ��� ������ {className}");
        }
    }

    public void DeactivateAllCameras()
    {
        foreach (var controller in cameraDictionary.Values)
        {
            if (controller != null)
                controller.DeactivateCamera();
        }
        Debug.Log("��� ������ ��������������");
    }

    public void ToggleAllCameras()
    {
        allCamerasDisabled = !allCamerasDisabled;

        if (allCamerasDisabled)
        {
            DeactivateAllCameras();
            Debug.Log("��� ������ � ������� ��������� ���������");
        }
        else
        {
            Debug.Log("���������� ���������� ����� �����");
        }
    }

    public void ReleaseAllResources()
    {
        foreach (var controller in cameraDictionary.Values)
        {
            if (controller != null)
                controller.ReleaseResources();
        }
        Debug.Log("��� ������� ����� �����������");
    }

    public void NotifyComponentsAdded(string className, int componentCount)
    {
        if (componentCount > 0 && cameraDictionary.ContainsKey(className))
            ActivateCameraForClass(className);
        else if (componentCount > 0)
            Debug.LogWarning($"������ ��� ������ {className} �� �������!");
    }

    public void LogCameraStatus()
    {
        Debug.Log("=== ������ ����� ===");
        foreach (var kvp in cameraDictionary)
        {
            string status = kvp.Value.IsActive() ? "�������" : "���������";
            Debug.Log($"������ {kvp.Key}: {status}");
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