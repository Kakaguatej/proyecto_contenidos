using UnityEngine;

public class Camara : MonoBehaviour
{
    public Transform target;     // Tu jugador (El Bit)
    public float minX;           // Límite izquierdo donde la cámara se detiene
    public bool usarMaxX = false;
    public float maxX;   
    public float minY;           // Límite izquierdo donde la cámara se detiene
    public bool usarMaxY = false;
    public float maxY;          // Opcional: Límite derecho del nivel

    private void LateUpdate()
    {
        if (target == null) return;

        // La cámara sigue al jugador en X, pero NUNCA bajará del valor minX
        float nuevaX = Mathf.Max(target.position.x, minX);
        float nuevaY = Mathf.Max(target.position.y, minY);

        // Si en el futuro quieres un límite final en la meta, se aplica aquí
        if (usarMaxX)
        {
            nuevaX = Mathf.Clamp(nuevaX, minX, maxX);
        }

        if (usarMaxY)
        {
            nuevaY = Mathf.Clamp(nuevaY, minY, maxY);
        }

        // Aplica la posición manteniendo tu Y y Z originales
        transform.position = new Vector3(nuevaX, nuevaY, transform.position.z);
    }
}