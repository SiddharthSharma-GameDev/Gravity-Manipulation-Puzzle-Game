using System.Collections;
using TMPro;
using UnityEngine;

public class GravityDirectionUI : MonoBehaviour
{
    [SerializeField] private TMP_Text gravityText;

    [Header("Position")]
    [SerializeField] private Vector2 screenOffset = new Vector2(260f, 0f);

    [Header("Show Animation")]
    [SerializeField] private float enterDuration = 0.16f;
    [SerializeField] private float popScale = 1.08f;
    [SerializeField] private float settleDuration = 0.16f;
    [SerializeField] private float fadeOutDuration = 0.18f;

    [Header("Hologram Arrow")]
    [SerializeField] private float arrowPulseSpeed = 2.5f;
    [SerializeField] private float arrowSizePulse = 8f;
    [SerializeField] private float arrowAlphaPulse = 0.25f;
    [SerializeField] private float arrowGlowSwitchSpeed = 0.18f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Vector2 baseAnchoredPosition;
    private Vector3 baseScale;

    private Coroutine animationRoutine;
    private Coroutine hideRoutine;

    private string currentSymbol;
    private string currentLabel;

    private void Awake()
    {
        if (gravityText == null)
            return;

        rectTransform = gravityText.rectTransform;
        canvasGroup = gravityText.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gravityText.gameObject.AddComponent<CanvasGroup>();

        baseAnchoredPosition = rectTransform.anchoredPosition + screenOffset;
        baseScale = rectTransform.localScale;

        HideInstant();
    }

    public void ShowSelectedDirection(string directionSymbol)
    {
        if (gravityText == null)
            return;

        currentSymbol = directionSymbol;
        currentLabel = GetDirectionLabel(directionSymbol);

        gravityText.gameObject.SetActive(true);

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(AnimateShowLoop());
    }

    public void HideDirection()
    {
        if (gravityText == null)
            return;

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(AnimateHide());
    }

    private string GetDirectionLabel(string symbol)
    {
        if (symbol == "↑")
            return "UP";

        if (symbol == "↓")
            return "DOWN";

        if (symbol == "←")
            return "LEFT";

        if (symbol == "→")
            return "RIGHT";

        return "SHIFT";
    }

    private string BuildText(float pulseValue, int glowFrame)
    {
        int arrowSize = Mathf.RoundToInt(Mathf.Lerp(210f, 210f + arrowSizePulse, pulseValue));
        string arrowColor = GetArrowColor(glowFrame, pulseValue);

        return
            "<size=70%><color=#8CF6FFFF><b>GRAVITY SHIFT</b></color></size>\n" +
            $"<size={arrowSize}%><color={arrowColor}><b>{currentSymbol}</b></color></size>\n" +
            $"<size=105%><color=#D8FFFFFF><b>{currentLabel}</b></color></size>\n" +
            "<size=58%><color=#A7EFFFFF>PRESS ENTER TO APPLY</color></size>";
    }

    private string GetArrowColor(int glowFrame, float pulseValue)
    {
        int alpha;

        if (glowFrame % 3 == 0)
            alpha = Mathf.RoundToInt(Mathf.Lerp(180f, 255f, pulseValue));
        else if (glowFrame % 3 == 1)
            alpha = Mathf.RoundToInt(Mathf.Lerp(140f, 230f, pulseValue));
        else
            alpha = Mathf.RoundToInt(Mathf.Lerp(210f, 255f, pulseValue));

        alpha = Mathf.Clamp(alpha, 0, 255);

        return $"#00F6FF{alpha:X2}";
    }

    private IEnumerator AnimateShowLoop()
    {
        canvasGroup.alpha = 0f;
        rectTransform.localScale = baseScale * 0.82f;
        rectTransform.anchoredPosition = baseAnchoredPosition;

        float t = 0f;

        while (t < enterDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / enterDuration);
            float eased = 1f - Mathf.Pow(1f - p, 3f);

            canvasGroup.alpha = Mathf.Lerp(0f, 1f, eased);
            rectTransform.localScale = Vector3.Lerp(baseScale * 0.82f, baseScale * popScale, eased);
            rectTransform.anchoredPosition = baseAnchoredPosition;

            gravityText.text = BuildText(1f, 0);

            yield return null;
        }

        t = 0f;

        while (t < settleDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / settleDuration);
            float eased = 1f - Mathf.Pow(1f - p, 3f);

            rectTransform.localScale = Vector3.Lerp(baseScale * popScale, baseScale, eased);
            rectTransform.anchoredPosition = baseAnchoredPosition;

            yield return null;
        }

        float loopTime = 0f;
        float glowTimer = 0f;
        int glowFrame = 0;

        while (true)
        {
            loopTime += Time.deltaTime * arrowPulseSpeed;
            glowTimer += Time.deltaTime;

            if (glowTimer >= arrowGlowSwitchSpeed)
            {
                glowTimer = 0f;
                glowFrame++;
            }

            float pulse = (Mathf.Sin(loopTime) + 1f) * 0.5f;
            float softAlpha = 1f - arrowAlphaPulse + pulse * arrowAlphaPulse;

            canvasGroup.alpha = softAlpha;
            rectTransform.localScale = baseScale;
            rectTransform.anchoredPosition = baseAnchoredPosition;

            gravityText.text = BuildText(pulse, glowFrame);

            yield return null;
        }
    }

    private IEnumerator AnimateHide()
    {
        float startAlpha = canvasGroup.alpha;
        Vector3 startScale = rectTransform.localScale;

        float t = 0f;

        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeOutDuration);
            float eased = 1f - Mathf.Pow(1f - p, 3f);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);
            rectTransform.localScale = Vector3.Lerp(startScale, baseScale * 0.92f, eased);
            rectTransform.anchoredPosition = baseAnchoredPosition;

            yield return null;
        }

        HideInstant();
    }

    private void HideInstant()
    {
        if (gravityText == null)
            return;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (rectTransform != null)
        {
            rectTransform.localScale = baseScale;
            rectTransform.anchoredPosition = baseAnchoredPosition;
        }

        gravityText.gameObject.SetActive(false);
    }
}