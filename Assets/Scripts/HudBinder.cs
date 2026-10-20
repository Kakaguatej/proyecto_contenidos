using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HudBinder : MonoBehaviour
{
    [Header("1. Estabilidad BIT (Barra de Vida y Retrato)")]
    [SerializeField] private Image hpFillOk;
    [SerializeField] private Image hpFillWarn;
    [SerializeField] private Image hpFillCrit;
    [Tooltip("Velocidad a la que se desliza visualmente la barra de vida")]
    [SerializeField] private float hpSmoothSpeed = 1.5f;

    [Header("Retrato de El Bit (Puedes usar objetos separados o un Image + Sprites)")]
    [SerializeField] private GameObject portraitOkObj;
    [SerializeField] private GameObject portraitHurtObj;
    [SerializeField] private GameObject portraitCritObj;

    [Header("2. Formateo del Sistema")]
    [SerializeField] private Image formatFill;
    [SerializeField] private TextMeshProUGUI formatText;
    [SerializeField] private Color formatNormalColor = Color.white;
    [SerializeField] private Color formatWarningColor = new Color(1f, 0.55f, 0f); // Naranja

    [Header("3. Temperatura (Anillo Radial 360 y Texto)")]
    [SerializeField] private Image tempRingCool; // < 100°C
    [SerializeField] private Image tempRingWarm; // 100°C a 130°C
    [SerializeField] private Image tempRingHot;  // >= 130°C
    [SerializeField] private TextMeshProUGUI tempTextInner; // Ej: "40°" dentro del círculo
    [SerializeField] private TextMeshProUGUI tempTextSide;  // Ej: "40°C" al lado de TEMP
    [Tooltip("Velocidad de animación suave del anillo al enfriarse con un puzzle")]
    [SerializeField] private float tempSmoothSpeed = 0.8f;

    [Header("4. Tensión de Corriente (Marcador en X y Texto)")]
    [SerializeField] private RectTransform voltMarkerNormal;
    [SerializeField] private RectTransform voltMarkerDanger;
    [Tooltip("Posición X local mínima (0V) y máxima (12V) del marcador dentro de volt_frame")]
    [SerializeField] private float markerMinX = -42f;
    [SerializeField] private float markerMaxX = 42f;
    [SerializeField] private TextMeshProUGUI voltageText;

    // Variables internas para animaciones suaves con Corrutinas (sin usar Update fijo)
    private float displayedHealthRatio = 1f;
    private float targetHealthRatio = 1f;
    private Coroutine healthAnimCoroutine;

    private float displayedTempRatio = 0f;
    private float targetTempRatio = 0f;
    private Coroutine tempAnimCoroutine;

    private void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        // Espera un frame por si GameState.Awake aún no se ha ejecutado
        while (GameState.Instance == null || GameState.Instance.Config == null)
        {
            yield return null;
        }

        GameState gs = GameState.Instance;
        gs.OnHealthChanged += HandleHealthChanged;
        gs.OnFormatChanged += HandleFormatChanged;
        gs.OnTemperatureChanged += HandleTemperatureChanged;
        gs.OnVoltageChanged += HandleVoltageChanged;

        // Sincronización inmediata al inicio
        HandleHealthChanged(gs.CurrentHealth, gs.Config.maxHealth);
        HandleFormatChanged(gs.CurrentFormat);
        HandleTemperatureChanged(gs.CurrentTemperature, gs.GetNormalizedTemperature());
        HandleVoltageChanged(gs.CurrentVoltage, gs.GetNormalizedVoltage());
    }

    private void OnDisable()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.OnHealthChanged -= HandleHealthChanged;
            GameState.Instance.OnFormatChanged -= HandleFormatChanged;
            GameState.Instance.OnTemperatureChanged -= HandleTemperatureChanged;
            GameState.Instance.OnVoltageChanged -= HandleVoltageChanged;
        }
    }

    // ==================== 1. VIDA Y RETRATO ====================
    private void HandleHealthChanged(float current, float max)
    {
        targetHealthRatio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        BalanceConfig cfg = GameState.Instance.Config;

        bool isCrit = targetHealthRatio < cfg.healthCritThreshold;
        bool isWarn = !isCrit && targetHealthRatio < cfg.healthWarnThreshold;
        bool isOk = !isCrit && !isWarn;

        // Activa solo la barra del color correspondiente al umbral
        if (hpFillOk != null) hpFillOk.gameObject.SetActive(isOk);
        if (hpFillWarn != null) hpFillWarn.gameObject.SetActive(isWarn);
        if (hpFillCrit != null) hpFillCrit.gameObject.SetActive(isCrit);

        // Activa el retrato correspondiente
        if (portraitOkObj != null) portraitOkObj.SetActive(isOk);
        if (portraitHurtObj != null) portraitHurtObj.SetActive(isWarn);
        if (portraitCritObj != null) portraitCritObj.SetActive(isCrit);

        if (healthAnimCoroutine != null) StopCoroutine(healthAnimCoroutine);
        healthAnimCoroutine = StartCoroutine(AnimateHealthFill());
    }

    private IEnumerator AnimateHealthFill()
    {
        while (!Mathf.Approximately(displayedHealthRatio, targetHealthRatio))
        {
            displayedHealthRatio = Mathf.MoveTowards(displayedHealthRatio, targetHealthRatio, hpSmoothSpeed * Time.deltaTime);
            ApplyHealthFillAmount(displayedHealthRatio);
            yield return null;
        }
        ApplyHealthFillAmount(targetHealthRatio);
    }

    private void ApplyHealthFillAmount(float amount)
    {
        if (hpFillOk != null) hpFillOk.fillAmount = amount;
        if (hpFillWarn != null) hpFillWarn.fillAmount = amount;
        if (hpFillCrit != null) hpFillCrit.fillAmount = amount;
    }

    // ==================== 2. FORMATEO DEL SISTEMA ====================
    private void HandleFormatChanged(float formatPercent)
    {
        if (formatFill != null)
        {
            formatFill.fillAmount = Mathf.Clamp01(formatPercent / 100f);
        }

        if (formatText != null)
        {
            int rounded = Mathf.FloorToInt(formatPercent);
            formatText.text = $"{rounded}%";

            bool isWarning = formatPercent >= GameState.Instance.Config.formatOrangeThreshold;
            formatText.color = isWarning ? formatWarningColor : formatNormalColor;
        }
    }

    // ==================== 3. TEMPERATURA ====================
    private void HandleTemperatureChanged(float currentTemp, float normalizedTemp)
    {
        // Fill Amount = (temperatura - 40) / 140
        targetTempRatio = normalizedTemp;
        BalanceConfig cfg = GameState.Instance.Config;

        bool isHot = currentTemp >= cfg.tempHotDangerThreshold;
        bool isWarm = !isHot && currentTemp >= cfg.tempWarmThreshold;
        bool isCool = !isHot && !isWarm;

        if (tempRingCool != null) tempRingCool.gameObject.SetActive(isCool);
        if (tempRingWarm != null) tempRingWarm.gameObject.SetActive(isWarm);
        if (tempRingHot != null) tempRingHot.gameObject.SetActive(isHot);

        int tempInt = Mathf.RoundToInt(currentTemp);
        if (tempTextInner != null) tempTextInner.text = $"{tempInt}°";
        if (tempTextSide != null) tempTextSide.text = $"{tempInt}°C";

        if (tempAnimCoroutine != null) StopCoroutine(tempAnimCoroutine);
        tempAnimCoroutine = StartCoroutine(AnimateTemperatureRing());
    }

    private IEnumerator AnimateTemperatureRing()
    {
        while (!Mathf.Approximately(displayedTempRatio, targetTempRatio))
        {
            displayedTempRatio = Mathf.MoveTowards(displayedTempRatio, targetTempRatio, tempSmoothSpeed * Time.deltaTime);
            ApplyTempFillAmount(displayedTempRatio);
            yield return null;
        }
        ApplyTempFillAmount(targetTempRatio);
    }

    private void ApplyTempFillAmount(float amount)
    {
        if (tempRingCool != null) tempRingCool.fillAmount = amount;
        if (tempRingWarm != null) tempRingWarm.fillAmount = amount;
        if (tempRingHot != null) tempRingHot.fillAmount = amount;
    }

    // ==================== 4. TENSIÓN DE CORRIENTE ====================
    private void HandleVoltageChanged(float currentVoltage, float normalizedVoltage)
    {
        BalanceConfig cfg = GameState.Instance.Config;
        bool isOverload = currentVoltage > cfg.voltageOverloadThreshold;

        if (voltMarkerNormal != null) voltMarkerNormal.gameObject.SetActive(!isOverload);
        if (voltMarkerDanger != null) voltMarkerDanger.gameObject.SetActive(isOverload);

        float targetX = Mathf.Lerp(markerMinX, markerMaxX, normalizedVoltage);

        if (voltMarkerNormal != null)
        {
            Vector2 pos = voltMarkerNormal.anchoredPosition;
            pos.x = targetX;
            voltMarkerNormal.anchoredPosition = pos;
        }

        if (voltMarkerDanger != null)
        {
            Vector2 pos = voltMarkerDanger.anchoredPosition;
            pos.x = targetX;
            voltMarkerDanger.anchoredPosition = pos;
        }

        if (voltageText != null)
        {
            voltageText.text = $"{currentVoltage:0.0}V";
        }
    }
}