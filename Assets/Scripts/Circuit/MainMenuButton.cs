using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;

    [Header("Tooltip Settings")]
    [SerializeField]
    [TextArea(2, 5)]
    private string tooltipTemplate =
        "Hello: \n[ID/DISPLAY_NAME] - element component menu\nPress and choose";

    private ComponentClass _componentClass;
    private string _finalTooltipText;

    public void Initialize(ComponentClass componentClass)
    {
        _componentClass = componentClass;
        iconImage.sprite = componentClass.toolbarIcon;

        // Формируем окончательный текст с ID и DisplayName компонента
        _finalTooltipText = tooltipTemplate
            .Replace("[ID]", componentClass.id)
            .Replace("[DISPLAY_NAME]", componentClass.displayName)
            .Replace("[ID/DISPLAY_NAME]", $"{componentClass.id}/{componentClass.displayName}");

        // Очищаем и добавляем обработчики
        Button button = GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.ActivateToolbarPanel(_componentClass);
        }

        // Скрываем подсказку при клике
        if (TooltipSystem.Instance != null)
        {
            TooltipSystem.Instance.ForceHideTooltip();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Показываем подсказку при наведении
        if (TooltipSystem.Instance != null)
        {
            TooltipSystem.Instance.ShowTooltip(_finalTooltipText, Input.mousePosition);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Скрываем подсказку при уходе курсора
        if (TooltipSystem.Instance != null)
        {
            TooltipSystem.Instance.HideTooltip();
        }
    }
}