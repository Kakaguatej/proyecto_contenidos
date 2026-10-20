using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // Necesario para volver a la escena del MainMenu

public class PlayerController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Configuración de Dash")]
    [SerializeField] private float dashSpeed = 38f;
    [SerializeField] private float dashDuration = 0.28f;
    [SerializeField] private float dashCooldown = 0.8f;
    [SerializeField] private LayerMask capasADestruir;
    private bool canDash = true;
    public bool isDashing { get; private set; } = false;

    [Header("Efecto Visual de Dash")]
    [SerializeField] private GameObject dashEffectPrefab;
    [SerializeField] private Transform dashPoint;

    [Header("Modificadores de Escudo")]
    [Range(0f, 1f)] [SerializeField] private float shieldSpeedMultiplier = 0.5f;

    [Header("Detección de Suelo")]
    public Transform groundCheck;
    public float groundRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Muerte y Cambio de Escena")]
    [Tooltip("Nombre del parámetro Trigger en el Animator para la animación de muerte")]
    [SerializeField] private string deathTriggerName = "Die";
    [Tooltip("Segundos que espera viendo la animación de muerte antes de ir al menú")]
    [SerializeField] private float delayBeforeMenu = 2.0f;
    [Tooltip("Nombre EXACTO de tu escena de menú o hub en la carpeta Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D playerCollider;
    private PlayerShield playerShield;
    private float moveInput;
    private bool facingRight = true;
    private bool isGrounded;
    private bool deathSequenceStarted = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<Collider2D>();
        playerShield = GetComponentInChildren<PlayerShield>();
    }

    private void Start()
    {
        // Se suscribe al evento de muerte del GameState
        if (GameState.Instance != null)
        {
            GameState.Instance.OnPlayerDied += HandlePlayerDied;
        }
    }

    private void OnDestroy()
    {
        if (GameState.Instance != null)
        {
            GameState.Instance.OnPlayerDied -= HandlePlayerDied;
        }
    }

    private void Update()
    {
        // Si ya murió, bloquea cualquier tecla o movimiento
        if (deathSequenceStarted || (GameState.Instance != null && GameState.Instance.IsPlayerDead))
        {
            return;
        }

        if (isDashing)
        {
            EscanearYDestruirEnemigos();
            return;
        }

        moveInput = Input.GetAxisRaw("Horizontal");

        if (moveInput > 0 && !facingRight) Flip();
        else if (moveInput < 0 && facingRight) Flip();

        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && (playerShield == null || !playerShield.isShieldUp))
        {
            StartCoroutine(RealizarDash());
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(moveInput));
            animator.SetFloat("VerticalSpeed", rb.linearVelocity.y);
            animator.SetBool("IsGrounded", isGrounded);
        }
    }

    private void FixedUpdate()
    {
        if (deathSequenceStarted || (GameState.Instance != null && GameState.Instance.IsPlayerDead)) return;
        if (isDashing) return;

        float currentSpeed = moveSpeed;
        if (playerShield != null && playerShield.isShieldUp)
        {
            currentSpeed *= shieldSpeedMultiplier;
        }

        rb.linearVelocity = new Vector2(moveInput * currentSpeed, rb.linearVelocity.y);

        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        }
    }

    // Se ejecuta automáticamente cuando GameState anuncia que la vida llegó a 0 o el Formateo al 100%
    private void HandlePlayerDied(DeathReason reason)
    {
        if (deathSequenceStarted) return;
        deathSequenceStarted = true;

        StartCoroutine(SecuenciaDeMuerte());
    }

    private IEnumerator SecuenciaDeMuerte()
    {
        // 1. Si estaba haciendo Dash o con el escudo, lo cancelamos y frenamos a El Bit
        isDashing = false;
        if (playerShield != null && playerShield.isShieldUp)
        {
            playerShield.DeactivateShield();
        }

        rb.linearVelocity = Vector2.zero;

        // 2. Lanzar la animación de muerte en el Animator
        if (animator != null && !string.IsNullOrEmpty(deathTriggerName))
        {
            animator.SetBool("isDashing", false);
            animator.SetFloat("Speed", 0f);
            animator.SetTrigger(deathTriggerName);
        }

        // 3. Esperar a que termine de verse la animación de muerte
        yield return new WaitForSeconds(delayBeforeMenu);

        // 4. Cargar la escena del MainMenu / Hub
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator RealizarDash()
    {
        canDash = false;
        isDashing = true;

        if (GameState.Instance != null)
        {
            GameState.Instance.OnPlayerDashed();
        }

        if (dashEffectPrefab != null)
        {
            Vector3 pos = (dashPoint != null) ? dashPoint.position : transform.position;
            GameObject fx = Instantiate(dashEffectPrefab, pos, Quaternion.identity);

            Vector3 fxScale = fx.transform.localScale;
            fxScale.x = Mathf.Abs(fxScale.x) * (facingRight ? 1f : -1f);
            fx.transform.localScale = fxScale;
        }

        if (animator != null) animator.SetBool("isDashing", true);

        float gravedadOriginal = rb.gravityScale;
        rb.gravityScale = 0f;

        float direccion = facingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direccion * dashSpeed, 0f);

        EscanearYDestruirEnemigos();

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = gravedadOriginal;
        rb.linearVelocity = Vector2.zero;
        isDashing = false;

        if (animator != null) animator.SetBool("isDashing", false);

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void EscanearYDestruirEnemigos()
    {
        Vector2 centro = playerCollider != null ? (Vector2)playerCollider.bounds.center : (Vector2)transform.position;
        Vector2 tamano = playerCollider != null ? (Vector2)playerCollider.bounds.size * 1.3f : new Vector2(1.5f, 2f);

        Collider2D[] contactos = Physics2D.OverlapBoxAll(centro, tamano, 0f);

        foreach (Collider2D col in contactos)
        {
            if (col.gameObject == gameObject) continue;
            ProcesarContacto(col.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDashing) ProcesarContacto(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDashing) ProcesarContacto(collision.gameObject);
    }

    private void ProcesarContacto(GameObject obj)
    {
        if (obj.CompareTag("Enemy"))
        {
            Enemigo scriptEnemigo = obj.GetComponent<Enemigo>();
            if (scriptEnemigo != null)
            {
                scriptEnemigo.MorirPorDash();
            }
            else
            {
                Destroy(obj);
            }
        }
        else if (obj.CompareTag("EnemyProjectile"))
        {
            Destroy(obj);
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
    }
}