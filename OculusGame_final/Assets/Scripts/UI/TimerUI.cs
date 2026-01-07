using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OculusGame.UI
{
    /// <summary>
    /// Displays the game timer on the UI with visual feedback.
    /// Works with GameTimerManager to show countdown and warning states.
    /// </summary>
    public class TimerUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("TextMeshProUGUI component to display the timer")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Tooltip("Optional Image component for timer background")]
        [SerializeField] private Image timerBackground;

        [Tooltip("Optional Image component for timer fill bar")]
        [SerializeField] private Image timerFillBar;

        [Header("Display Settings")]
        [Tooltip("Prefix text before the timer (e.g., 'Time: ')")]
        [SerializeField] private string timerPrefix = "Time: ";

        [Tooltip("Show time in MM:SS format")]
        [SerializeField] private bool useMinuteSecondFormat = true;

        [Tooltip("Font size for normal state")]
        [SerializeField] private float normalFontSize = 36f;

        [Header("Color Settings")]
        [Tooltip("Text color when time is normal")]
        [SerializeField] private Color normalColor = Color.white;

        [Tooltip("Text color when time is in warning state")]
        [SerializeField] private Color warningColor = Color.yellow;

        [Tooltip("Text color when time is critical (last few seconds)")]
        [SerializeField] private Color criticalColor = Color.red;

        [Tooltip("Background color when time is normal")]
        [SerializeField] private Color normalBackgroundColor = new Color(0, 0, 0, 0.5f);

        [Tooltip("Background color when time is in warning state")]
        [SerializeField] private Color warningBackgroundColor = new Color(1f, 1f, 0f, 0.5f);

        [Tooltip("Background color when time is critical")]
        [SerializeField] private Color criticalBackgroundColor = new Color(1f, 0f, 0f, 0.5f);

        [Header("Animation Settings")]
        [Tooltip("Enable pulsing animation in warning/critical states")]
        [SerializeField] private bool enablePulsing = true;

        [Tooltip("Speed of the pulsing animation")]
        [SerializeField] private float pulseSpeed = 2f;

        [Tooltip("Scale multiplier for pulsing (1.0 = no pulse)")]
        [SerializeField] private float pulseScale = 1.2f;

        [Header("Time Thresholds")]
        [Tooltip("Time threshold for warning state (seconds)")]
        [SerializeField] private float warningThreshold = 30f;

        [Tooltip("Time threshold for critical state (seconds)")]
        [SerializeField] private float criticalThreshold = 10f;

        // Private variables
        private GameTimerManager timerManager;
        private Vector3 originalScale;
        private bool isWarning = false;
        private bool isCritical = false;

        private void Start()
        {
            // Get timer manager reference
            timerManager = GameTimerManager.Instance;

            if (timerManager == null)
            {
                Debug.LogError("[TimerUI] GameTimerManager not found in scene!");
                enabled = false;
                return;
            }

            // Auto-find TextMeshProUGUI if not assigned
            if (timerText == null)
            {
                timerText = GetComponent<TextMeshProUGUI>();
                if (timerText == null)
                {
                    timerText = GetComponentInChildren<TextMeshProUGUI>();
                }
            }

            if (timerText == null)
            {
                Debug.LogError("[TimerUI] TextMeshProUGUI component not found!");
                enabled = false;
                return;
            }

            // Store original scale for pulsing
            originalScale = transform.localScale;

            // Set initial font size
            if (timerText != null)
            {
                timerText.fontSize = normalFontSize;
            }

            // Subscribe to timer events
            if (timerManager != null)
            {
                timerManager.OnWarningTime.AddListener(OnWarningTimeReached);
            }
        }

        private void Update()
        {
            if (timerManager == null || timerText == null)
                return;

            // Update timer display
            UpdateTimerDisplay();

            // Update visual state based on remaining time
            UpdateVisualState();

            // Handle pulsing animation
            if (enablePulsing && (isWarning || isCritical))
            {
                UpdatePulsingAnimation();
            }
            else
            {
                transform.localScale = originalScale;
            }
        }

        /// <summary>
        /// Updates the timer text display
        /// </summary>
        private void UpdateTimerDisplay()
        {
            string timeString;

            if (useMinuteSecondFormat)
            {
                timeString = timerManager.GetFormattedTime();
            }
            else
            {
                // Show as seconds only
                int seconds = Mathf.CeilToInt(timerManager.CurrentTime);
                timeString = seconds.ToString();
            }

            timerText.text = timerPrefix + timeString;
        }

        /// <summary>
        /// Updates colors and states based on remaining time
        /// </summary>
        private void UpdateVisualState()
        {
            float currentTime = timerManager.CurrentTime;

            // Determine current state
            if (currentTime <= criticalThreshold)
            {
                if (!isCritical)
                {
                    isCritical = true;
                    isWarning = false;
                    OnCriticalTimeReached();
                }
                SetVisualState(criticalColor, criticalBackgroundColor);
            }
            else if (currentTime <= warningThreshold)
            {
                if (!isWarning)
                {
                    isWarning = true;
                    isCritical = false;
                }
                SetVisualState(warningColor, warningBackgroundColor);
            }
            else
            {
                isWarning = false;
                isCritical = false;
                SetVisualState(normalColor, normalBackgroundColor);
            }

            // Update fill bar if available
            if (timerFillBar != null)
            {
                timerFillBar.fillAmount = timerManager.TimePercentage;
                timerFillBar.color = timerText.color;
            }
        }

        /// <summary>
        /// Sets the visual state (colors)
        /// </summary>
        private void SetVisualState(Color textColor, Color backgroundColor)
        {
            if (timerText != null)
            {
                timerText.color = textColor;
            }

            if (timerBackground != null)
            {
                timerBackground.color = backgroundColor;
            }
        }

        /// <summary>
        /// Handles pulsing animation
        /// </summary>
        private void UpdatePulsingAnimation()
        {
            float pulse = 1f + (Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f) * (pulseScale - 1f);
            transform.localScale = originalScale * pulse;
        }

        /// <summary>
        /// Called when warning time is reached
        /// </summary>
        private void OnWarningTimeReached()
        {
            Debug.Log("[TimerUI] Warning time reached!");
            // You can add additional visual effects here
        }

        /// <summary>
        /// Called when critical time is reached
        /// </summary>
        private void OnCriticalTimeReached()
        {
            Debug.Log("[TimerUI] Critical time reached!");
            // You can add additional visual effects here (screen shake, etc.)
        }

        /// <summary>
        /// Manually set the timer display (useful for custom displays)
        /// </summary>
        public void SetTimerDisplay(string text)
        {
            if (timerText != null)
            {
                timerText.text = text;
            }
        }

        /// <summary>
        /// Show or hide the timer UI
        /// </summary>
        public void SetTimerVisibility(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (timerManager != null)
            {
                timerManager.OnWarningTime.RemoveListener(OnWarningTimeReached);
            }
        }

        #region Editor Testing
        [ContextMenu("Test Warning State")]
        private void TestWarningState()
        {
            isWarning = true;
            isCritical = false;
            SetVisualState(warningColor, warningBackgroundColor);
        }

        [ContextMenu("Test Critical State")]
        private void TestCriticalState()
        {
            isWarning = false;
            isCritical = true;
            SetVisualState(criticalColor, criticalBackgroundColor);
        }

        [ContextMenu("Reset to Normal State")]
        private void TestNormalState()
        {
            isWarning = false;
            isCritical = false;
            SetVisualState(normalColor, normalBackgroundColor);
            transform.localScale = originalScale;
        }
        #endregion
    }
}
