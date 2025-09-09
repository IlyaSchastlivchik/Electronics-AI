using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    [SerializeField] private ComponentDatabase database;
    [SerializeField] private GameObject mainMenuButtonPrefab;
    [SerializeField] private RectTransform mainMenuPanel;
    [SerializeField] private RectTransform toolbarsContainer; // ��������� ��� ������� ������������

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // ������� ��������� ��� ������� ������������, ���� �� �� ��������
        if (toolbarsContainer == null)
        {
            GameObject container = new GameObject("ToolbarsContainer");
            container.transform.SetParent(mainMenuPanel.parent);
            container.AddComponent<RectTransform>();
            toolbarsContainer = container.GetComponent<RectTransform>();

            // ����������� ���������
            SetupToolbarsContainer();
        }
    }

    private void SetupToolbarsContainer()
    {
        // ����������� ��������� ��� ������� ������������
        toolbarsContainer.anchorMin = new Vector2(0, 1);
        toolbarsContainer.anchorMax = new Vector2(0, 1);
        toolbarsContainer.pivot = new Vector2(0, 1);
        toolbarsContainer.anchoredPosition = Vector2.zero;
        toolbarsContainer.sizeDelta = mainMenuPanel.sizeDelta;
    }

    private void Start()
    {
        GenerateMainMenuButtons();
    }

    private void GenerateMainMenuButtons()
    {
        foreach (ComponentClass cls in database.classes)
        {
            GameObject button = Instantiate(mainMenuButtonPrefab, mainMenuPanel);
            MainMenuButton buttonScript = button.GetComponent<MainMenuButton>();
            if (buttonScript != null)
            {
                buttonScript.Initialize(cls);
            }
        }
    }

    public void ActivateToolbarPanel(ComponentClass componentClass)
    {
        // ������������ ��� ������ � ����� ToolbarPanel
        foreach (Transform child in toolbarsContainer)
        {
            if (child.CompareTag("ToolbarPanel"))
                child.gameObject.SetActive(false);
        }

        // ���������� ������ ������
        GameObject panel = GameObject.Find($"Toolbar_{componentClass.id}");
        if (panel == null)
        {
            panel = Instantiate(componentClass.toolbarPanelPrefab, toolbarsContainer);
            panel.name = $"Toolbar_{componentClass.id}";
            panel.tag = "ToolbarPanel";

            // �������� ��������� RectTransform �� �������
            CopyRectTransformSettings(panel.GetComponent<RectTransform>(),
                componentClass.toolbarPanelPrefab.GetComponent<RectTransform>());

            // ������������� ��� ������� ����
            PositionPanelBelowMainMenu(panel.GetComponent<RectTransform>());

            // �������������� ������
            ToolbarPanel panelScript = panel.GetComponent<ToolbarPanel>();
            if (panelScript != null)
            {
                panelScript.Initialize(componentClass);
            }
            else
            {
                Debug.LogError("ToolbarPanel component missing on panel prefab!");
            }
        }

        panel.SetActive(true);
        ForceLayoutUpdate(panel.GetComponent<RectTransform>());
    }

    private void CopyRectTransformSettings(RectTransform target, RectTransform source)
    {
        // �������� ��� �������� ��������� RectTransform 
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.pivot = source.pivot;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;

        // �������� �������� 
        target.offsetMin = source.offsetMin;
        target.offsetMax = source.offsetMax;
    }

    private void PositionPanelBelowMainMenu(RectTransform panelTransform)
    {
        if (mainMenuPanel != null)
        {
            // ��������� ������� ��� ������� ����
            Vector2 menuPosition = mainMenuPanel.anchoredPosition;
            float menuHeight = mainMenuPanel.rect.height;

            // ������������� ������� ������ ������������
            panelTransform.anchoredPosition = new Vector2(
                menuPosition.x,
                menuPosition.y - menuHeight
            );
        }
    }

    private void ForceLayoutUpdate(RectTransform rectTransform)
    {
        // �������������� ���������� layout 
        Canvas.ForceUpdateCanvases();
        rectTransform.ForceUpdateRectTransforms();

        // �������������� ���������� ��� LayoutGroup 
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    private void Update()
    {
        // �������� ������� ������ ��� ��������� ����
        foreach (ComponentClass cls in database.classes)
        {
            if (Input.GetKeyDown(cls.hotkey))
            {
                ActivateToolbarPanel(cls);
                break;
            }
        }
    }
}