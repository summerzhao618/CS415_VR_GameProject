using UnityEngine;
using TMPro;

/// <summary>
/// Manages player score and displays it on UI
/// Singleton pattern for easy access from anywhere
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // Singleton instance
    public static ScoreManager Instance { get; private set; }

    [Header("Score Settings")]
    [Tooltip("Current player score")]
    [SerializeField] private int currentScore = 0;

    [Header("UI References")]
    [Tooltip("Text element to display score")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Tooltip("Text format (use {0} for score number)")]
    [SerializeField] private string scoreFormat = "Score: {0}";

    [Header("Animation Settings")]
    [Tooltip("Animate score text when points are added")]
    [SerializeField] private bool animateOnScoreChange = true;

    [Tooltip("Scale multiplier for animation")]
    [SerializeField] private float animationScale = 1.2f;

    [Tooltip("Animation duration")]
    [SerializeField] private float animationDuration = 0.2f;

    // Properties
    public int CurrentScore => currentScore;

    private Vector3 originalScale;
    private bool isAnimating = false;

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

        if (scoreText != null)
        {
            originalScale = scoreText.transform.localScale;
        }
    }

    private void Start()
    {
        UpdateScoreDisplay();
    }

    /// <summary>
    /// Add points to current score
    /// </summary>
    public void AddScore(int points)
    {
        currentScore += points;
        currentScore = Mathf.Max(currentScore, 0); // Don't allow negative scores

        Debug.Log($"Score added: +{points} | Total: {currentScore}");

        UpdateScoreDisplay();

        if (animateOnScoreChange && !isAnimating)
        {
            StartCoroutine(AnimateScoreText());
        }
    }

    /// <summary>
    /// Subtract points from score
    /// </summary>
    public void SubtractScore(int points)
    {
        AddScore(-points);
    }

    /// <summary>
    /// Reset score to zero
    /// </summary>
    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreDisplay();
        Debug.Log("Score reset to 0");
    }

    /// <summary>
    /// Set score to specific value
    /// </summary>
    public void SetScore(int newScore)
    {
        currentScore = newScore;
        UpdateScoreDisplay();
    }

    /// <summary>
    /// Update the score text display
    /// </summary>
    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = string.Format(scoreFormat, currentScore);
        }
    }

    /// <summary>
    /// Animate score text when score changes
    /// </summary>
    private System.Collections.IEnumerator AnimateScoreText()
    {
        if (scoreText == null)
        {
            yield break;
        }

        isAnimating = true;

        Transform textTransform = scoreText.transform;
        Vector3 targetScale = originalScale * animationScale;

        float elapsed = 0f;

        // Scale up
        while (elapsed < animationDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (animationDuration / 2f);
            textTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        elapsed = 0f;

        // Scale down
        while (elapsed < animationDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (animationDuration / 2f);
            textTransform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        textTransform.localScale = originalScale;
        isAnimating = false;
    }

    /// <summary>
    /// Save score to PlayerPrefs (optional persistence)
    /// </summary>
    public void SaveScore()
    {
        PlayerPrefs.SetInt("HighScore", Mathf.Max(currentScore, GetHighScore()));
        PlayerPrefs.Save();
        Debug.Log($"Score saved: {currentScore}");
    }

    /// <summary>
    /// Load high score from PlayerPrefs
    /// </summary>
    public int GetHighScore()
    {
        return PlayerPrefs.GetInt("HighScore", 0);
    }

    /// <summary>
    /// Check if current score is a new high score
    /// </summary>
    public bool IsNewHighScore()
    {
        return currentScore > GetHighScore();
    }

    // Called when game ends or player quits
    private void OnApplicationQuit()
    {
        SaveScore();
    }

    // Optional: Reset high score (for testing)
    [ContextMenu("Reset High Score")]
    public void ResetHighScore()
    {
        PlayerPrefs.DeleteKey("HighScore");
        Debug.Log("High score reset");
    }
}
