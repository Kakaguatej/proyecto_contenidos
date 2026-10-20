using UnityEngine;

public enum GateType { VACIO, AND, OR, NOT }

public class LogicSocketController : MonoBehaviour
{
    [Header("1. Configuración de Aleatoriedad y Dificultad")]
    [Tooltip("Si está marcado, A y B se generan al azar al iniciar la partida")]
    public bool entradasAleatorias = true;
    [Tooltip("Define cuánta temperatura baja al resolverlo (Easy 15%, Medium 25%, Hard 40%)")]
    public PuzzleDifficulty dificultadPuzzle = PuzzleDifficulty.Easy;

    [Header("Valores de Entrada (Pistas A y B)")]
    public bool inputA = true;
    public bool inputB = false;

    [Header("2. Condición de Salida para Desactivar")]
    [Tooltip("El resultado que apaga el láser (1 lógico)")]
    public bool salidaRequeridaParaApagar = true;

    [Header("3. Referencias de Escena")]
    [SerializeField] private GameObject cartelInteractuar; 
    [SerializeField] private GameObject selectorChipsUI;   
    [SerializeField] private GameObject laserBarrera;      
    [SerializeField] private SpriteRenderer socketSR;      
    [SerializeField] private SpriteRenderer bitIndicatorSR; // Indicador de salida OUT

    [Header("4. Indicadores de Entrada")]
    [SerializeField] private SpriteRenderer bitIndicatorA;
    [SerializeField] private SpriteRenderer bitIndicatorB;

    [Header("5. Sprites del Zócalo (socket_sheet)")]
    [SerializeField] private Sprite spriteVacio; 
    [SerializeField] private Sprite spriteAND;   
    [SerializeField] private Sprite spriteOR;    
    [SerializeField] private Sprite spriteNOT;   

    [Header("6. Sprites de Números (ui_icons)")]
    [SerializeField] private Sprite spriteBitCero; // [0] Rojo
    [SerializeField] private Sprite spriteBitUno;  // [1] Verde

    private GateType compuertaInstalada = GateType.VACIO;
    private bool jugadorCerca = false;
    private bool menuAbierto = false;
    private bool resuelto = false;

    private void Start()
    {
        if (cartelInteractuar != null) cartelInteractuar.SetActive(false);
        if (selectorChipsUI != null) selectorChipsUI.SetActive(false);

        if (entradasAleatorias)
        {
            GenerarEntradasValidas();
        }

        if (socketSR != null && spriteVacio != null) socketSR.sprite = spriteVacio;
        if (bitIndicatorSR != null && spriteBitCero != null) bitIndicatorSR.sprite = spriteBitCero;

        ActualizarVisualEntradas();
    }

    private void GenerarEntradasValidas()
    {
        bool esValido = false;

        while (!esValido)
        {
            inputA = Random.value > 0.5f;
            inputB = Random.value > 0.5f;

            bool puedeAND = inputA && inputB;
            bool puedeOR  = inputA || inputB;
            bool puedeNOT = !inputA;

            if (puedeAND || puedeOR || puedeNOT)
            {
                esValido = true;
            }
        }

        Debug.Log($"[Puzzle Lógico] Entradas generadas al azar -> A: {(inputA ? 1 : 0)}, B: {(inputB ? 1 : 0)}");
    }

    private void ActualizarVisualEntradas()
    {
        if (bitIndicatorA != null) bitIndicatorA.sprite = inputA ? spriteBitUno : spriteBitCero;
        if (bitIndicatorB != null) bitIndicatorB.sprite = inputB ? spriteBitUno : spriteBitCero;
    }

    private void Update()
    {
        if (resuelto) return;

        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            menuAbierto = !menuAbierto;
            if (selectorChipsUI != null) selectorChipsUI.SetActive(menuAbierto);
            if (cartelInteractuar != null) cartelInteractuar.SetActive(!menuAbierto);
        }
    }

    public void SeleccionarChipConMouse(GateType tipo)
    {
        if (!menuAbierto || resuelto) return;

        Sprite spriteElegido = spriteVacio;

        switch (tipo)
        {
            case GateType.AND:
                spriteElegido = spriteAND;
                break;
            case GateType.OR:
                spriteElegido = spriteOR;
                break;
            case GateType.NOT:
                spriteElegido = spriteNOT;
                break;
        }

        ColocarCompuerta(tipo, spriteElegido);
    }

    private void ColocarCompuerta(GateType tipo, Sprite spriteChip)
    {
        compuertaInstalada = tipo;
        if (socketSR != null) socketSR.sprite = spriteChip;

        menuAbierto = false;
        if (selectorChipsUI != null) selectorChipsUI.SetActive(false);

        EvaluarCircuito();
    }

    private void EvaluarCircuito()
    {
        bool salidaCalculada = false;

        switch (compuertaInstalada)
        {
            case GateType.AND:
                salidaCalculada = inputA && inputB;
                break;
            case GateType.OR:
                salidaCalculada = inputA || inputB;
                break;
            case GateType.NOT:
                salidaCalculada = !inputA;
                break;
            default:
                salidaCalculada = false;
                break;
        }

        if (bitIndicatorSR != null)
        {
            bitIndicatorSR.sprite = salidaCalculada ? spriteBitUno : spriteBitCero;
        }

        if (salidaCalculada == salidaRequeridaParaApagar)
        {
            ResolverPuzzle();
        }
        else
        {
            // CONEXIÓN GAMESTATE: Falló el puzzle -> Sube +6°C de temperatura
            if (GameState.Instance != null)
            {
                GameState.Instance.OnPuzzleResult(false, dificultadPuzzle);
            }

            Debug.Log($"Salida calculada: {salidaCalculada}. Láser continúa activo (+6°C).");
            if (cartelInteractuar != null) cartelInteractuar.SetActive(true);
        }
    }

    private void ResolverPuzzle()
    {
        resuelto = true;

        // CONEXIÓN GAMESTATE: Completó el puzzle -> Enfría la placa según dificultad
        if (GameState.Instance != null)
        {
            GameState.Instance.OnPuzzleResult(true, dificultadPuzzle);
        }

        Debug.Log("¡Circuito resuelto! Salida en 1. Láser apagado y temperatura reducida.");

        if (laserBarrera != null) laserBarrera.SetActive(false);
        if (cartelInteractuar != null) cartelInteractuar.SetActive(false);
        if (selectorChipsUI != null) selectorChipsUI.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !resuelto)
        {
            jugadorCerca = true;
            if (cartelInteractuar != null && !menuAbierto) cartelInteractuar.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            jugadorCerca = false;
            menuAbierto = false;
            if (cartelInteractuar != null) cartelInteractuar.SetActive(false);
            if (selectorChipsUI != null) selectorChipsUI.SetActive(false);
        }
    }
}