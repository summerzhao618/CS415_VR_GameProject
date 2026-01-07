using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Manages the Win Screen UI display
/// Shows congratulations message, final score, and options to restart or quit
/// Similar to GameOverManager but for winning
/// </summary>
public class WinScreenUI : MonoBehaviour
{
    // Singleton instance
    public static WinScreenUI Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Main win screen panel")]
    [SerializeField] private GameObject winScreenPanel;

    [Tooltip("Win title text (e.g., 'YOU WON!')")]
    [SerializeField] private TextMeshProUGUI winTitleText;

    [Tooltip("Congratulations message text")]
    [SerializeField] private TextMeshProUGUI congratsText;

    [Tooltip("Final score display text")]
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Tooltip("High score display text (optional)")]
    [SerializeField] private TextMeshProUGUI highScoreText;

    [Tooltip("Restart button")]
    [SerializeField] private Button restartButton;

    [Tooltip("Main menu button")]
    [SerializeField] private Button mainMenuButton;

    [Tooltip("Quit button")]
    [SerializeField] private Button quitButton;

    [Header("Win Screen Settings")]
    [Tooltip("Distance from camera in VR (for WorldSpace canvas)")]
    [SerializeField] private float distanceFromCamera = 2f;

    [Tooltip("Show cursor when win screen appears")]
    [SerializeField] private bool showCursor = true;

    [Header("Text Settings")]
    [Tooltip("Win title text")]
    [SerializeField] private string winTitle = "🎉 YOU WON! 🎉";

    [Tooltip("Congratulations message")]
    [SerializeField] private string congratsMessage = "Congratulations! You collected all the coins!";

    [Tooltip("Final score format")]
    [SerializeField] private string finalScoreFormat = "Final Score: {0}";

    [Tooltip("High score format")]
    [SerializeField] private string highScoreFormat = "High Score: {0}";

    [Header("Scene Management")]
    [Tooltip("Name of main menu scene (leave empty to reload current scene)")]
    [SerializeField] private string mainMenuSceneName = "";

    [Header("Audio Settings")]
    [Tooltip("Victory sound effect to play when winning")]
    [SerializeField] private AudioClip victorySoundEffect;

    [Tooltip("Volume for victory sound (0-1)")]
    [SerializeField] [Range(0f, 1f)] private float victoryVolume = 0.7f;

    private Canvas canvas;
    private bool isShowing = false;
    private AudioSource audioSource;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Get canvas component
        canvas = GetComponent<Canvas>();

        // Setup button listeners
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }

        // Setup audio source for victory sound
        if (victorySoundEffect != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = victorySoundEffect;
            audioSource.playOnAwake = false;
            audioSource.volume = victoryVolume;
            audioSource.spatialBlend = 0f; // 2D sound
        }
    }

    private void Start()
    {
        // Ensure win screen is hidden at start
        if (winScreenPanel != null)
        {
            winScreenPanel.SetActive(false);
        }

        // Setup canvas for VR if needed
        SetupCanvasForVR();

        // Add VR UI Helper if not present
        VRUIHelper vrHelper = GetComponent<VRUIHelper>();
        if (vrHelper == null)
        {
            vrHelper = gameObject.AddComponent<VRUIHelper>();
            Debug.Log("WinScreenUI: Added VRUIHelper component");
        }
    }

    /// <summary>
    /// Show the win screen with current score
    /// </summary>
    public void ShowWinScreen()
    {
        if (isShowing)
        {
            return;
        }

        isShowing = true;

        Debug.Log("Displaying win screen");

        // Play victory sound effect
        PlayVictorySound();

        // Activate panel
        if (winScreenPanel != null)
        {
            winScreenPanel.SetActive(true);
        }

        // Update text displays
        UpdateWinScreenText();

        // Position canvas in front of camera (for VR)
        PositionCanvasInFrontOfCamera();

        // Show cursor if enabled
        if (showCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // Save high score
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SaveScore();
        }
    }

    /// <summary>
    /// Play victory sound effect (works even when game is paused)
    /// </summary>
    private void PlayVictorySound()
    {
        if (audioSource != null && victorySoundEffect != null)
        {
            // Play sound unaffected by time scale (works when paused)
            audioSource.PlayOneShot(victorySoundEffect, victoryVolume);
            Debug.Log("Playing victory sound effect");
        }
    }

    /// <summary>
    /// Hide the win screen
    /// </summary>
    public void HideWinScreen()
    {
        isShowing = false;

        if (winScreenPanel != null)
        {
            winScreenPanel.SetActive(false);
        }

        // Hide cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Update all text elements on win screen
    /// </summary>
    private void UpdateWinScreenText()
    {
        int finalScore = 0;
        int highScore = 0;

        if (ScoreManager.Instance != null)
        {
            finalScore = ScoreManager.Instance.CurrentScore;
            highScore = ScoreManager.Instance.GetHighScore();
        }

        // Update win title
        if (winTitleText != null)
        {
            winTitleText.text = winTitle;
        }

        // Update congratulations message
        if (congratsText != null)
        {
            congratsText.text = congratsMessage;
        }

        // Update final score
        if (finalScoreText != null)
        {
            finalScoreText.text = string.Format(finalScoreFormat, finalScore);
        }

        // Update high score
        if (highScoreText != null)
        {
            highScoreText.text = string.Format(highScoreFormat, Mathf.Max(finalScore, highScore));
        }
    }

    /// <summary>
    /// Setup canvas for VR rendering
    /// </summary>
    private void SetupCanvasForVR()
    {
        if (canvas == null)
        {
            return;
        }

        // Set to WorldSpace for VR
        canvas.renderMode = RenderMode.WorldSpace;

        // Find and assign main camera
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            canvas.worldCamera = mainCamera;
        }

        // Check if canvas is child of XR Origin
        Transform parent = transform.parent;
        if (parent != null && parent.name.Contains("XR Origin"))
        {
            Debug.Log("WinScreenUI: Canvas is child of XR Origin");
        }
    }

    /// <summary>
    /// Position canvas in front of VR camera
    /// </summary>
    private void PositionCanvasInFrontOfCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null || canvas == null)
        {
            return;
        }

        // Position in front of camera
        Vector3 cameraPosition = mainCamera.transform.position;
        Vector3 cameraForward = mainCamera.transform.forward;

        transform.position = cameraPosition + cameraForward * distanceFromCamera;
        transform.rotation = Quaternion.LookRotation(transform.position - cameraPosition);
    }

    /// <summary>
    /// Restart the current game scene
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("Restarting game...");

        // Unpause game
        Time.timeScale = 1f;

        // Reset win condition
        if (WinConditionManager.Instance != null)
        {
            WinConditionManager.Instance.ResetWinCondition();
        }

        // Reset score
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }

        // Reload current scene
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    /// <summary>
    /// Go to main menu scene
    /// </summary>
    public void GoToMainMenu()
    {
        Debug.Log("Going to main menu...");

        // Unpause game
        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogWarning("Main menu scene name not set! Restarting current scene instead.");
            RestartGame();
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    /// <summary>
    /// Quit the game application
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Quitting game...");

        // Unpause game
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Update distance from camera (for adjusting in VR)
    /// </summary>
    public void SetDistanceFromCamera(float distance)
    {
        distanceFromCamera = distance;
        if (isShowing)
        {
            PositionCanvasInFrontOfCamera();
        }
    }
}
