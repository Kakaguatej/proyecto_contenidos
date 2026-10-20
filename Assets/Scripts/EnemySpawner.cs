using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs de Enemigos")]
    [SerializeField] private GameObject ovenEnemyPrefab;         // Horno ("Sobre Corriente")
    [SerializeField] private GameObject shortCircuitEnemyPrefab; // Cortocircuito

    [Header("Puntos de Aparición en la Escena")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Camera mainCamera;
    [Tooltip("Margen extra para asegurar que nazcan fuera de la vista de la cámara")]
    [SerializeField] private float offscreenMargin = 0.08f;

    // Listas para rastrear cuántos enemigos de cada tipo siguen vivos
    private readonly List<GameObject> aliveOvens = new List<GameObject>();
    private readonly List<GameObject> aliveShortCircuits = new List<GameObject>();

    // Escalón activo actual (calculado solo cuando cambia la Temperatura o Tensión por evento)
    private float currentOvenRate = 0f;
    private int currentOvenMaxAlive = 0;

    private float currentShortCircuitRate = 0f;
    private int currentShortCircuitMaxAlive = 0;

    private float ovenTimer = 0f;
    private float shortCircuitTimer = 0f;

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        if (GameState.Instance != null)
        {
            GameState.Instance.OnTemperatureChanged += EvaluateOvenTier;
            GameState.Instance.OnVoltageChanged += EvaluateShortCircuitTier;

            // Evaluar estado inicial
            EvaluateOvenTier(GameState.Instance.CurrentTemperature, GameState.Instance.GetNormalizedTemperature());
            EvaluateShortCircuitTier(GameState.Instance.CurrentVoltage, GameState.Instance.GetNormalizedVoltage());
        }
    }

    private void OnDestroy()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.OnTemperatureChanged -= EvaluateOvenTier;
            GameState.Instance.OnVoltageChanged -= EvaluateShortCircuitTier;
        }
    }

    private void Update()
    {
        if (GameState.Instance == null || GameState.Instance.IsPlayerDead) return;

        // 1. Temporizador de Hornos (Temperatura)
        if (currentOvenRate > 0f && ovenEnemyPrefab != null)
        {
            ovenTimer += Time.deltaTime;
            float interval = 1f / currentOvenRate;
            if (ovenTimer >= interval)
            {
                ovenTimer = 0f;
                TrySpawnEnemy(ovenEnemyPrefab, aliveOvens, currentOvenMaxAlive);
            }
        }

        // 2. Temporizador de Cortocircuitos (Tensión)
        if (currentShortCircuitRate > 0f && shortCircuitEnemyPrefab != null)
        {
            shortCircuitTimer += Time.deltaTime;
            float interval = 1f / currentShortCircuitRate;
            if (shortCircuitTimer >= interval)
            {
                shortCircuitTimer = 0f;
                TrySpawnEnemy(shortCircuitEnemyPrefab, aliveShortCircuits, currentShortCircuitMaxAlive);
            }
        }
    }

    private void EvaluateOvenTier(float currentTemp, float _)
    {
        EvaluateTier(currentTemp, GameState.Instance.Config.ovenSpawnTiers, out currentOvenRate, out currentOvenMaxAlive);
    }

    private void EvaluateShortCircuitTier(float currentVolt, float _)
    {
        EvaluateTier(currentVolt, GameState.Instance.Config.shortCircuitSpawnTiers, out currentShortCircuitRate, out currentShortCircuitMaxAlive);
    }

    private void EvaluateTier(float value, SpawnTier[] tiers, out float rate, out int maxAlive)
    {
        rate = 0f;
        maxAlive = 0;
        if (tiers == null) return;

        for (int i = 0; i < tiers.Length; i++)
        {
            if (value >= tiers[i].minThreshold)
            {
                rate = tiers[i].spawnRatePerSecond;
                maxAlive = tiers[i].maxAlive;
            }
        }
    }

    private void TrySpawnEnemy(GameObject prefab, List<GameObject> aliveList, int maxAllowed)
    {
        // Limpiar referencias de enemigos que ya fueron destruidos por El Bit
        aliveList.RemoveAll(item => item == null);

        if (aliveList.Count >= maxAllowed) return;

        Transform chosenPoint = GetOffscreenSpawnPoint();
        if (chosenPoint == null) return;

        GameObject newEnemy = Instantiate(prefab, chosenPoint.position, Quaternion.identity);
        aliveList.Add(newEnemy);
    }

    private Transform GetOffscreenSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        List<Transform> validPoints = new List<Transform>();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform pt = spawnPoints[i];
            if (pt == null) continue;

            if (IsPointOutsideCamera(pt.position))
            {
                validPoints.Add(pt);
            }
        }

        // Si todos los puntos están en cámara, usa cualquiera por seguridad; si no, elige uno fuera de vista al azar
        if (validPoints.Count > 0)
        {
            return validPoints[Random.Range(0, validPoints.Count)];
        }
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }

    private bool IsPointOutsideCamera(Vector3 worldPos)
    {
        if (mainCamera == null) return true;
        Vector3 vp = mainCamera.WorldToViewportPoint(worldPos);
        return vp.x < -offscreenMargin || vp.x > (1f + offscreenMargin) || vp.y < -offscreenMargin || vp.y > (1f + offscreenMargin);
    }
}