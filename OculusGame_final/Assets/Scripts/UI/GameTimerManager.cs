using UnityEngine;
using UnityEngine.Events;

namespace OculusGame.UI
{
    /// <summary>
    /// Manages game timer countdown and triggers win/lose conditions based on coin collection.
    /// Works with existing ScoreManager, WinScreenUI, and GameOverManager systems.
    /// </summary>
    public class GameTimerManager : MonoBehaviour
    {
        #region Singleton
        private static GameTimerManager _instance;
        public static GameTimerManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameTimerManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameTimerManager");
                        _instance = go.AddComponent<GameTimerManager>();
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        #endregion

        [Header("Timer Settings")]
        [Tooltip("Total time in seconds for the game (default: 120 = 2 minutes)")]
        [SerializeField] private float totalGameTime = 120f;

        [Tooltip("Number of coins required to win (default: 4)")]
        [SerializeField] private int coinsRequiredToWin = 4;

        [Tooltip("Points per coin (should match Collectible.cs pointValue)")]
        [SerializeField] private int pointsPerCoin = 10;

        [Header("Timer Control")]
        [Tooltip("Start timer automatically on game start")]
        [SerializeField] private bool startOnAwake = true;

        [Tooltip("Pause game when timer ends")]
        [SerializeField] private bool pauseOnTimerEnd = true;

        [Header("Audio (Optional)")]
        [Tooltip("Sound to play when timer reaches critical time")]
        [SerializeField] private AudioClip warningSound;

        [Tooltip("Time threshold for warning sound (in seconds)")]
        [SerializeField] private float warningTimeThreshold = 10f;

        [SerializeField] private float warningSoundVolume = 0.5f;

        [Header("Events")]
        [Tooltip("Triggered when timer reaches 0")]
        public UnityEvent OnTimerEnd;

        [Tooltip("Triggered when entering warning time")]
        public UnityEvent OnWarningTime;

        // Private variables
        private float currentTime;
        private bool isTimerRunning = false;
        private bool hasWarningPlayed = false;
        private bool hasTimerEnded = false;
        private AudioSource audioSource;

        #region Properties
        /// <summary>
        /// Current remaining time in seconds
        /// </summary>
        public float CurrentTime => currentTime;

        /// <summary>
        /// Total game time in seconds
        /// </summary>
        public float TotalTime => totalGameTime;

        /// <summary>
        /// Is the timer currently running?
        /// </summary>
        public bool IsTimerRunning => isTimerRunning;

        /// <summary>
        /// Has the timer reached zero?
        /// </summary>
        public bool HasTimerEnded => hasTimerEnded;

        /// <summary>
        /// Time remaining as a percentage (0-1)
        /// </summary>
        public float TimePercentage => Mathf.Clamp01(currentTime / totalGameTime);

        /// <summary>
        /// Number of coins required to win
        /// </summary>
        public int CoinsRequired => coinsRequiredToWin;
        #endregion

        private void Start()
        {
            // Setup audio source
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && warningSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound
            }

            // Initialize timer
            currentTime = totalGameTime;

            // Start timer if enabled
            if (startOnAwake)
            {
                StartTimer();
            }
        }

        private void Update()
        {
            if (!isTimerRunning || hasTimerEnded)
                return;

            // Countdown
            currentTime -= Time.deltaTime;

            // Check for warning time
            if (!hasWarningPlayed && currentTime <= warningTimeThreshold && currentTime > 0)
            {
                hasWarningPlayed = true;
                OnWarningTime?.Invoke();
                PlayWarningSound();
            }

            // Check if time is up
            if (currentTime <= 0)
            {
                currentTime = 0;
                hasTimerEnded = true;
                isTimerRunning = false;
                OnTimerEnd?.Invoke();
                HandleTimerEnd();
            }
        }

        /// <summary>
        /// Starts or resumes the timer
        /// </summary>
        public void StartTimer()
        {
            if (!hasTimerEnded)
            {
                isTimerRunning = true;
                Debug.Log($"[GameTimerManager] Timer started. Total time: {totalGameTime}s, Coins required: {coinsRequiredToWin}");
            }
        }

        /// <summary>
        /// Pauses the timer
        /// </summary>
        public void PauseTimer()
        {
            isTimerRunning = false;
            Debug.Log("[GameTimerManager] Timer paused");
        }

        /// <summary>
        /// Resumes the timer
        /// </summary>
        public void ResumeTimer()
        {
            if (!hasTimerEnded)
            {
                isTimerRunning = true;
                Debug.Log("[GameTimerManager] Timer resumed");
            }
        }

        /// <summary>
        /// Stops and resets the timer
        /// </summary>
        public void ResetTimer()
        {
            isTimerRunning = false;
            currentTime = totalGameTime;
            hasTimerEnded = false;
            hasWarningPlayed = false;
            Debug.Log("[GameTimerManager] Timer reset");
        }

        /// <summary>
        /// Adds time to the current timer (useful for power-ups)
        /// </summary>
        public void AddTime(float seconds)
        {
            currentTime += seconds;
            Debug.Log($"[GameTimerManager] Added {seconds}s. Current time: {currentTime}s");
        }

        /// <summary>
        /// Sets the total game time (must be called before starting)
        /// </summary>
        public void SetTotalTime(float seconds)
        {
            totalGameTime = seconds;
            currentTime = seconds;
            Debug.Log($"[GameTimerManager] Total time set to: {seconds}s");
        }

        /// <summary>
        /// Sets the number of coins required to win
        /// </summary>
        public void SetCoinsRequired(int coins)
        {
            coinsRequiredToWin = coins;
            Debug.Log($"[GameTimerManager] Coins required set to: {coins}");
        }

        /// <summary>
        /// Gets current coin count from ScoreManager
        /// </summary>
        public int GetCurrentCoins()
        {
            if (ScoreManager.Instance != null)
            {
                return ScoreManager.Instance.CurrentScore / pointsPerCoin;
            }
            return 0;
        }

        /// <summary>
        /// Checks if player has collected enough coins
        /// </summary>
        public bool HasEnoughCoins()
        {
            return GetCurrentCoins() >= coinsRequiredToWin;
        }

        /// <summary>
        /// Formats time as MM:SS
        /// </summary>
        public string GetFormattedTime()
        {
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        /// <summary>
        /// Handles what happens when timer reaches zero
        /// </summary>
        private void HandleTimerEnd()
        {
            Debug.Log("[GameTimerManager] Timer ended!");

            // Check if player has enough coins
            bool hasWon = HasEnoughCoins();
            int currentCoins = GetCurrentCoins();

            if (hasWon)
            {
                Debug.Log($"[GameTimerManager] Player WON! Collected {currentCoins}/{coinsRequiredToWin} coins");
                ShowWinScreen();

                // Pause game if enabled (after showing win screen)
                if (pauseOnTimerEnd)
                {
                    Time.timeScale = 0f;
                }
            }
            else
            {
                Debug.Log($"[GameTimerManager] Player LOST! Only collected {currentCoins}/{coinsRequiredToWin} coins");

                // Pause game BEFORE showing game over screen to prevent Invoke issues
                // We'll show game over immediately instead of using delay
                if (pauseOnTimerEnd)
                {
                    Time.timeScale = 0f;
                }

                ShowGameOverScreen();
            }
        }

        /// <summary>
        /// Shows the win screen using existing WinScreenUI
        /// </summary>
        private void ShowWinScreen()
        {
            if (WinScreenUI.Instance != null)
            {
                WinScreenUI.Instance.ShowWinScreen();
            }
            else
            {
                Debug.LogWarning("[GameTimerManager] WinScreenUI not found in scene!");
            }
        }

        /// <summary>
        /// Shows the game over screen using existing GameOverManager
        /// </summary>
        private void ShowGameOverScreen()
        {
            if (GameOverManager.Instance != null)
            {
                GameOverManager.Instance.ShowGameOver();
            }
            else
            {
                Debug.LogWarning("[GameTimerManager] GameOverManager not found in scene!");
            }
        }

        /// <summary>
        /// Plays warning sound when timer is running low
        /// </summary>
        private void PlayWarningSound()
        {
            if (warningSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(warningSound, warningSoundVolume);
                Debug.Log("[GameTimerManager] Warning sound played");
            }
        }

        #region Testing Methods (Editor Only)
        /// <summary>
        /// Manually trigger timer end (for testing)
        /// </summary>
        [ContextMenu("Test Timer End")]
        public void TestTimerEnd()
        {
            currentTime = 0;
            hasTimerEnded = true;
            isTimerRunning = false;
            HandleTimerEnd();
        }

        /// <summary>
        /// Set timer to 5 seconds for quick testing
        /// </summary>
        [ContextMenu("Set Timer to 5 Seconds")]
        public void SetTimerToFiveSeconds()
        {
            currentTime = 5f;
            hasTimerEnded = false;
            Debug.Log("[GameTimerManager] Timer set to 5 seconds for testing");
        }

        /// <summary>
        /// Add test coins to meet win condition
        /// </summary>
        [ContextMenu("Add Winning Amount of Coins")]
        public void AddWinningCoins()
        {
            if (ScoreManager.Instance != null)
            {
                int coinsNeeded = coinsRequiredToWin - GetCurrentCoins();
                if (coinsNeeded > 0)
                {
                    ScoreManager.Instance.AddScore(coinsNeeded * pointsPerCoin);
                    Debug.Log($"[GameTimerManager] Added {coinsNeeded} coins for testing");
                }
            }
        }
        #endregion
    }
}
