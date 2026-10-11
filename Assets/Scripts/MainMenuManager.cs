using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena

public class MainMenuManager : MonoBehaviour
{
    [Header("Panel de Créditos")]
    [SerializeField] private GameObject creditsPanel;

    public void OpenCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    // Función para iniciar el juego cargando el nivel 1
    public void PlayGame()
    {
        SceneManager.LoadScene("Level1Scene");
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}