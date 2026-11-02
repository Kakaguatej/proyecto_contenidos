using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena

public class MainMenuManager : MonoBehaviour
{
    [Header("Panel de Créditos")]
    [SerializeField] private GameObject creditsPanel;

    [Header("Panel de Opciones")]
    [SerializeField] private GameObject optionsPanel;

    public void OpenCredits()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUiButton();
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUiButton();
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void OpenOptions()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUiButton();
        if (optionsPanel != null) optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUiButton();
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }


    // Función para iniciar el juego cargando el nivel 1
    public void PlayGame()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUiButton();
        SceneManager.LoadScene("Level1Scene");
    }

    public void QuitGame()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUiButton();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}