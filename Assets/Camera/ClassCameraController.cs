using UnityEngine;

public class ClassCameraController : MonoBehaviour
{
    [Header("Class Settings")]
    public string componentClass; // "R", "C", "L" � �.�.

    [Header("Hotkey Settings")]
    public KeyCode modifierKey = KeyCode.F; // ����������� F
    public int hotkeyNumber = 1; // ����� ������� ������� (1-15)

    [Header("Camera Reference")]
    public Camera classCamera;

    [Header("Render Texture & Display Settings")]
    public RenderTexture classRenderTexture; // ������ �� Render Texture
    public UnityEngine.UI.RawImage classDisplayRawImage; // ������ �� Raw Image � Canvas
    public string displayNamePattern = "{0}_Display"; // ������� ��� ������ Display �� �����

    private bool isActive = false;
    private bool modifierHeld = false;
    private float modifierHoldStartTime = 0f;
    private const float modifierHoldTime = 0.3f; // ����� ��������� ������������

    // ������ ��� ������������� ������ �������
    private KeyCode[] numberKeys = {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
        KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0,
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T
    };

    void Start()
    {
        // ������������� ������� ������ �� ���� �� GameObject
        if (classCamera == null)
            classCamera = GetComponent<Camera>();

        // ������������� ���� Render Texture ���� �� ��������
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

        // ������������� ���� Raw Image ���� �� ��������
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
            DeactivateCamera(); // ���������� ������ ���������
            Debug.Log($"������ {componentClass} ����������������. ������� �������: F+{(hotkeyNumber <= 10 ? hotkeyNumber.ToString() : numberKeys[hotkeyNumber - 1].ToString())}");

            if (classDisplayRawImage != null)
                Debug.Log($"������ Display: {classDisplayRawImage.gameObject.name}");
            else
                Debug.LogWarning($"Display ��� ������ {componentClass} �� ������!");

            if (classRenderTexture != null)
                Debug.Log($"������ Render Texture: {classRenderTexture.name}");
            else
                Debug.LogWarning($"Render Texture ��� ������ {componentClass} �� ������!");
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
        // ��������� ������� ������������ F
        if (Input.GetKeyDown(modifierKey))
        {
            modifierHeld = true;
            modifierHoldStartTime = Time.time;
        }

        // ��������� ���������� ������������ F
        if (Input.GetKeyUp(modifierKey))
            modifierHeld = false;

        // �������� ���������� F + �����
        if (modifierHeld && hotkeyNumber >= 1 && hotkeyNumber <= 15)
        {
            KeyCode targetKey = numberKeys[hotkeyNumber - 1];
            if (Input.GetKeyDown(targetKey))
            {
                // ���������, ��� ����������� ������������ ���������� �����
                if (Time.time - modifierHoldStartTime >= modifierHoldTime)
                {
                    ToggleCamera(); // ����� ������������ ������
                    modifierHeld = false; // ���������� ����� ������������
                }
            }
        }

        // �������������� ���������� ���� ����������� ��������� ������� ������
        if (modifierHeld && (Time.time - modifierHoldStartTime) > modifierHoldTime + 0.5f)
            modifierHeld = false;
    }

    public void ActivateCamera()
    {
        if (classCamera != null && !classCamera.enabled)
        {
            // �������� Render Texture ���� �� ����
            if (classRenderTexture != null)
            {
                classCamera.targetTexture = classRenderTexture;
                classRenderTexture.Create(); // ���������� ��� Render Texture ������
            }

            classCamera.enabled = true;
            isActive = true;

            // �������� Raw Image ���� �� ����
            if (classDisplayRawImage != null)
            {
                classDisplayRawImage.enabled = true;
                // ��������� Render Texture ���� �� ����
                if (classRenderTexture != null)
                {
                    classDisplayRawImage.texture = classRenderTexture;
                }
            }

            Debug.Log($"������������ ������ � ����������� ��� ������ {componentClass}");
        }
    }

    public void DeactivateCamera()
    {
        if (classCamera != null && classCamera.enabled)
        {
            classCamera.enabled = false;
            isActive = false;

            // ����������� Render Texture
            if (classRenderTexture != null)
            {
                classCamera.targetTexture = null;
                classRenderTexture.Release(); // ����������� ������� Render Texture
            }

            // ��������� Raw Image
            if (classDisplayRawImage != null)
            {
                classDisplayRawImage.enabled = false;
                classDisplayRawImage.texture = null;
            }
        }
    }

    public void ToggleCamera()
    {
        // ���������� ������������ ��������� ������
        if (isActive)
            DeactivateCamera();
        else
            ActivateCamera();
    }

    public string GetComponentClass() => componentClass;
    public bool IsActive() => isActive && classCamera != null && classCamera.enabled;

    // ����� ��� ��������������� ������������ ��������
    public void ReleaseResources()
    {
        if (classRenderTexture != null)
        {
            classRenderTexture.Release();
        }
    }

    // ����� ��� ������ � ���������� Display �� �����
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