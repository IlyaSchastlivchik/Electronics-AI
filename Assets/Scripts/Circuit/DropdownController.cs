using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DropdownController : MonoBehaviour
{
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform contentPanel;

    private ToolbarButton _parentButton;
    private ComponentClass _componentClass;

    public void Initialize(ComponentClass componentClass, ToolbarButton parentButton)
    {
        _parentButton = parentButton;
        _componentClass = componentClass;

        CreateButtons();
    }

    private void CreateButtons()
    {
        // Очищаем существующие кнопки
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        // Создаем кнопки для каждого подкласса
        foreach (var subclass in _componentClass.subclasses)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, contentPanel);
            Button button = buttonObj.GetComponent<Button>();
            Image image = buttonObj.GetComponent<Image>();

            image.sprite = subclass.icon;

            button.onClick.AddListener(() => {
                _parentButton.CreateComponent(subclass.prefab);
                Destroy(gameObject);
            });
        }
    }
}