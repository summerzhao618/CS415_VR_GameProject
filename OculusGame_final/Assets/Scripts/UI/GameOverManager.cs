using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Manages Game Over screen with Restart and Exit options
/// Singleton pattern for easy access from anywhere
/// </summary>
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The entire Game Over UI panel")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("Game Over title text")]
    [SerializeField] private TextMeshProUGUI gameOverText;

    [Tooltip("Final score text (optional)")]
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Header("Settings")]
    [Tooltip("Delay before showing Game Over screen")]
    [SerializeField] private float showDelay = 1f;

    [Tooltip("Pause game when Game Over")]
    [SerializeField] private bool pauseOnGameOver = false;

    [Header("VR Camera")]
    [Tooltip("VR Camera to position UI in front of")]
    [SerializeField] private Transform vrCamera;

    [Tooltip("Distance from camera to place UI")]
    [SerializeField] private float distanceFromCamera = 2f;

    private bool isGameOver = false;

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

        // Auto-find VR camera if not assigned
        if (vrCamera == null)
        {
            vrCamera = Camera.main?.transform;
        }

        // Setup canvas for VR interaction
        SetupCanvasForVR();

        // Hide Game Over panel at start
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Configure canvas for VR controller interaction
    /// </summary>
    private void SetupCanvasForVR()
    {
        if (gameOverPanel == null) return;

        // Get canvas
        Transform canvasTransform = gameOverPanel.transform.parent;
        if (canvasTransform == null) return;

        Canvas canvas = canvasTransform.GetComponent<Canvas>();
        if (canvas == null) return;

        // Ensure World Space mode
        canvas.renderMode = RenderMode.WorldSpace;

        // Assign event camera if not set
        if (canvas.worldCamera == null)
        {
            canvas.worldCamera = Camera.main;
        }
    }

    /// <summary>
    /// Show Game Over screen
    /// </summary>
    public void ShowGameOver()
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;

        // If game is paused (Time.timeScale == 0), show immediately
        // Otherwise use delay
        if (Time.timeScale == 0f || showDelay <= 0f)
        {
            DisplayGameOver();
        }
        else
        {
            Invoke(nameof(DisplayGameOver), showDelay);
        }
    }

    private void DisplayGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            // Position UI in front of VR camera (only if canvas is not child of XR Origin)
            if (!IsCanvasChildOfXROrigin())
            {
                PositionUIInFrontOfCamera();
            }
        }

        // Update final score if available
        if (finalScoreText != null && ScoreManager.Instance != null)
        {
            int finalScore = ScoreManager.Instance.CurrentScore;
            int highScore = ScoreManager.Instance.GetHighScore();

            finalScoreText.text = $"Final Score: {finalScore}\nHigh Score: {highScore}";
        }

        // Pause game if enabled
        if (pauseOnGameOver)
        {
            Time.timeScale = 0f;
        }

        // Show cursor for VR menu interaction
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// Check if canvas is already a child of XR Origin
    /// </summary>
    private bool IsCanvasChildOfXROrigin()
    {
        if (gameOverPanel == null) return false;

        Transform canvasTransform = gameOverPanel.transform.parent;
        if (canvasTransform == null) return false;

        // Check if any parent is named "XR Origin"
        Transform current = canvasTransform;
        while (current != null)
        {
            if (current.name.Contains("XR Origin"))
            {
                return true;
            }
            current = current.parent;
        }
        return false;
    }

    /// <summary>
    /// Position the Game Over UI in front of the VR camera
    /// </summary>
    private void PositionUIInFrontOfCamera()
    {
        if (vrCamera == null || gameOverPanel == null)
        {
            return;
        }

        // Get the canvas (parent of gameOverPanel)
        Transform canvasTransform = gameOverPanel.transform.parent;
        if (canvasTransform == null)
        {
            return;
        }

        // Position canvas in front of camera at eye level
        Vector3 cameraPosition = vrCamera.position;
        Vector3 cameraForward = vrCamera.forward;

        // Calculate position: camera position + forward direction * distance
        Vector3 targetPosition = cameraPosition + cameraForward * distanceFromCamera;

        // Set canvas position
        canvasTransform.position = targetPosition;

        // Make canvas face the camera
        canvasTransform.rotation = Quaternion.LookRotation(canvasTransform.position - cameraPosition);
    }

    /// <summary>
    /// Restart the current scene
    /// </summary>
    public void RestartGame()
    {
        // Unpause if paused
        Time.timeScale = 1f;

        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Exit to main menu or quit application
    /// </summary>
    public void ExitGame()
    {
        // Unpause if paused
        Time.timeScale = 1f;

        // Try to load main menu scene
        // If no main menu, just quit application
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// Check if game is over
    /// </summary>
    public bool IsGameOver()
    {
        return isGameOver;
    }

    /// <summary>
    /// Reset game over state (for testing)
    /// </summary>
    public void ResetGameOver()
    {
        isGameOver = false;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        Time.timeScale = 1f;
    }
}
