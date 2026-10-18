using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Configuración de Dash")]
    [SerializeField] private float dashSpeed = 38f;          // Velocidad del desplazamiento
    [SerializeField] private float dashDuration = 0.28f;      // Duración del impulso
    [SerializeField] private float dashCooldown = 0.8f;       // Tiempo de espera entre dashes
    private bool canDash = true;
    public bool isDashing { get; private set; } = false;

    [Header("Efecto Visual de Dash")]
    [SerializeField] private GameObject dashEffectPrefab;    // Prefab DashFX
    [SerializeField] private Transform dashPoint;            // Punto DashPoint

    [Header("Modificadores de Escudo")]
    [Range(0f, 1f)] [SerializeField] private float shieldSpeedMultiplier = 0.5f;

    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D playerCollider;
    private PlayerShield playerShield;
    private float moveInput;
    private bool facingRight = true;
    private bool isGrounded;

    [Header("Detección de Suelo")]
    public Transform groundCheck;
    public float groundRadius = 0.2f;
    public LayerMask groundLayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<Collider2D>();
        playerShield = GetComponentInChildren<PlayerShield>();
    }

    private void Update()
    {
        if (isDashing) return;

        moveInput = Input.GetAxisRaw("Horizontal");

        if (moveInput > 0 && !facingRight) Flip();
        else if (moveInput < 0 && facingRight) Flip();

        // SALTO: Exclusivo con W o Flecha Arriba
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // DASH: Activación con LeftShift
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

    private IEnumerator RealizarDash()
    {
        canDash = false;
        isDashing = true;

        // 1. Convertir temporalmente el collider de El Bit en Trigger
        // Esto elimina cualquier freno físico contra enemigos o proyectiles y le permite traspasarlos libremente
        bool eraTrigger = playerCollider != null && playerCollider.isTrigger;
        if (playerCollider != null) playerCollider.isTrigger = true;

        // 2. Crear el efecto visual detrás de El Bit
        if (dashEffectPrefab != null)
        {
            Vector3 pos = (dashPoint != null) ? dashPoint.position : transform.position;
            GameObject fx = Instantiate(dashEffectPrefab, pos, Quaternion.identity);

            Vector3 fxScale = fx.transform.localScale;
            fxScale.x = Mathf.Abs(fxScale.x) * (facingRight ? 1f : -1f);
            fx.transform.localScale = fxScale;
        }

        // 3. Activar animación de Dash en Animator
        if (animator != null) animator.SetBool("isDashing", true);

        // 4. Apagar gravedad temporal para un desplazamiento recto
        float gravedadOriginal = rb.gravityScale;
        rb.gravityScale = 0f;

        // 5. Aplicar velocidad en la dirección hacia donde mira
        float direccion = facingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direccion * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        // 6. Frenar y restaurar físicas normales
        rb.gravityScale = gravedadOriginal;
        rb.linearVelocity = Vector2.zero;
        isDashing = false;

        // 7. Restaurar el collider al estado que tenía antes del Dash
        if (playerCollider != null) playerCollider.isTrigger = eraTrigger;

        if (animator != null) animator.SetBool("isDashing", false);

        // 8. Cooldown antes de poder usarlo de nuevo
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // --- DETECCIÓN Y DESTRUCCIÓN TOTAL DURANTE EL DASH ---

    private void OnTriggerEnter2D(Collider2D collision)
    {
        ProcesarContactoDash(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        ProcesarContactoDash(collision.gameObject);
    }

    private void ProcesarContactoDash(GameObject objetivo)
    {
        if (isDashing)
        {
            // Si el objeto tocado es un enemigo
            if (objetivo.CompareTag("Enemy"))
            {
                Debug.Log($"¡Enemigo {objetivo.name} destruido con el Dash!");
                Destroy(objetivo);
            }
            // Si el objeto tocado es un proyectil enemigo
            else if (objetivo.CompareTag("EnemyProjectile"))
            {
                Debug.Log($"¡Proyectil {objetivo.name} destruido en el Dash!");
                Destroy(objetivo);
            }
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