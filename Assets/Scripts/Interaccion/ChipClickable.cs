using UnityEngine;

public class ChipClickable : MonoBehaviour
{
    [Header("Configuración del Chip")]
    public GateType tipoDeChip; // AND, OR o NOT

    private LogicSocketController socketPadre;

    private void Awake()
    {
        // Busca el script del zócalo en el objeto padre
        socketPadre = GetComponentInParent<LogicSocketController>();
    }

    // Se ejecuta automáticamente al hacer clic sobre el Collider2D de este objeto
    private void OnMouseDown()
    {
        if (socketPadre != null)
        {
            socketPadre.SeleccionarChipConMouse(tipoDeChip);
        }
    }

    // Efecto visual opcional: agranda ligeramente el chip al pasar el cursor encima
    private void OnMouseEnter()
    {
        transform.localScale *= 1.15f;
    }

    private void OnMouseExit()
    {
        transform.localScale /= 1.15f;
    }
}