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
        // Очищаем панель от старых кнопок
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Создаем кнопки для каждого подкласса
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

        // Принудительное обновление layout после создания кнопок 
        ForceLayoutUpdate();
    }

    private void ForceLayoutUpdate()
    {
        Canvas.ForceUpdateCanvases();

        // Обновляем LayoutGroup если он есть
        HorizontalOrVerticalLayoutGroup layoutGroup = GetComponent<HorizontalOrVerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }
    }

    private void Update()
    {
        // Проверка горячих клавиш для компонентов
        if (!gameObject.activeSelf) return;

        foreach (ComponentSubclass subclass in _componentClass.subclasses)
        {
            if (Input.GetKeyDown(subclass.hotkey))
            {
                CreateComponent(subclass.prefab);
                break;
            }
        }

        // Возврат в главное меню по Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToMainMenu();
        }
    }

    private void CreateComponent(GameObject prefab)
    {
        GameObject newComponent = Instantiate(prefab);
        ComponentDragger dragger = newComponent.AddComponent<ComponentDragger>();
        dragger.Initialize(_componentClass.id);
    }

    private void ReturnToMainMenu()
    {
        gameObject.SetActive(false);
    }
}