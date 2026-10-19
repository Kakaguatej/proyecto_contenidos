using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Configuración de Dash")]
    [SerializeField] private float dashSpeed = 38f;
    [SerializeField] private float dashDuration = 0.28f;
    [SerializeField] private float dashCooldown = 0.8f;
    [SerializeField] private LayerMask capasADestruir; // Capas de Enemy y Proyectiles
    private bool canDash = true;
    public bool isDashing { get; private set; } = false;

    [Header("Efecto Visual de Dash")]
    [SerializeField] private GameObject dashEffectPrefab;
    [SerializeField] private Transform dashPoint;

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
        if (isDashing)
        {
            // Barrido activo permanente mientras dura el Dash para no perder ningún impacto
            EscanearYDestruirEnemigos();
            return;
        }

        moveInput = Input.GetAxisRaw("Horizontal");

        if (moveInput > 0 && !facingRight) Flip();
        else if (moveInput < 0 && facingRight) Flip();

        // Salto con W o Flecha Arriba
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Dash con LeftShift
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

        // Limpieza inmediata en el punto de arranque
        EscanearYDestruirEnemigos();

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = gravedadOriginal;
        rb.linearVelocity = Vector2.zero;
        isDashing = false;

        if (animator != null) animator.SetBool("isDashing", false);

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // Barrido volumétrico frontal: detecta todo lo que esté en su caja de impacto cada frame
    private void EscanearYDestruirEnemigos()
    {
        Vector2 centro = playerCollider != null ? (Vector2)playerCollider.bounds.center : (Vector2)transform.position;
        Vector2 tamano = playerCollider != null ? (Vector2)playerCollider.bounds.size * 1.3f : new Vector2(1.5f, 2f);

        // Busca todos los colliders en el área del cuerpo expandida
        Collider2D[] contactos = Physics2D.OverlapBoxAll(centro, tamano, 0f);

        foreach (Collider2D col in contactos)
        {
            if (col.gameObject == gameObject) continue;

            if (col.CompareTag("Enemy") || col.CompareTag("EnemyProjectile"))
            {
                Debug.Log($"¡Destrucción garantizada por Dash: {col.gameObject.name}!");
                Destroy(col.gameObject);
            }
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
        if (obj.CompareTag("Enemy") || obj.CompareTag("EnemyProjectile"))
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