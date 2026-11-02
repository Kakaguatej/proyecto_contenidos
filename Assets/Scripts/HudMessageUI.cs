using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Mensajes breves del HUD con aparición y desaparición suaves.
/// No usa Update ni búsquedas en tiempo de ejecución.
/// </summary>
public class HudMessageUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Tiempos")]
    [SerializeField] private float fadeInDuration = 0.15f;
    [SerializeField] private float holdDuration = 1.2f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    [Header("Colores")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color alertColor = new Color(1f, 0.35f, 0.35f, 1f);

    private GameState gameState;
    private Coroutine showRoutine;
    private float lastHealth = -1f;
    private float lastTemperature = -1f;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (messageText == null) messageText = GetComponent<TextMeshProUGUI>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    private void Start()
    {
        StartCoroutine(InitializeWhenReady());
    }

    private IEnumerator InitializeWhenReady()
    {
        float timeout = 3f;

        while (GameState.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (GameState.Instance == null)
        {
            Debug.LogWarning("[HudMessageUI] No se encontró GameState.");
            enabled = false;
            yield break;
        }

        // Espera un frame para que GameState.Start ejecute ResetState.
        yield return null;

        gameState = GameState.Instance;
        gameState.OnHealthChanged += HandleHealthChanged;
        gameState.OnTemperatureChanged += HandleTemperatureChanged;
        lastHealth = gameState.CurrentHealth;
        lastTemperature = gameState.CurrentTemperature;
    }

    private void OnDestroy()
    {
        if (gameState != null)
        {
            gameState.OnHealthChanged -= HandleHealthChanged;
            gameState.OnTemperatureChanged -= HandleTemperatureChanged;
        }
    }

    private void HandleHealthChanged(float current, float max)
    {
        float previous = lastHealth;
        lastHealth = current;

        if (previous < 0f)
        {
            return;
        }

        if (current < previous - 0.01f && current > 0.01f)
        {
            Show("¡DAÑO RECIBIDO!", alertColor);
        }
    }

    private void HandleTemperatureChanged(float current, float normalized)
    {
        float previous = lastTemperature;
        lastTemperature = current;

        if (previous < 0f)
        {
            return;
        }

        float delta = current - previous;
        if (delta >= 3f)
        {
            Show("¡FALLO DE CIRCUITO!", alertColor);
        }
        else if (delta <= -1f)
        {
            Show("¡CIRCUITO REPARADO!", normalColor);
        }
    }

    public void Show(string message, Color color)
    {
        if (messageText == null || canvasGroup == null)
        {
            return;
        }

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
        }

        showRoutine = StartCoroutine(ShowRoutine(message, color));
    }

    private IEnumerator ShowRoutine(string message, Color color)
    {
        messageText.text = message;
        messageText.color = color;
        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.one * 0.92f;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            canvasGroup.alpha = t;
            transform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one;

        elapsed = 0f;
        while (elapsed < holdDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            canvasGroup.alpha = 1f - t;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        showRoutine = null;
    }
}
