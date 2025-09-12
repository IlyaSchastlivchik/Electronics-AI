using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TooltipSystem : MonoBehaviour
{
    public static TooltipSystem Instance;

    [Header("Settings")]
    [SerializeField] private GameObject tooltipPrefab;
    [SerializeField] private float showDelay = 0.3f;
    [SerializeField] private float hideDelay = 0.1f;

    [Header("Position Adjustment")]
    [SerializeField] private Vector2 positionOffset = new Vector2(0, -70);
    [SerializeField] private bool preventScreenOverflow = true;
    [SerializeField] private float screenMargin = 20f;

    private GameObject currentTooltip;
    private Coroutine showCoroutine;
    private Coroutine hideCoroutine;
    private bool isShowing = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ShowTooltip(string text, Vector3 screenPosition)
    {
        if (isShowing && currentTooltip != null)
        {
            UpdateTooltip(text, screenPosition);
            return;
        }

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
        }

        showCoroutine = StartCoroutine(ShowTooltipDelayed(text, screenPosition));
    }

    private IEnumerator ShowTooltipDelayed(string text, Vector3 screenPosition)
    {
        yield return new WaitForSeconds(showDelay);

        if (currentTooltip == null && tooltipPrefab != null)
        {
            currentTooltip = Instantiate(tooltipPrefab, transform);
            currentTooltip.name = "ActiveTooltip";
        }

        if (currentTooltip != null)
        {
            UpdateTooltip(text, screenPosition);
            currentTooltip.SetActive(true);
            isShowing = true;
        }
    }

    private void UpdateTooltip(string text, Vector3 screenPosition)
    {
        if (currentTooltip == null) return;

        RectTransform tooltipRect = currentTooltip.GetComponent<RectTransform>();
        Vector3 targetPosition = CalculateTooltipPosition(screenPosition, tooltipRect);

        tooltipRect.position = targetPosition;

        TextMeshProUGUI textComponent = currentTooltip.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = text;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
    }

    private Vector3 CalculateTooltipPosition(Vector3 screenPosition, RectTransform tooltipRect)
    {
        // Базовая позиция со смещением
        Vector3 targetPosition = screenPosition + new Vector3(positionOffset.x, positionOffset.y, 0);

        if (preventScreenOverflow)
        {
            // Предварительный расчет размера подсказки
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

            // Проверка границ экрана :cite[5]
            float tooltipWidth = tooltipRect.rect.width;
            float tooltipHeight = tooltipRect.rect.height;

            // Проверка правой границы
            if (targetPosition.x + tooltipWidth > Screen.width - screenMargin)
            {
                targetPosition.x = Screen.width - tooltipWidth - screenMargin;
            }

            // Проверка левой границы
            if (targetPosition.x < screenMargin)
            {
                targetPosition.x = screenMargin;
            }

            // Проверка нижней границы
            if (targetPosition.y - tooltipHeight < screenMargin)
            {
                targetPosition.y = tooltipHeight + screenMargin;
            }

            // Проверка верхней границы
            if (targetPosition.y > Screen.height - screenMargin)
            {
                targetPosition.y = Screen.height - screenMargin;
            }
        }

        return targetPosition;
    }

    private IEnumerator HideTooltipDelayed()
    {
        yield return new WaitForSeconds(hideDelay);

        if (currentTooltip != null)
        {
            currentTooltip.SetActive(false);
            isShowing = false;
        }
    }

    public void HideTooltip()
    {
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        hideCoroutine = StartCoroutine(HideTooltipDelayed());
    }

    private void Update()
    {
        if (currentTooltip != null && currentTooltip.activeInHierarchy)
        {
            // Обновляем позицию с учетом границ экрана
            RectTransform tooltipRect = currentTooltip.GetComponent<RectTransform>();
            Vector3 targetPosition = CalculateTooltipPosition(Input.mousePosition, tooltipRect);
            tooltipRect.position = targetPosition;
        }
    }

    public void ForceHideTooltip()
    {
        if (currentTooltip != null)
        {
            currentTooltip.SetActive(false);
            isShowing = false;
        }

        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
    }
}