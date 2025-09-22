using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DiagnosticAutoSnap : MonoBehaviour
{
    [Header("Settings")]
    public KeyCode snapHotkey = KeyCode.P;
    public bool useControlModifier = true;

    private bool isProcessing = false;
    private List<string> debugLog = new List<string>();

    void Update()
    {
        if (CheckHotkey() && !isProcessing)
        {
            StartCoroutine(DiagnosticRoutine());
        }
    }

    private IEnumerator DiagnosticRoutine()
    {
        isProcessing = true;
        debugLog.Clear();

        AddLog("=== DIAGNOSTIC STARTED ===");
        AddLog($"Unity version: {Application.unityVersion}");
        AddLog($"System memory: {SystemInfo.systemMemorySize}MB");

        // Проверка физической системы
        AddLog($"Physics2D.autoSyncTransforms: {Physics2D.autoSyncTransforms}");
        // Новый код
        AddLog($"Physics2D.simulationMode: {Physics2D.simulationMode}");

        // Получение компонентов
        CircuitComponent[] components = FindObjectsOfType<CircuitComponent>(true);
        AddLog($"Found {components.Length} components");

        foreach (CircuitComponent component in components)
        {
            if (component != null && component.gameObject.activeInHierarchy)
            {
                AddLog($"Processing: {component.name}");

                // Краткая проверка коллизий
                Collider2D[] colliders = component.GetComponentsInChildren<Collider2D>();
                AddLog($"- Colliders: {colliders.Length}");

                yield return null;
            }
        }

        AddLog("=== DIAGNOSTIC COMPLETED ===");

        // Вывод всех логов
        foreach (string log in debugLog)
        {
            Debug.Log(log);
        }

        isProcessing = false;
    }

    private void AddLog(string message)
    {
        debugLog.Add($"{Time.time:F2}: {message}");
    }

    private bool CheckHotkey()
    {
        if (useControlModifier)
            return Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(snapHotkey);
        else
            return Input.GetKeyDown(snapHotkey);
    }
}