using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private bool isTopDown = false; // Desactivado para plataformas (se mantiene en false)

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private bool facingRight = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // 1. Captura de entradas del teclado (A/D o Flechas)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = isTopDown ? Input.GetAxisRaw("Vertical") : 0f;
        moveInput = new Vector2(horizontal, vertical).normalized;

        // 2. Control de orientación visual (Flip del Sprite)
        if (horizontal > 0 && !facingRight) Flip();
        else if (horizontal < 0 && facingRight) Flip();

        // 3. Envío de datos de velocidad al Animator
        float currentSpeed = moveInput.sqrMagnitude;
        if (animator != null)
        {
            animator.SetFloat("Speed", currentSpeed);
        }
    }

    private void FixedUpdate()
    {
        // Movimiento por físicas en el ciclo FixedUpdate
        if (isTopDown)
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }
        else
        {
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}