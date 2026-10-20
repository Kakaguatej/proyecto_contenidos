using UnityEngine;

public class ProyectilEnemigo : MonoBehaviour
{
    [Header("Configuración del Proyectil")]
    public float velocidad = 7f;
    public float tiempoDeVida = 3f;
    public string tagPared = "Floor";
    public string tagEscudo = "Shield";

    [Header("Daño al Jugador (GameState)")]
    public float danoBase = 10f;
    public bool esCritico = false;

    private int direccion = 1;

    public void Inicializar(int dir)
    {
        direccion = dir;

        Vector3 escala = transform.localScale;
        escala.x = Mathf.Abs(escala.x) * direccion;
        transform.localScale = escala;

        Destroy(gameObject, tiempoDeVida);
    }

    void Update()
    {
        transform.Translate(Vector2.right * direccion * velocidad * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        // 1. SI CHOCA CONTRA EL ESCUDO
        if (otro.CompareTag(tagEscudo))
        {
            PlayerShield escudo = otro.GetComponent<PlayerShield>();
            if (escudo == null) escudo = otro.GetComponentInParent<PlayerShield>();

            if (escudo != null && escudo.isShieldUp)
            {
                escudo.OnHit(esCritico);
                Destroy(gameObject);
                return;
            }
        }

        // 2. SI CHOCA CONTRA EL JUGADOR (Sin escudo)
        if (otro.CompareTag("Player"))
        {
            PlayerController pc = otro.GetComponent<PlayerController>();
            // Si el jugador está en medio de un Dash, el Dash destruye la bala sin hacerle daño
            if (pc != null && pc.isDashing)
            {
                Destroy(gameObject);
                return;
            }

            if (GameState.Instance != null)
            {
                GameState.Instance.DamagePlayer(danoBase);
            }
            Destroy(gameObject);
        }
        // 3. SI CHOCA CONTRA EL SUELO O PARED
        else if (otro.CompareTag(tagPared)) 
        {
            Destroy(gameObject);
        }
    }
}