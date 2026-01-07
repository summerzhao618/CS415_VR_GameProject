using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Monitors game conditions and triggers win screen when target score is reached
/// Works with ScoreManager to check score milestones
/// </summary>
public class WinConditionManager : MonoBehaviour
{
    // Singleton instance
    public static WinConditionManager Instance { get; private set; }

    [Header("Win Condition Settings")]
    [Tooltip("Target score to win (e.g., 10 coins = 100 points if each coin = 10)")]
    [SerializeField] private int targetScore = 100;

    [Tooltip("Number of coins needed to win (display only, actual check uses targetScore)")]
    [SerializeField] private int coinsToWin = 10;

    [Tooltip("Points per coin (for reference)")]
    [SerializeField] private int pointsPerCoin = 10;

    [Header("Win Screen Settings")]
    [Tooltip("Delay before showing win screen (seconds)")]
    [SerializeField] private float winScreenDelay = 1f;

    [Tooltip("Pause game when win screen is shown")]
    [SerializeField] private bool pauseOnWin = true;

    [Header("Events")]
    [Tooltip("Triggered when player wins")]
    public UnityEvent onWinConditionMet;

    // Properties
    public int TargetScore => targetScore;
    public int CoinsToWin => coinsToWin;
    public bool HasWon { get; private set; } = false;

    private bool isCheckingWin = false;

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

        // Calculate target score based on coins if not manually set
        if (targetScore == 100 && coinsToWin != 10)
        {
            targetScore = coinsToWin * pointsPerCoin;
        }
    }

    private void Start()
    {
        // Subscribe to score changes if needed
        Debug.Log($"Win Condition: Collect {coinsToWin} coins ({targetScore} points) to win!");
    }

    private void Update()
    {
        // Check win condition every frame (efficient since it's just a comparison)
        if (!HasWon && !isCheckingWin)
        {
            CheckWinCondition();
        }
    }

    /// <summary>
    /// Check if player has met win condition
    /// </summary>
    private void CheckWinCondition()
    {
        if (ScoreManager.Instance == null)
        {
            return;
        }

        int currentScore = ScoreManager.Instance.CurrentScore;

        if (currentScore >= targetScore)
        {
            TriggerWin();
        }
    }

    /// <summary>
    /// Manually trigger win condition (for testing)
    /// </summary>
    [ContextMenu("Trigger Win")]
    public void TriggerWin()
    {
        if (HasWon || isCheckingWin)
        {
            return;
        }

        isCheckingWin = true;
        HasWon = true;

        Debug.Log("🎉 WIN CONDITION MET! 🎉");

        // Invoke win event after delay
        Invoke(nameof(ShowWinScreen), winScreenDelay);
    }

    /// <summary>
    /// Show the win screen
    /// </summary>
    private void ShowWinScreen()
    {
        Debug.Log("Showing win screen...");

        // Trigger win event
        onWinConditionMet?.Invoke();

        // Show win screen UI
        if (WinScreenUI.Instance != null)
        {
            WinScreenUI.Instance.ShowWinScreen();
        }
        else
        {
            Debug.LogWarning("WinConditionManager: No WinScreenUI found in scene!");
        }

        // Pause game if enabled
        if (pauseOnWin)
        {
            Time.timeScale = 0f;
            Debug.Log("Game paused");
        }
    }

    /// <summary>
    /// Update target score (useful for difficulty settings)
    /// </summary>
    public void SetTargetScore(int newTarget)
    {
        targetScore = newTarget;
        coinsToWin = Mathf.CeilToInt((float)targetScore / pointsPerCoin);
        Debug.Log($"Win condition updated: {coinsToWin} coins ({targetScore} points)");
    }

    /// <summary>
    /// Update coins to win (recalculates target score)
    /// </summary>
    public void SetCoinsToWin(int newCoinsAmount)
    {
        coinsToWin = newCoinsAmount;
        targetScore = coinsToWin * pointsPerCoin;
        Debug.Log($"Win condition updated: {coinsToWin} coins ({targetScore} points)");
    }

    /// <summary>
    /// Get progress towards win condition (0-1)
    /// </summary>
    public float GetWinProgress()
    {
        if (ScoreManager.Instance == null)
        {
            return 0f;
        }

        return Mathf.Clamp01((float)ScoreManager.Instance.CurrentScore / targetScore);
    }

    /// <summary>
    /// Get remaining coins needed to win
    /// </summary>
    public int GetRemainingCoins()
    {
        if (ScoreManager.Instance == null)
        {
            return coinsToWin;
        }

        int currentCoins = Mathf.FloorToInt((float)ScoreManager.Instance.CurrentScore / pointsPerCoin);
        return Mathf.Max(0, coinsToWin - currentCoins);
    }

    /// <summary>
    /// Reset win condition (for restarting game)
    /// </summary>
    public void ResetWinCondition()
    {
        HasWon = false;
        isCheckingWin = false;
        Time.timeScale = 1f; // Unpause game
        Debug.Log("Win condition reset");
    }

    private void OnDestroy()
    {
        // Ensure time scale is reset
        Time.timeScale = 1f;
    }

    // Editor helpers
    private void OnValidate()
    {
        // Auto-calculate target score when coins to win changes in editor
        if (Application.isPlaying == false)
        {
            targetScore = coinsToWin * pointsPerCoin;
        }
    }
}
