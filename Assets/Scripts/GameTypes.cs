using System;
using UnityEngine;

// Tipos de enemigos que existen en Compuertas de Escape
public enum EnemyType
{
    OvercurrentOven, // Horno ("Sobre Corriente") -> Regenera +4 de vida
    ShortCircuit     // Cortocircuito -> Regenera +3 de vida
}

// Dificultad de los puzzles de compuertas lógicas
public enum PuzzleDifficulty
{
    Easy,   // Baja 15% del exceso de temperatura
    Medium, // Baja 25% del exceso de temperatura
    Hard    // Baja 40% del exceso de temperatura
}

// Razón por la cual El Bit muere
public enum DeathReason
{
    ZeroHealth,      // Estabilidad BIT llegó a 0
    SystemFormatted  // Formateo del sistema llegó al 100%
}

// Estructura configurable para cada escalón de aparición de enemigos
[Serializable]
public struct SpawnTier
{
    [Tooltip("Valor mínimo de Temperatura (°C) o Tensión (V) para activar este escalón")]
    public float minThreshold;

    [Tooltip("Cuántos enemigos aparecen por segundo en este escalón")]
    public float spawnRatePerSecond;

    [Tooltip("Máximo de enemigos de este tipo vivos al mismo tiempo")]
    public int maxAlive;
}