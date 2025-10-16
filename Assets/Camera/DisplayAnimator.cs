using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DisplayAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float expandDuration = 1.5f;
    public float collapseDuration = 1.0f;
    public AnimationCurve expandCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve collapseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Display Size Settings")]
    public Vector2 minimizedSize = new Vector2(200, 100);
    public Vector2 expandedSize = new Vector2(400, 200);
    public Vector2 compactExpandPosition = new Vector2(0, 0);

    [Header("References")]
    public CompactVerticalAutoSnap autoSnap;
    public CameraManager cameraManager;
    public ComponentManager componentManager;

    [Header("Class Sequence")]
    public string[] classSequence = {
        "R", "C", "D", "L", "U", "G", "Q", "J",
        "K", "S", "Z", "O", "X", "A", "P", "M"
    };

    [Header("Debug")]
    public bool debugMode = true;
    public bool showAnimationProgress = true;

    private Dictionary<string, RectTransform> displayTransforms = new Dictionary<string, RectTransform>();
    private Dictionary<string, Vector2> displayPositions = new Dictionary<string, Vector2>();
    private List<string> activeClasses = new List<string>();
    private Coroutine currentAnimationCoroutine;
    private bool isAnimating = false;
    private string currentProcessingClass = "";

    // События для синхронизации
    public System.Action<string> OnDisplayExpanded;
    public System.Action<string> OnDisplayCollapsed;

    void Start()
    {
        InitializeDisplays();
        SetupDisplayPositions();

        // Подписываемся на события AutoSnap
        if (autoSnap != null)
        {
            autoSnap.OnReadyForNextClass += OnReadyForNextClass;
        }
    }

    void OnDestroy()
    {
        if (autoSnap != null)
        {
            autoSnap.OnReadyForNextClass -= OnReadyForNextClass;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) && !isAnimating)
        {
            StartFullAnimationSequence();
        }
    }

    private void InitializeDisplays()
    {
        displayTransforms.Clear();
        displayPositions.Clear();

        foreach (string className in classSequence)
        {
            string displayName = className + "_Display";
            GameObject displayObj = GameObject.Find(displayName);

            if (displayObj != null)
            {
                RectTransform rectTransform = displayObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    displayTransforms[className] = rectTransform;

                    if (debugMode)
                    {
                        Debug.Log($"Found display for class {className}");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Display not found: {displayName}");
            }
        }
    }

    private void SetupDisplayPositions()
    {
        displayPositions["R"] = new Vector2(-850, 310);
        displayPositions["C"] = new Vector2(-850, 205);
        displayPositions["D"] = new Vector2(-850, 100);
        displayPositions["L"] = new Vector2(-850, -5);
        displayPositions["U"] = new Vector2(-850, -110);
        displayPositions["G"] = new Vector2(-850, -215);
        displayPositions["Q"] = new Vector2(-850, -320);
        displayPositions["J"] = new Vector2(-850, -425);
        displayPositions["K"] = new Vector2(-850, -530);
        displayPositions["S"] = new Vector2(-645, 310);
        displayPositions["Z"] = new Vector2(-645, 205);
        displayPositions["O"] = new Vector2(-645, 100);
        displayPositions["X"] = new Vector2(-645, -5);
        displayPositions["A"] = new Vector2(-645, -110);
        displayPositions["P"] = new Vector2(-645, -215);
        displayPositions["M"] = new Vector2(-645, -320);
    }

    public void StartFullAnimationSequence()
    {
        if (isAnimating) return;

        DetectActiveClasses();

        if (activeClasses.Count == 0)
        {
            Debug.LogWarning("No active classes found for animation!");
            return;
        }

        if (currentAnimationCoroutine != null)
            StopCoroutine(currentAnimationCoroutine);

        currentAnimationCoroutine = StartCoroutine(FullAnimationSequence());
    }

    private void DetectActiveClasses()
    {
        activeClasses.Clear();

        if (componentManager != null)
        {
            foreach (string className in classSequence)
            {
                if (componentManager.HasComponentsInList(className))
                {
                    activeClasses.Add(className);
                    if (debugMode)
                    {
                        Debug.Log($"Active class detected: {className} with {componentManager.GetComponentCountInList(className)} components");
                    }
                }
            }
        }

        if (debugMode)
        {
            Debug.Log($"Detected active classes: {string.Join(", ", activeClasses)}");
        }
    }

    private IEnumerator FullAnimationSequence()
    {
        isAnimating = true;

        if (showAnimationProgress)
            Debug.Log("=== STARTING FULL DISPLAY ANIMATION SEQUENCE ===");

        // Показываем все мини-дисплеи
        yield return StartCoroutine(ShowAllMiniDisplays());

        // Запускаем последовательную обработку через AutoSnap
        if (autoSnap != null)
        {
            // Передаем управление AutoSnap, который будет вызывать обратно через события
            yield return StartCoroutine(autoSnap.StartSynchronizedArrangement(activeClasses));
        }

        // Активируем все камеры в конце
        yield return StartCoroutine(ActivateAllCamerasAtEnd());

        if (showAnimationProgress)
            Debug.Log("=== DISPLAY ANIMATION SEQUENCE COMPLETED ===");

        isAnimating = false;
    }

    private IEnumerator ActivateAllCamerasAtEnd()
    {
        if (debugMode)
            Debug.Log("Activating all cameras for active classes at the end...");

        if (cameraManager != null)
        {
            // Сначала деактивируем все камеры
            cameraManager.DeactivateAllCameras();
            yield return null;

            // Затем активируем все камеры для активных классов
            foreach (string className in activeClasses)
            {
                cameraManager.ActivateCameraForClassWithoutDeactivation(className);
                if (debugMode)
                    Debug.Log($"Activated camera for class: {className}");

                yield return new WaitForSeconds(0.1f);
            }
        }

        if (debugMode)
            Debug.Log($"Total activated cameras: {activeClasses.Count}");
    }

    private IEnumerator ShowAllMiniDisplays()
    {
        if (showAnimationProgress)
            Debug.Log("Showing all mini displays...");

        foreach (string className in activeClasses)
        {
            if (displayTransforms.ContainsKey(className))
            {
                RectTransform display = displayTransforms[className];
                display.anchoredPosition = displayPositions[className];
                display.sizeDelta = minimizedSize;
                display.gameObject.SetActive(true);

                if (debugMode)
                {
                    Debug.Log($"Display {className} shown at position {displayPositions[className]}");
                }
            }
        }

        yield return new WaitForSeconds(1.0f);
    }

    // Вызывается AutoSnap когда готов начать анимацию для следующего класса
    private void OnReadyForNextClass(string className)
    {
        if (debugMode)
            Debug.Log($"AutoSnap ready for next class: {className}");

        // Запускаем анимацию для этого класса
        StartCoroutine(AnimateClassDisplay(className));
    }

    private IEnumerator AnimateClassDisplay(string className)
    {
        if (!displayTransforms.ContainsKey(className)) yield break;

        currentProcessingClass = className;

        if (showAnimationProgress)
            Debug.Log($"=== STARTING DISPLAY ANIMATION FOR CLASS: {className} ===");

        // Шаг 1: Активируем камеру и увеличиваем дисплей
        if (cameraManager != null)
        {
            cameraManager.ActivateCameraForClass(className);
            if (debugMode)
                Debug.Log($"Activated camera for class: {className}");
        }

        yield return StartCoroutine(ExpandDisplay(className));

        // Уведомляем что дисплей расширен и можно начинать расстановку
        OnDisplayExpanded?.Invoke(className);

        if (debugMode)
            Debug.Log($"Display expanded for class: {className}, waiting for arrangement...");

        // Ждем сигнала от AutoSnap что расстановка завершена
        yield return new WaitUntil(() => autoSnap.IsClassArrangementComplete(className));

        if (debugMode)
            Debug.Log($"Arrangement completed for class: {className}");

        // Шаг 2: ЗАДЕРЖКА ДЛЯ ПРОСМОТРА увеличенного окна
        if (autoSnap != null)
        {
            yield return new WaitForSeconds(autoSnap.delayBetweenClasses);
        }

        // Шаг 3: Уменьшаем дисплей до исходного состояния
        yield return StartCoroutine(CollapseDisplay(className));

        // Уведомляем что дисплей свернут
        OnDisplayCollapsed?.Invoke(className);

        // Уведомляем AutoSnap что можно переходить к следующему классу
        if (autoSnap != null)
        {
            autoSnap.NotifyDisplayAnimationComplete(className);
        }

        if (showAnimationProgress)
            Debug.Log($"=== COMPLETED DISPLAY ANIMATION FOR CLASS: {className} ===");

        currentProcessingClass = "";
    }

    private IEnumerator ExpandDisplay(string className)
    {
        if (!displayTransforms.ContainsKey(className)) yield break;

        RectTransform display = displayTransforms[className];
        Vector2 startSize = display.sizeDelta;
        Vector2 startPos = display.anchoredPosition;

        if (debugMode)
            Debug.Log($"Expanding {className} from {startSize} to {expandedSize}");

        yield return StartCoroutine(AnimateSizeAndPosition(
            display,
            startSize,
            expandedSize,
            startPos,
            compactExpandPosition,
            expandDuration,
            expandCurve
        ));
    }

    private IEnumerator CollapseDisplay(string className)
    {
        if (!displayTransforms.ContainsKey(className)) yield break;

        RectTransform display = displayTransforms[className];
        Vector2 startSize = display.sizeDelta;
        Vector2 startPos = display.anchoredPosition;
        Vector2 targetPos = displayPositions[className];

        if (debugMode)
            Debug.Log($"Collapsing {className} from {startSize} to {minimizedSize}");

        yield return StartCoroutine(AnimateSizeAndPosition(
            display,
            startSize,
            minimizedSize,
            startPos,
            targetPos,
            collapseDuration,
            collapseCurve
        ));

        // ДИСПЛЕЙ ОСТАЕТСЯ ВИДИМЫМ после схлопывания
        display.gameObject.SetActive(true);
    }

    private IEnumerator AnimateSizeAndPosition(RectTransform target, Vector2 startSize, Vector2 endSize,
                                             Vector2 startPos, Vector2 endPos, float duration, AnimationCurve curve)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = curve.Evaluate(elapsed / duration);

            target.sizeDelta = Vector2.Lerp(startSize, endSize, t);
            target.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            yield return null;
        }

        target.sizeDelta = endSize;
        target.anchoredPosition = endPos;
    }

    // Public methods
    public void StopAnimation()
    {
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            isAnimating = false;
            currentProcessingClass = "";
        }
    }

    public bool IsAnimating() => isAnimating;
    public List<string> GetActiveClasses() => new List<string>(activeClasses);
    public string GetCurrentProcessingClass() => currentProcessingClass;
}