using UnityEngine;

public class Parallax : MonoBehaviour
{
    public float parallaxMultiplier = 0.1f; // La velocidad a la que se moverá la capa

    private Material parallaxMaterial;
    private Transform player;
    private float lastPlayerX;

    void Start()
    {
        // Obtiene el material del fondo (MeshRenderer en Unity 6 / Quad)
        parallaxMaterial = GetComponent<Renderer>().material;
        
        // Encuentra al jugador usando el Tag "Player"
        player = GameObject.FindGameObjectWithTag("Player").transform;
        
        // Guarda la posición inicial del jugador
        lastPlayerX = player.position.x;
    }

    void Update()
    {
        // Calcula cuánto se movió el jugador desde el último frame
        float deltaX = player.position.x - lastPlayerX;

        // Suma ese movimiento al Offset del material multiplicado por la velocidad
        parallaxMaterial.mainTextureOffset += new Vector2(deltaX * parallaxMultiplier, 0);

        // Actualiza la última posición conocida del jugador
        lastPlayerX = player.position.x;
    }
}