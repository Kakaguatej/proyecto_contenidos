using UnityEngine;

public class GameDebugPanel : MonoBehaviour
{
    [Header("Configuración del Panel de Pruebas")]
    [Tooltip("Tecla para mostrar u ocultar el panel en pantalla durante Play Mode")]
    [SerializeField] private KeyCode toggleGuiKey = KeyCode.F1;
    [SerializeField] private bool showDebugOverlay = true;

    [Header("Incrementos por Pulsación")]
    [SerializeField] private float healthStep = 15f;
    [SerializeField] private float formatStep = 10f;
    [SerializeField] private float tempStep = 15f;
    [SerializeField] private float voltageStep = 1.5f;

    private void Update()
    {
        if (Input.GetKeyDown(toggleGuiKey))
        {
            showDebugOverlay = !showDebugOverlay;
        }

        GameState gs = GameState.Instance;
        if (gs == null || gs.Config == null) return;

        // 1 y 2: Bajar / Subir VIDA (Simula Daño Base 15 o Curación Directa)
        if (Input.GetKeyDown(KeyCode.Alpha1)) gs.DamagePlayer(healthStep);
        if (Input.GetKeyDown(KeyCode.Alpha2)) gs.DebugAddHealth(healthStep);

        // 3 y 4: Bajar / Subir FORMATEO DEL SISTEMA (-10% / +10%)
        if (Input.GetKeyDown(KeyCode.Alpha3)) gs.DebugAddFormat(-formatStep);
        if (Input.GetKeyDown(KeyCode.Alpha4)) gs.DebugAddFormat(formatStep);

        // 5 y 6: Bajar / Subir TEMPERATURA (-15°C / +15°C)
        if (Input.GetKeyDown(KeyCode.Alpha5)) gs.DebugAddTemperature(-tempStep);
        if (Input.GetKeyDown(KeyCode.Alpha6)) gs.DebugAddTemperature(tempStep);

        // 7 y 8: Bajar / Subir TENSIÓN (-1.5V / +1.5V)
        if (Input.GetKeyDown(KeyCode.Alpha7)) gs.DebugAddVoltage(-voltageStep);
        if (Input.GetKeyDown(KeyCode.Alpha8)) gs.DebugAddVoltage(voltageStep);

        // 9: Simular DASH (+0.8V por defecto)
        if (Input.GetKeyDown(KeyCode.Alpha9)) gs.OnPlayerDashed();

        // 0: Simular PUZZLE FALLADO (+6°C)
        if (Input.GetKeyDown(KeyCode.Alpha0)) gs.OnPuzzleResult(false, PuzzleDifficulty.Easy);

        // F2, F3, F4: Simular PUZZLE GANADO (Fácil 15%, Medio 25%, Difícil 40%)
        if (Input.GetKeyDown(KeyCode.F2)) gs.OnPuzzleResult(true, PuzzleDifficulty.Easy);
        if (Input.GetKeyDown(KeyCode.F3)) gs.OnPuzzleResult(true, PuzzleDifficulty.Medium);
        if (Input.GetKeyDown(KeyCode.F4)) gs.OnPuzzleResult(true, PuzzleDifficulty.Hard);

        // F5 y F6: Simular MUERTE DE ENEMIGO (+4 Vida Horno / +3 Vida Cortocircuito)
        if (Input.GetKeyDown(KeyCode.F5)) gs.OnEnemyKilled(EnemyType.OvercurrentOven);
        if (Input.GetKeyDown(KeyCode.F6)) gs.OnEnemyKilled(EnemyType.ShortCircuit);

        // R: Reiniciar todas las variables si El Bit muere en la prueba
        if (Input.GetKeyDown(KeyCode.R)) gs.ResetState();
    }

    private void OnGUI()
    {
        if (!showDebugOverlay || GameState.Instance == null || GameState.Instance.Config == null) return;

        GameState gs = GameState.Instance;
        GUI.Box(new Rect(15, Screen.height - 245, 430, 230), "PANEL DE DEPURACIÓN - COMPUERTAS DE ESCAPE (F1 Ocultar)");

        int y = Screen.height - 218;
        int line = 18;

        GUI.Label(new Rect(25, y, 410, 20), $"Estado: {(gs.IsPlayerDead ? "MUERTO (Presiona R para reiniciar)" : "ACTIVO")}"); y += line;
        GUI.Label(new Rect(25, y, 410, 20), $"[1 / 2] Vida: {gs.CurrentHealth:0.0} / {gs.Config.maxHealth} (Daño real escala con Temp)"); y += line;
        GUI.Label(new Rect(25, y, 410, 20), $"[3 / 4] Formateo: {gs.CurrentFormat:0.0}% (-10% / +10%)"); y += line;
        GUI.Label(new Rect(25, y, 410, 20), $"[5 / 6] Temperatura: {gs.CurrentTemperature:0.0}°C (-15°C / +15°C)"); y += line;
        GUI.Label(new Rect(25, y, 410, 20), $"[7 / 8] Tensión: {gs.CurrentVoltage:0.00}V (-1.5V / +1.5V) | [9] Simular Dash"); y += line;
        GUI.Label(new Rect(25, y, 410, 20), $"[0] Fallar Puzzle (+6°C) | [F2/F3/F4] Ganar Puzzle (Fácil/Medio/Difícil)"); y += line;
        GUI.Label(new Rect(25, y, 410, 20), $"[F5] Matar Horno (+4 HP) | [F6] Matar Cortocircuito (+3 HP)"); y += line + 4;

        // Botones clicables rápidos en pantalla por si prefieres usar el ratón
        if (GUI.Button(new Rect(25, y, 95, 24), "Daño (1)")) gs.DamagePlayer(healthStep);
        if (GUI.Button(new Rect(125, y, 95, 24), "Temp +15° (6)")) gs.DebugAddTemperature(tempStep);
        if (GUI.Button(new Rect(225, y, 95, 24), "Puzzle OK (F3)")) gs.OnPuzzleResult(true, PuzzleDifficulty.Medium);
        if (GUI.Button(new Rect(325, y, 105, 24), "Reiniciar (R)")) gs.ResetState();
    }
}