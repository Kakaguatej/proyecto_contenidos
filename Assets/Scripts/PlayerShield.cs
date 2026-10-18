using UnityEngine;

public class PlayerShield : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator anim;
    [SerializeField] private Collider2D shieldCollider;

    [Header("Control de Escudo")]
    [SerializeField] private KeyCode shieldKey = KeyCode.Space; // Activación exclusiva con ESPACIO
    public bool isShieldUp { get; private set; }

    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        if (shieldCollider == null) shieldCollider = GetComponent<Collider2D>();

        if (shieldCollider != null) shieldCollider.enabled = false;
    }

    private void Update()
    {
        // Mantiene el escudo levantado mientras presiones ESPACIO
        if (Input.GetKeyDown(shieldKey))
        {
            ActivateShield();
        }
        else if (Input.GetKeyUp(shieldKey))
        {
            DeactivateShield();
        }
    }

    public void ActivateShield()
    {
        isShieldUp = true;
        if (shieldCollider != null) shieldCollider.enabled = true;
        if (anim != null) anim.SetBool("isActive", true);
    }

    public void DeactivateShield()
    {
        isShieldUp = false;
        if (shieldCollider != null) shieldCollider.enabled = false;
        if (anim != null) anim.SetBool("isActive", false);
    }

    public void OnHit(bool isCritical)
    {
        if (anim == null) return;

        if (isCritical)
        {
            anim.SetTrigger("HitCritical");
        }
        else
        {
            anim.SetTrigger("HitNormal");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Detecta proyectiles enemigos para bloquearlos
        if (collision.CompareTag("EnemyProjectile"))
        {
            bool critical = collision.name.Contains("Heavy");
            OnHit(critical);
            Destroy(collision.gameObject);
        }
    }
}