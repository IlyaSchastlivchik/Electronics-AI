using UnityEngine;
using UnityEngine.UI;

public class MainMenuButton : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    private ComponentClass _componentClass;

    public void Initialize(ComponentClass componentClass)
    {
        _componentClass = componentClass;
        iconImage.sprite = componentClass.toolbarIcon;
        GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.ActivateToolbarPanel(_componentClass);
        }
    }
}