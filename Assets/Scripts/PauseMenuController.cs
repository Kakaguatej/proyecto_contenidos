using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla el menú de pausa del nivel: congela el tiempo, desactiva los
/// controles del jugador y permite ajustar audio o salir.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject pauseButton;

    [Header("Jugador")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerShield playerShield;

    private bool isPaused;

    private void Awake()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseButton != null) pauseButton.SetActive(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        if (isPaused)
        {
            return;
        }

        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null) pausePanel.SetActive(true);
        if (pauseButton != null) pauseButton.SetActive(false);
        if (playerController != null) playerController.enabled = false;
        if (playerShield != null) playerShield.enabled = false;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayUiButton();
    }

    public void Resume()
    {
        if (!isPaused)
        {
            return;
        }

        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseButton != null) pauseButton.SetActive(true);
        if (playerController != null) playerController.enabled = true;
        if (playerShield != null) playerShield.enabled = true;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayUiButton();
    }

    public void ExitToMenu()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUiButton();

        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }

    public void QuitGame()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUiButton();

        Time.timeScale = 1f;

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private void OnDisable()
    {
        if (isPaused)
        {
            isPaused = false;
            Time.timeScale = 1f;
            if (playerController != null) playerController.enabled = true;
            if (playerShield != null) playerShield.enabled = true;
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
