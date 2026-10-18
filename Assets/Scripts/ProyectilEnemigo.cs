using UnityEngine;

public class ProyectilEnemigo : MonoBehaviour
{
    [Header("Configuración del Proyectil")]
    public float velocidad = 7f;
    public float tiempoDeVida = 3f;
    public string tagPared = "Floor"; // El tag de tus muros/suelo para que la bala se destruya al chocar

    private int direccion = 1;

    // Esta función la llama el script Enemigo.cs automáticamente al disparar
    public void Inicializar(int dir)
    {
        direccion = dir;

        // Voltea el dibujo del proyectil según hacia dónde dispare el enemigo
        Vector3 escala = transform.localScale;
        escala.x = Mathf.Abs(escala.x) * direccion;
        transform.localScale = escala;

        // Destruye el proyectil tras unos segundos en el aire para no llenar la memoria RAM
        Destroy(gameObject, tiempoDeVida);
    }

    void Update()
    {
        // Mueve el proyectil en línea recta horizontalmente
        transform.Translate(Vector2.right * direccion * velocidad * Time.deltaTime, Space.World);
    }

    // Detecta cuando el proyectil choca con algo (Requiere que la casilla "Is Trigger" esté marcada en su Collider)
    private void OnTriggerEnter2D(Collider2D otro)
    {
        // Si choca contra el jugador ("El Bit")
        if (otro.CompareTag("Player"))
        {
            Debug.Log("¡El proyectil impactó al jugador!");
            // NOTA: Aquí conectaremos la función para quitarle vida al jugador más adelante
            
            Destroy(gameObject); // La bala se destruye al impactar
        }
        // Si choca contra un muro o el suelo
        else if (otro.CompareTag(tagPared)) 
        {
            Destroy(gameObject); // La bala se destruye sin hacer daño
        }
    }
}