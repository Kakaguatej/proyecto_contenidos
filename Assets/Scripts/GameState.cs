using System;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState Instance { get; private set; }

    [Header("Configuración de Balance")]
    [SerializeField] private BalanceConfig config;
    public BalanceConfig Config => config;

    // Eventos a los que se suscriben el HUD y el Spawner (Sin usar Update en ellos)
    public event Action<float, float> OnHealthChanged;       // (vidaActual, vidaMax)
    public event Action<float> OnFormatChanged;              // (porcentajeFormateo 0-100)
    public event Action<float, float> OnTemperatureChanged;  // (tempActual, tempNormalizada 0-1)
    public event Action<float, float> OnVoltageChanged;      // (voltActual, voltNormalizado 0-1)
    public event Action<DeathReason> OnPlayerDied;           // (razónDeMuerte)

    // Propiedades de solo lectura pública
    public float CurrentHealth { get; private set; }
    public float CurrentFormat { get; private set; }
    public float CurrentTemperature { get; private set; }
    public float CurrentVoltage { get; private set; }
    public bool IsPlayerDead { get; private set; }

    private float timeSinceLastDash;

    // Variables internas para no disparar eventos cada micro-frame si el cambio es imperceptible
    private float lastNotifiedFormat = -1f;
    private float lastNotifiedTemp = -1f;
    private float lastNotifiedVolt = -1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (config == null)
        {
            Debug.LogError("[GameState] ¡Falta asignar el archivo BalanceConfig en el Inspector!");
            enabled = false;
            return;
        }

        ResetState();
    }

    public void ResetState()
    {
        IsPlayerDead = false;
        CurrentHealth = config.initialHealth;
        CurrentFormat = 0f;
        CurrentTemperature = Mathf.Clamp(config.initialTemperature, config.minTemperature, config.maxTemperature);
        CurrentVoltage = Mathf.Clamp(config.initialVoltage, config.minVoltage, config.maxVoltage);
        timeSinceLastDash = config.voltageCooldownDelay;

        // Emitir estado inicial para que el HUD se sincronice al arrancar el nivel
        NotifyHealth();
        NotifyFormat(true);
        NotifyTemperature(true);
        NotifyVoltage(true);
    }

    private void Update()
    {
        if (IsPlayerDead || config == null) return;

        float dt = Time.deltaTime;

        // 1. Subida lineal del Formateo del Sistema
        if (config.formatDurationSeconds > 0f)
        {
            float formatRate = 100f / config.formatDurationSeconds;
            CurrentFormat = Mathf.Min(100f, CurrentFormat + formatRate * dt);
            NotifyFormat(false);

            if (CurrentFormat >= 100f)
            {
                TriggerDeath(DeathReason.SystemFormatted);
                return;
            }
        }

        // 2. Subida pasiva de Temperatura
        CurrentTemperature = Mathf.Clamp(
            CurrentTemperature + config.tempRisePerSecond * dt,
            config.minTemperature,
            config.maxTemperature
        );
        NotifyTemperature(false);

        // 3. Enfriamiento pasivo de Tensión si lleva 3 segundos sin hacer Dash
        timeSinceLastDash += dt;
        if (timeSinceLastDash >= config.voltageCooldownDelay && CurrentVoltage > config.minVoltage)
        {
            CurrentVoltage = Mathf.Max(config.minVoltage, CurrentVoltage - config.voltageDecayPerSecond * dt);
            NotifyVoltage(false);
        }
    }

    // ==================== MÉTODOS PÚBLICOS PARA TUS SCRIPTS ====================

    /// <summary>
    /// Aplica daño a El Bit multiplicado por la curva de temperatura actual.
    /// </summary>
    public void DamagePlayer(float baseDamage)
    {
        if (IsPlayerDead || baseDamage <= 0f) return;

        float normalizedTemp = GetNormalizedTemperature();
        float tempMultiplier = config.tempDamageMultiplierCurve.Evaluate(normalizedTemp);
        float finalDamage = baseDamage * tempMultiplier;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - finalDamage);
        NotifyHealth();

        if (CurrentHealth <= 0f)
        {
            TriggerDeath(DeathReason.ZeroHealth);
        }
    }

    /// <summary>
    /// Regenera vida cuando El Bit destruye un enemigo (+4 Horno, +3 Cortocircuito).
    /// </summary>
    public void OnEnemyKilled(EnemyType type)
    {
        if (IsPlayerDead) return;

        float healAmount = (type == EnemyType.OvercurrentOven)
            ? config.healOnOvenKilled
            : config.healOnShortCircuitKilled;

        CurrentHealth = Mathf.Min(config.maxHealth, CurrentHealth + healAmount);
        NotifyHealth();
    }

    /// <summary>
    /// Sube la tensión cuando El Bit ejecuta un Dash y reinicia el contador de enfriamiento.
    /// </summary>
    public void OnPlayerDashed()
    {
        if (IsPlayerDead) return;

        timeSinceLastDash = 0f;
        CurrentVoltage = Mathf.Clamp(CurrentVoltage + config.voltageAddedPerDash, config.minVoltage, config.maxVoltage);
        NotifyVoltage(true);
    }

    /// <summary>
    /// Ajusta la temperatura al completar o fallar un puzzle de compuertas lógicas.
    /// </summary>
    public void OnPuzzleResult(bool success, PuzzleDifficulty difficulty)
    {
        if (IsPlayerDead) return;

        if (success)
        {
            float reductionPercent = difficulty switch
            {
                PuzzleDifficulty.Easy => config.tempReductionEasy,
                PuzzleDifficulty.Medium => config.tempReductionMedium,
                PuzzleDifficulty.Hard => config.tempReductionHard,
                _ => config.tempReductionEasy
            };

            // Fórmula: nuevaTemp = 40 + (temp - 40) * (1 - porcentaje)
            float excess = Mathf.Max(0f, CurrentTemperature - config.minTemperature);
            CurrentTemperature = config.minTemperature + excess * (1f - reductionPercent);
            CurrentTemperature = Mathf.Clamp(CurrentTemperature, config.minTemperature, config.maxTemperature);
        }
        else
        {
            CurrentTemperature = Mathf.Clamp(
                CurrentTemperature + config.tempPenaltyOnPuzzleFail,
                config.minTemperature,
                config.maxTemperature
            );
        }

        NotifyTemperature(true);
    }

    // ==================== AUXILIARES Y EMISIÓN DE EVENTOS ====================

    public float GetNormalizedTemperature()
    {
        float range = config.maxTemperature - config.minTemperature;
        return range > 0f ? Mathf.Clamp01((CurrentTemperature - config.minTemperature) / range) : 0f;
    }

    public float GetNormalizedVoltage()
    {
        float range = config.maxVoltage - config.minVoltage;
        return range > 0f ? Mathf.Clamp01((CurrentVoltage - config.minVoltage) / range) : 0f;
    }

    private void NotifyHealth()
    {
        OnHealthChanged?.Invoke(CurrentHealth, config.maxHealth);
    }

    private void NotifyFormat(bool force)
    {
        if (force || Mathf.Abs(CurrentFormat - lastNotifiedFormat) >= 0.05f)
        {
            lastNotifiedFormat = CurrentFormat;
            OnFormatChanged?.Invoke(CurrentFormat);
        }
    }

    private void NotifyTemperature(bool force)
    {
        if (force || Mathf.Abs(CurrentTemperature - lastNotifiedTemp) >= 0.05f)
        {
            lastNotifiedTemp = CurrentTemperature;
            OnTemperatureChanged?.Invoke(CurrentTemperature, GetNormalizedTemperature());
        }
    }

    private void NotifyVoltage(bool force)
    {
        if (force || Mathf.Abs(CurrentVoltage - lastNotifiedVolt) >= 0.02f)
        {
            lastNotifiedVolt = CurrentVoltage;
            OnVoltageChanged?.Invoke(CurrentVoltage, GetNormalizedVoltage());
        }
    }

    private void TriggerDeath(DeathReason reason)
    {
        if (IsPlayerDead) return;
        IsPlayerDead = true;
        Debug.Log($"[GameState] ¡El Bit ha muerto! Motivo: {reason}");
        OnPlayerDied?.Invoke(reason);
    }
    // ==================== MÉTODOS EXCLUSIVOS DE DEPURACIÓN (DEBUG) ====================
    public void DebugAddHealth(float delta)
    {
        if (IsPlayerDead) return;
        CurrentHealth = Mathf.Clamp(CurrentHealth + delta, 0f, config.maxHealth);
        NotifyHealth();
        if (CurrentHealth <= 0f) TriggerDeath(DeathReason.ZeroHealth);
    }

    public void DebugAddFormat(float delta)
    {
        if (IsPlayerDead) return;
        CurrentFormat = Mathf.Clamp(CurrentFormat + delta, 0f, 100f);
        NotifyFormat(true);
        if (CurrentFormat >= 100f) TriggerDeath(DeathReason.SystemFormatted);
    }

    public void DebugAddTemperature(float delta)
    {
        if (IsPlayerDead) return;
        CurrentTemperature = Mathf.Clamp(CurrentTemperature + delta, config.minTemperature, config.maxTemperature);
        NotifyTemperature(true);
    }

    public void DebugAddVoltage(float delta)
    {
        if (IsPlayerDead) return;
        timeSinceLastDash = 0f; // Pausa el enfriamiento por 3s para que puedas ver el cambio
        CurrentVoltage = Mathf.Clamp(CurrentVoltage + delta, config.minVoltage, config.maxVoltage);
        NotifyVoltage(true);
    }
}