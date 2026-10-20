using UnityEngine;

public class Enemigo : MonoBehaviour
{
    [Header("1. Movimiento y Patrulla")]
    public float velocidad = 2f;
    public Transform detectorPared;
    public float distanciaPared = 0.8f;
    public string tagPared = "Floor";

    [Header("2. Detección y Disparo")]
    public Transform puntoDisparo;
    public GameObject prefabProyectil;
    public float distanciaDeteccion = 6f;
    public float margenAltura = 1.5f;
    public float tiempoEntreDisparos = 1.5f;
    
    [Header("3. Animaciones")]
    public string nombreTriggerAtaque = "Atacar";
    public string nombreEstadoDisparo = "Disparo";

    [Header("4. Tipo de Enemigo y Daño (GameState)")]
    public EnemyType tipoEnemigo = EnemyType.OvercurrentOven;
    public float danoContactoBase = 8f;

    private Rigidbody2D rb;
    private Animator anim;
    private Transform player;
    private int direccion = 1; 
    private float cronometroDisparo;
    private bool yaMurio = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        if (detectorPared == null) detectorPared = transform;
        if (puntoDisparo == null) puntoDisparo = transform;

        GameObject objPlayer = GameObject.FindGameObjectWithTag("Player");
        if (objPlayer != null)
        {
            player = objPlayer.transform;
        }
    }

    void Update()
    {
        if (JugadorCerca())
        {
            MirarHaciaElJugador();

            cronometroDisparo -= Time.deltaTime;
            if (cronometroDisparo <= 0f)
            {
                Disparar();
                cronometroDisparo = tiempoEntreDisparos;
            }
        }
        else if (DetectaParedEnfrente())
        {
            Girar();
        }
    }

    void FixedUpdate()
    {
        bool estaDisparando = false;
        if (anim != null)
        {
            estaDisparando = anim.GetCurrentAnimatorStateInfo(0).IsName(nombreEstadoDisparo);
        }

        if (estaDisparando)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(direccion * velocidad, rb.linearVelocity.y);
        }
    }

    // Llamado por PlayerController cuando El Bit lo destruye con Dash
    public void MorirPorDash()
    {
        if (yaMurio) return;
        yaMurio = true;

        if (GameState.Instance != null)
        {
            GameState.Instance.OnEnemyKilled(tipoEnemigo);
        }

        Destroy(gameObject);
    }

    // Si El Bit toca al enemigo SIN estar haciendo Dash, recibe daño de contacto
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController pc = collision.gameObject.GetComponent<PlayerController>();
            if (pc != null && !pc.isDashing && GameState.Instance != null)
            {
                GameState.Instance.DamagePlayer(danoContactoBase);
            }
        }
    }

    bool DetectaParedEnfrente()
    {
        RaycastHit2D[] impactos = Physics2D.RaycastAll(detectorPared.position, Vector2.right * direccion, distanciaPared);
        foreach (RaycastHit2D hit in impactos)
        {
            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                if (hit.collider.CompareTag(tagPared)) return true;
            }
        }
        return false;
    }

    bool JugadorCerca()
    {
        if (player == null) return false;
        float distanciaX = Mathf.Abs(player.position.x - transform.position.x);
        float distanciaY = Mathf.Abs(player.position.y - transform.position.y);
        return distanciaX <= distanciaDeteccion && distanciaY <= margenAltura;
    }

    void MirarHaciaElJugador()
    {
        float diferenciaX = player.position.x - transform.position.x;
        if (Mathf.Abs(diferenciaX) > 0.2f)
        {
            if ((diferenciaX > 0 && direccion == -1) || (diferenciaX < 0 && direccion == 1))
            {
                Girar();
            }
        }
    }

    void Disparar()
    {
        if (anim != null)
        {
            anim.SetTrigger(nombreTriggerAtaque);
        }

        if (prefabProyectil != null)
        {
            GameObject proyectil = Instantiate(prefabProyectil, puntoDisparo.position, Quaternion.identity);
            ProyectilEnemigo scriptProyectil = proyectil.GetComponent<ProyectilEnemigo>();
            if (scriptProyectil != null)
            {
                scriptProyectil.Inicializar(direccion);
            }
        }
    }

    void Girar()
    {
        direccion *= -1;
        Vector3 escala = transform.localScale;
        escala.x = Mathf.Abs(escala.x) * direccion;
        transform.localScale = escala;
    }

    private void OnDrawGizmosSelected()
    {
        Transform origenPared = detectorPared != null ? detectorPared : transform;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(origenPared.position, origenPared.position + Vector3.right * direccion * distanciaPared);

        Transform origenDisparo = puntoDisparo != null ? puntoDisparo : transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origenDisparo.position - Vector3.right * distanciaDeteccion, origenDisparo.position + Vector3.right * distanciaDeteccion);
    }
}