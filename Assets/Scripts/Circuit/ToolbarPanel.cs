using UnityEngine;
using UnityEngine.UI;

public class ToolbarPanel : MonoBehaviour
{
    [SerializeField] private GameObject componentButtonPrefab;
    private ComponentClass _componentClass;

    public void Initialize(ComponentClass componentClass)
    {
        _componentClass = componentClass;
        GenerateToolbarButtons();
    }

    private void GenerateToolbarButtons()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        foreach (ComponentSubclass subclass in _componentClass.subclasses)
        {
            GameObject button = Instantiate(componentButtonPrefab, transform);
            button.name = $"{subclass.name}Button";

            ToolbarButton buttonScript = button.GetComponent<ToolbarButton>();
            if (buttonScript != null)
            {
                buttonScript.Initialize(subclass);
            }
        }
        ForceLayoutUpdate();
    }

    private void ForceLayoutUpdate()
    {
        Canvas.ForceUpdateCanvases();
        HorizontalOrVerticalLayoutGroup layoutGroup = GetComponent<HorizontalOrVerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }
    }

    // НОВЫЙ МЕТОД: мост для вызова из ToolbarButton
    public void CreateComponentFromButton(ComponentSubclass subclass)
    {
        CreateComponent(subclass);
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

        foreach (ComponentSubclass subclass in _componentClass.subclasses)
        {
            if (Input.GetKeyDown(subclass.hotkey))
            {
                CreateComponent(subclass);
                break;
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            gameObject.SetActive(false);
        }
    }

    private void CreateComponent(ComponentSubclass subclass)
    {
        GameObject newComponent = Instantiate(subclass.prefab);
        ComponentDragger dragger = newComponent.GetComponent<ComponentDragger>();
        if (dragger == null)
        {
            dragger = newComponent.AddComponent<ComponentDragger>();
        }

        dragger.Initialize(_componentClass.id, subclass);

        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.CloseAllToolbarPanels();
        }
    }
}