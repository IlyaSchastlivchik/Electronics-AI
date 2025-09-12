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

        // ��������� ������������� ����� � ID � DisplayName ����������
        _finalTooltipText = tooltipTemplate
            .Replace("[ID]", componentClass.id)
            .Replace("[DISPLAY_NAME]", componentClass.displayName)
            .Replace("[ID/DISPLAY_NAME]", $"{componentClass.id}/{componentClass.displayName}");

        // ������� � ��������� �����������
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

        // �������� ��������� ��� �����
        if (TooltipSystem.Instance != null)
        {
            TooltipSystem.Instance.ForceHideTooltip();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // ���������� ��������� ��� ���������
        if (TooltipSystem.Instance != null)
        {
            TooltipSystem.Instance.ShowTooltip(_finalTooltipText, Input.mousePosition);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // �������� ��������� ��� ����� �������
        if (TooltipSystem.Instance != null)
        {
            TooltipSystem.Instance.HideTooltip();
        }
    }
}