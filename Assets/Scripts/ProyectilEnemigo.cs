using UnityEngine;

public class ProyectilEnemigo : MonoBehaviour
{
    [Header("Configuración del Proyectil")]
    public float velocidad = 7f;
    public float tiempoDeVida = 3f;
    public string tagPared = "Floor";
    public string tagEscudo = "Shield";

    [Header("Tipo de Daño")]
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
                Debug.Log("¡Proyectil bloqueado por el escudo!");
                escudo.OnHit(esCritico);
                Destroy(gameObject);
                return;
            }
        }

        // 2. SI CHOCA CONTRA EL JUGADOR (Sin escudo)
        if (otro.CompareTag("Player"))
        {
            Debug.Log("¡Impacto directo a El Bit!");
            // Destruye la bala al impactar directamente
            Destroy(gameObject);
        }
        // 3. SI CHOCA CONTRA EL SUELO O PARED
        else if (otro.CompareTag(tagPared)) 
        {
            Destroy(gameObject);
        }
    }
}