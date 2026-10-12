using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;

    private Rigidbody2D rb;
    private Animator animator;
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
    }

    private void Update()
    {
        // Captura movimiento horizontal (A/D o Flechas)
        moveInput = Input.GetAxisRaw("Horizontal");

        // Orientación visual (Flip del sprite)
        if (moveInput > 0 && !facingRight) Flip();
        else if (moveInput < 0 && facingRight) Flip();

        // Salto con barra espaciadora o flecha arriba solo si está en el suelo
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow)) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        // Envía la velocidad absoluta al Animator para controlar Idle y Run
        animator.SetFloat("Speed", Mathf.Abs(moveInput));
        animator.SetFloat("VerticalSpeed", rb.linearVelocity.y);
        animator.SetBool("IsGrounded", isGrounded);
    }

    private void FixedUpdate()
    {
        // Aplica el movimiento horizontal manteniendo la física vertical
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // Detección de suelo en 2D usando Physics2D.OverlapCircle
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
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