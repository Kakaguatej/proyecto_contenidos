using UnityEngine;

public class AutoDestroyEffect : MonoBehaviour
{
    [Header("Configuración de Vida")]
    [Tooltip("Tiempo en segundos antes de que el efecto se destruya solo")]
    [SerializeField] private float tiempoDeVida = 0.4f;

    void Start()
    {
        // Programa la destrucción del objeto apenas se crea en la escena
        Destroy(gameObject, tiempoDeVida);
    }
}