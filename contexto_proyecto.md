# Contexto del Proyecto — Compuertas de Escape ("El Bit")

> Documento de contexto generado a partir de una inspección de solo lectura del proyecto.
> Fecha de análisis: 2026-10-31.

---

## 1. Resumen del Proyecto

| Dato | Valor |
|---|---|
| Versión de Unity | **6000.6.3f1** (`ProjectSettings/ProjectVersion.txt`, revisión `45d8eee7de74`) |
| Nombre de producto | `proyecto_contenidos` |
| Compañía | `BiteRunner` |
| Pipeline de render | **Universal Render Pipeline 2D** (`com.unity.render-pipelines.universal` 17.6.0, `Renderer2D.asset`) |
| Espacio de color | Linear (`m_ActiveColorSpace: 1`) |
| Resolución por defecto | 1920 × 1080 |
| Entrada | `activeInputHandler: 2` (Input System nuevo **y** Input Manager clásico). El código actual usa la API clásica `Input.*` |
| Repositorio | Git, rama `main`, HEAD `2753dfd` ("Expansion del Nivel") |
| Escena activa al analizar | `Assets/Scenes/MainMenuScene.unity` (editor sin Play Mode, sin compilación pendiente) |

**Concepto del juego:** plataformas/runner 2D. El jugador ("El Bit") corre, salta y hace *dash* para destruir enemigos, mientras gestiona cuatro recursos:

1. **Estabilidad BIT** (vida, 0–100).
2. **Formateo del Sistema** (0–100 %, sube con el tiempo; al 100 % muere).
3. **Temperatura** (40–180 °C; sube pasivamente y con puzzles fallados; baja al resolver puzzles).
4. **Tensión de Corriente** (0–12 V; sube al hacer dash y baja tras 3 s sin dash).

La temperatura multiplica el daño recibido y determina la aparición de enemigos "Horno"; la tensión determina la aparición de "Cortocircuito". Los puzzles de compuertas lógicas (AND/OR/NOT) apagan láseres y enfrían la placa.

**Estado actual observado**

- 3 escenas en `Assets/Scenes`; en Build Settings solo están habilitadas `MainMenuScene` (índice 0) y `Level1Scene` (índice 1). `GameScene` está deshabilitada.
- `GameScene` es una escena de pruebas/arenas (cámara, cielo, sprite del jugador) sin scripts propios.
- El proyecto compila y el editor está listo (`ready_for_tools: true`).
- Hay 8 archivos con cambios sin commitear (settings de URP/proyecto, `manifest.json`, `packages-lock.json`, `BalanceConfig_Default.asset`).
- `Audio/` está vacía (solo su `.meta`).
- Existe `Assets/_Recovery/0.unity` (copia de recuperación de una escena tipo menú) y `Assets/qw.cs` (script vacío).

---

## 2. Estructura de Carpetas

```text
Assets/
├── Animations/
│   ├── LaserBarrier.controller          # Controlador del láser
│   ├── Laser_Loop.anim                  # Clip del láser
│   ├── enemigo_cortocircuito/           # 3 clips + 2 controllers (enemigo y proyectil)
│   ├── enemigo_horno/                   # 3 clips + 2 controllers (enemigo y proyectil)
│   ├── menu/                            # 4 clips + 4 controllers de botones
│   ├── player/                          # idle, run, jump, fall, dash, death + Player.controller
│   └── Shield/                          # idle, open, hit normal, hit critical, break + controller
├── Audio/                               # Vacía
├── Materials/
│   └── Parallax.mat
├── Prefabs/
│   ├── Cortocircuito.prefab             # Enemigo tipo ShortCircuit
│   ├── Horno.prefab                     # Enemigo tipo OvercurrentOven
│   ├── ProyectilEnemigo.prefab          # Proyectil del Horno
│   ├── Proyectil_cortocircuito.prefab   # Proyectil del Cortocircuito
│   └── DashFX.prefab                    # Efecto de dash (AutoDestroyEffect)
├── Scenes/
│   ├── MainMenuScene.unity              # Menú principal + créditos (build 0)
│   ├── Level1Scene.unity                # Nivel jugable completo (build 1)
│   └── GameScene.unity                  # Escena de pruebas, deshabilitada en build
├── Scripts/
│   ├── BalanceConfig.cs
│   ├── BalanceConfig_Default.asset      # ScriptableObject con el balance real
│   ├── GameTypes.cs
│   ├── GameState.cs
│   ├── PlayerController.cs
│   ├── PlayerShield.cs
│   ├── Enemigo.cs
│   ├── EnemySpawner.cs
│   ├── ProyectilEnemigo.cs
│   ├── HudBinder.cs
│   ├── MainMenuManager.cs
│   ├── Camara.cs
│   ├── Parallax.cs
│   ├── AutoDestroyEffect.cs
│   ├── GameDebugPanel.cs
│   └── Interaccion/
│       ├── ChipClickable.cs
│       └── LogicSocketController.cs
├── Settings/
│   ├── UniversalRP.asset
│   ├── Renderer2D.asset
│   ├── UniversalRenderPipelineGlobalSettings.asset
│   ├── DefaultVolumeProfile.asset
│   ├── Build Profiles/Windows.asset
│   └── Scenes/URP2DSceneTemplate.unity
├── Sprites/
│   ├── EjercicioCL/                     # chips, socket, láser, iconos y prompts del puzzle
│   ├── enemigos/                        # spritesheets de Horno, Cortocircuito, proyectiles y dash FX
│   ├── fondos_y_imagenes/               # fondos, logo, botones, vídeo del menú, render texture
│   ├── Free Industrial Zone Tileset/    # tileset industrial (1 Tiles … 4 Animated objects)
│   ├── HUD/                             # kits de HUD
│   ├── player/                          # idle, run, caída, salto, muerte y escudo
│   └── Sidescroller Shooter - Central City/  # pack de edificios/props/fondos
├── TextMesh Pro/                        # Recursos y shaders importados de TMP
├── Tilemaps/
│   ├── Nivel1Tile.prefab
│   ├── tileset_pistas.png
│   └── tileset_pistas_0.asset … _37.asset  # 38 tile assets
├── Welcome/                             # Template 2D de Unity (asmdef propio)
├── _Recovery/0.unity                    # Escena de recuperación (menú)
└── qw.cs                                # StateMachineBehaviour vacío
```

**Archivos raíz relevantes:** `Assembly-CSharp.csproj`, `proyecto_contenidos.slnx`, `Packages/manifest.json`, `ProjectSettings/`.

---

## 3. Arquitectura de Código

Los scripts de gameplay viven en `Assembly-CSharp` (no hay `.asmdef` para ellos). Solo `Assets/Welcome/Unity.2D.Welcome.asmdef` define un ensamblado aparte para el template.

### 3.1 Núcleo de estado y balance

| Script | Propósito |
|---|---|
| **`GameState.cs`** | Singleton (`GameState.Instance`). Fuente única de verdad de vida, formateo, temperatura y tensión. En `Update` avanza el formateo (100 % en `formatDurationSeconds`), la temperatura pasiva y el enfriamiento de tensión. Expone eventos y métodos públicos (`DamagePlayer`, `OnEnemyKilled`, `OnPlayerDashed`, `OnPuzzleResult`, `ResetState`) y métodos de debug. |
| **`BalanceConfig.cs`** | `ScriptableObject` con todos los parámetros de balance (vida, umbrales, curva daño/temperatura, tiempos, escalones de spawn). Menú: `CompuertasDeEscape/Balance Config`. |
| **`BalanceConfig_Default.asset`** | Instancia usada por `Level1Scene`. **Sus valores mandan sobre los defaults del script.** |
| **`GameTypes.cs`** | Enums y estructura compartidos: `EnemyType` (`OvercurrentOven`, `ShortCircuit`), `PuzzleDifficulty` (`Easy/Medium/Hard`), `DeathReason` (`ZeroHealth`, `SystemFormatted`) y `SpawnTier` (`minThreshold`, `spawnRatePerSecond`, `maxAlive`). |

**Eventos de `GameState`:** `OnHealthChanged(float,float)`, `OnFormatChanged(float)`, `OnTemperatureChanged(float,float)`, `OnVoltageChanged(float,float)`, `OnPlayerDied(DeathReason)`. El HUD, el spawner y el jugador se suscriben a ellos; no se consultan valores por frame.

### 3.2 Jugador

| Script | Propósito |
|---|---|
| **`PlayerController.cs`** | Movimiento horizontal (`Input.GetAxisRaw("Horizontal")`), salto (W/↑), *dash* (LeftShift, bloqueado si el escudo está activo), animaciones (`Speed`, `VerticalSpeed`, `IsGrounded`, `isDashing`, trigger `Die`), detección de suelo por `OverlapCircle` y destrucción de enemigos/proyectiles durante el dash. Al morir espera `delayBeforeMenu` y carga `mainMenuSceneName` (`MainMenuScene`). |
| **`PlayerShield.cs`** | Escudo mantenido con Espacio. Activa/desactiva `Collider2D` y `Animator`. `OnHit(bool isCritical)` dispara triggers `HitNormal`/`HitCritical`. También destruye proyectiles con tag `EnemyProjectile` (crítico si el nombre contiene `"Heavy"`). |

### 3.3 Enemigos y combate

| Script | Propósito |
|---|---|
| **`Enemigo.cs`** | Patrulla horizontal, gira al detectar pared por `RaycastAll` (tag `Floor`), detecta al jugador por distancia X/Y, se orienta hacia él y dispara cada `tiempoEntreDisparos`. `MorirPorDash()` avisa a `GameState.OnEnemyKilled` y destruye el objeto. Daña al jugador por contacto si este no está en dash. |
| **`EnemySpawner.cs`** | Se suscribe a los eventos de temperatura y tensión para recalcular escalones (`SpawnTier[]`). Instancia Hornos y Cortocircuitos en 5 `spawnPoints`, solo en puntos fuera del frustum de cámara y respetando `maxAlive`. |
| **`ProyectilEnemigo.cs`** | Avanza en línea recta, se destruye por tiempo (`tiempoDeVida`), al impactar suelo/escudo/jugador. Aplica daño base con `GameState.DamagePlayer`. |

### 3.4 Interfaz, cámara y mundo

| Script | Propósito |
|---|---|
| **`HudBinder.cs`** | Se suscribe a los eventos de `GameState`. Gestiona: barras de vida con umbrales OK/Warn/Crit y retratos; barra y texto de formateo (naranja al 50 %); anillo radial de temperatura con tres estados (cool/warm/hot) y textos; marcador de tensión que se desplaza en X y cambia a estado de sobrecarga. Anima con corrutinas. |
| **`MainMenuManager.cs`** | `OpenCredits`/`CloseCredits`, `PlayGame` (carga `Level1Scene`) y `QuitGame`. |
| **`Camara.cs`** | Sigue al jugador en `LateUpdate` con límites opcionales `minX/maxX/minY/maxY`. |
| **`Parallax.cs`** | Desplaza el `mainTextureOffset` del material según el delta X del jugador. Se usa en 4 capas de fondo. |
| **`AutoDestroyEffect.cs`** | Destruye el objeto tras `tiempoDeVida` (usado por `DashFX`). |
| **`GameDebugPanel.cs`** | Panel IMGUI (F1) y atajos de teclado para probar vida, formateo, temperatura, tensión, dash, puzzles y muertes de enemigos. |

### 3.5 Puzzle de compuertas lógicas (`Scripts/Interaccion`)

| Script | Propósito |
|---|---|
| **`LogicSocketController.cs`** | Define también `enum GateType { VACIO, AND, OR, NOT }`. Genera entradas A/B aleatorias, muestra indicadores de bits, abre el selector de chips con E, evalúa la compuerta instalada contra `salidaRequeridaParaApagar`, apaga `laserBarrera` al resolver y llama a `GameState.OnPuzzleResult(true/false, dificultadPuzzle)`. |
| **`ChipClickable.cs`** | Se coloca en cada chip (`Slot_1_AND`, `Slot_2_OR`, `Slot_3_NOT`). Al hacer clic llama al `LogicSocketController` padre con su `GateType`. Efecto de escala al pasar el ratón. |

### 3.6 Template / residuales

| Script | Propósito |
|---|---|
| **`Assets/Welcome/Welcome2DScript.cs`** | Template de Unity 2D (namespace `Unity.U2D.Welcome`), abre una URL. No participa del gameplay. |
| **`Assets/qw.cs`** | `StateMachineBehaviour` vacío, sin uso. |

### 3.7 Relaciones entre sistemas

```text
BalanceConfig_Default.asset
        │ referencia
        ▼
   GameState (singleton, eventos)
   ├── PlayerController ──► OnPlayerDashed / OnPlayerDied
   ├── Enemigo ───────────► OnEnemyKilled / DamagePlayer
   ├── ProyectilEnemigo ──► DamagePlayer
   ├── LogicSocketController ─► OnPuzzleResult
   ├── EnemySpawner ──────► lee escalones de BalanceConfig y eventos Temp/Tensión
   ├── HudBinder ─────────► escucha los 4 eventos de estado
   └── GameDebugPanel ────► llama métodos de debug

MainMenuManager ──► carga Level1Scene
PlayerController ─► al morir carga MainMenuScene
PlayerController ─► dash destruye objetos con tag Enemy / EnemyProjectile
Enemigo ──────────► instancia ProyectilEnemigo / Proyectil_cortocircuito
```

### 3.8 Cableado en escenas y prefabs

**`Level1Scene` (escena jugable):**

- `GameSystems`: `GameState` (con `BalanceConfig_Default`), `EnemySpawner`, `GameDebugPanel`. Hijos: `SpawnPoint_1` … `SpawnPoint_1 (4)`.
- `Player`: `PlayerController`; hijos `ShieldObject` (`PlayerShield`), `GroundCheck`, `DashPoint`.
- `Main Camera`: `Camara` (target = Player, `minX = 0`, sin límites máximos) + `background` con 4 capas `layer_*` con `Parallax`.
- `HUD_Canvas`: `HudBinder` y toda la jerarquía de UI (barras, retratos, anillo de temperatura, marcador de tensión).
- Dos puzzles independientes: `Puzzle_Compuerta_Laser` y `Puzzle_Compuerta_Laser (1)`, cada uno con `LaserBarrier` (emisores + `LaserBeam`) y `LogicSocket` (indicadores A/B/OUT, `ChipSelectorUI` con 3 slots y `PromptInteract`).
- `Grid/Tilemap` con el tileset de pistas; `Fence1_0`, `Fence2_0`, `Fence3_0`, `LimiteIzquierdo`, `EventSystem`.
- Ambos puzzles tienen `entradasAleatorias = true`, `dificultadPuzzle = Easy`, `salidaRequeridaParaApagar = true`.

**Prefabs:**

- `Horno.prefab` → `Enemigo`, tag `Enemy`, `tipoEnemigo = 0` (OvercurrentOven), daño de contacto 8, lanza `ProyectilEnemigo`.
- `Cortocircuito.prefab` → `Enemigo`, tag `Enemy`, `tipoEnemigo = 1` (ShortCircuit), daño de contacto 8, lanza `Proyectil_cortocircuito`.
- `ProyectilEnemigo.prefab` y `Proyectil_cortocircuito.prefab` → `ProyectilEnemigo`, tag `EnemyProjectile`, velocidad 7, vida 3 s, daño 10, `esCritico = false`.
- `DashFX.prefab` → `AutoDestroyEffect`.

**Tags definidos:** `Layer`, `Floor`, `Shield`, `EnemyProjectile`, `Enemy` (más los estándar `Player`, `Untagged`).
**Capas:** `Floor` (índice 3), `Shield` (índice 6). `groundLayer` del jugador apunta a la capa 3 (`m_Bits: 8`).
**Sorting Layer extra:** `jugador`.

### 3.9 Valores de balance realmente configurados

`BalanceConfig_Default.asset` difiere de los defaults del script en varios puntos:

| Parámetro | Valor en asset | Default del script |
|---|---|---|
| `healOnOvenKilled` | **15** | 4 |
| `healOnShortCircuitKilled` | **10** | 3 |
| `voltageAddedPerDash` | **1.5** | 0.8 |
| `maxHealth` / `initialHealth` | 100 / 100 | 100 / 100 |
| `formatDurationSeconds` | 600 s | 600 s |
| `tempRisePerSecond` | 0.14 °C/s | 0.14 °C/s |
| `voltageDecayPerSecond` | 0.4 V/s | 0.4 V/s |

Overrides de escena (`Level1Scene`): `PlayerController` con `moveSpeed 14`, `dashSpeed 22`, `dashDuration 0.2`, `dashCooldown 0.8`, `groundRadius 0.1`, `mainMenuSceneName "MainMenuScene"`.

### 3.10 Observaciones técnicas / deuda pendiente

- `dashEffectPrefab` y `dashPoint` están **sin asignar** en `Level1Scene`, aunque existen `DashFX.prefab` y el hijo `DashPoint`.
- `capasADestruir` está declarado en `PlayerController` pero no se usa.
- El escudo detecta golpe crítico por nombre (`collision.name.Contains("Heavy")`) mientras los proyectiles usan el flag `esCritico` (ambos en `false`). `PlayerShield.OnTriggerEnter2D` y `ProyectilEnemigo.OnTriggerEnter2D` pueden procesar el mismo impacto (doble `OnHit`/`Destroy`).
- `qw.cs` y `Assets/_Recovery/0.unity` son residuales.
- Hay 4 imágenes `*.png~` sin `.meta` (copias de respaldo del pack Central City); Unity las ignora.
- No hay tests ni asmdef de gameplay. El input es legacy aunque el proyecto tenga Input System instalado.

---

## 4. Reglas Críticas de Metadatos (IMPORTANTE)

> **REGLA ESTRICTA DE UNITY: Los archivos `.meta` son críticos. Está estrictamente prohibido mover, renombrar o eliminar scripts, imágenes o assets directamente en el sistema de archivos. Cualquier manipulación de archivos debe solicitarse a través de las herramientas del editor de Unity (vía MCP). Ignorar esto romperá las referencias en los Prefabs y GameObjects.**
