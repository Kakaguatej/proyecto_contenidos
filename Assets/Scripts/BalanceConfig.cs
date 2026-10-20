using UnityEngine;

[CreateAssetMenu(fileName = "BalanceConfig", menuName = "CompuertasDeEscape/Balance Config")]
public class BalanceConfig : ScriptableObject
{
    [Header("1. VIDA (Estabilidad BIT)")]
    public float maxHealth = 100f;
    public float initialHealth = 100f;
    [Tooltip("Curva que multiplica el daño según la temperatura normalizada (0 = 40°C -> 1.0x, 1 = 180°C -> 2.5x)")]
    public AnimationCurve tempDamageMultiplierCurve = new AnimationCurve(
        new Keyframe(0f, 1.0f),
        new Keyframe(1f, 2.5f)
    );
    public float healOnOvenKilled = 4f;
    public float healOnShortCircuitKilled = 3f;
    [Range(0f, 1f)] public float healthWarnThreshold = 0.50f; // 50%
    [Range(0f, 1f)] public float healthCritThreshold = 0.25f; // 25%

    [Header("2. FORMATEO DEL SISTEMA")]
    [Tooltip("Tiempo en segundos para llegar al 100% de formateo (600s = 10 minutos)")]
    public float formatDurationSeconds = 600f;
    [Range(0f, 100f)] public float formatOrangeThreshold = 50f; // Pasa a naranja al 50%

    [Header("3. TEMPERATURA (40°C a 180°C)")]
    public float minTemperature = 40f;
    public float maxTemperature = 180f;
    public float initialTemperature = 40f;
    [Tooltip("Grados que sube por segundo pasivamente")]
    public float tempRisePerSecond = 0.14f;
    [Tooltip("Grados que sube al fallar un puzzle")]
    public float tempPenaltyOnPuzzleFail = 6f;
    [Range(0f, 1f)] public float tempReductionEasy = 0.15f;
    [Range(0f, 1f)] public float tempReductionMedium = 0.25f;
    [Range(0f, 1f)] public float tempReductionHard = 0.40f;
    public float tempWarmThreshold = 100f;
    public float tempHotDangerThreshold = 130f;

    [Header("4. TENSIÓN DE CORRIENTE (0V a 12V)")]
    public float minVoltage = 0f;
    public float maxVoltage = 12f;
    public float initialVoltage = 0f;
    [Tooltip("Voltios que sube cada vez que El Bit hace Dash")]
    public float voltageAddedPerDash = 0.8f;
    [Tooltip("Segundos sin hacer Dash antes de que empiece a bajar la tensión")]
    public float voltageCooldownDelay = 3f;
    [Tooltip("Voltios por segundo que baja tras el tiempo de espera")]
    public float voltageDecayPerSecond = 0.4f;
    public float voltageLowThreshold = 3f;
    public float voltageOverloadThreshold = 9f;

    [Header("5. ESCALONES DE SPAWN: HORNOS (Temperatura >= 50°C)")]
    public SpawnTier[] ovenSpawnTiers = new SpawnTier[]
    {
        new SpawnTier { minThreshold = 50f,  spawnRatePerSecond = 0.15f, maxAlive = 3 },
        new SpawnTier { minThreshold = 100f, spawnRatePerSecond = 0.30f, maxAlive = 5 },
        new SpawnTier { minThreshold = 130f, spawnRatePerSecond = 0.60f, maxAlive = 8 }
    };

    [Header("6. ESCALONES DE SPAWN: CORTOCIRCUITOS (Tensión >= 2V)")]
    public SpawnTier[] shortCircuitSpawnTiers = new SpawnTier[]
    {
        new SpawnTier { minThreshold = 2f, spawnRatePerSecond = 0.20f, maxAlive = 3 },
        new SpawnTier { minThreshold = 6f, spawnRatePerSecond = 0.35f, maxAlive = 5 },
        new SpawnTier { minThreshold = 9f, spawnRatePerSecond = 0.65f, maxAlive = 8 }
    };
}