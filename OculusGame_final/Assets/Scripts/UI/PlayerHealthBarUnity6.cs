using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Unity 6.2 Compatible Player Health Bar
/// Uses RectTransform width instead of Image.fillAmount
/// </summary>
public class PlayerHealthBarUnity6 : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to player health component")]
    [SerializeField] private PlayerHealth playerHealth;

    [Tooltip("The fill image that represents health amount")]
    [SerializeField] private Image healthBarFill;

    [Tooltip("Text to display health numbers (optional)")]
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Visual Settings")]
    [Tooltip("Color when health is full")]
    [SerializeField] private Color fullHealthColor = Color.green;

    [Tooltip("Color when health is low")]
    [SerializeField] private Color lowHealthColor = Color.red;

    [Tooltip("Health percentage threshold for low health (0-1)")]
    [SerializeField] private float lowHealthThreshold = 0.3f;

    [Tooltip("Animate when taking damage")]
    [SerializeField] private bool animateOnDamage = true;

    [Tooltip("Flash duration when taking damage")]
    [SerializeField] private float flashDuration = 0.2f;

    private RectTransform fillRectTransform;
    private float maxWidth;
    private float previousHealth;
    private bool isFlashing = false;

    private void Awake()
    {
        if (healthBarFill != null)
        {
            fillRectTransform = healthBarFill.GetComponent<RectTransform>();

            // Store the maximum width
            if (fillRectTransform != null)
            {
                maxWidth = fillRectTransform.rect.width;
            }
        }
    }

    private void Start()
    {
        // Auto-find player health if not assigned
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();

            if (playerHealth == null)
            {
                Debug.LogWarning("PlayerHealthBarUnity6: No PlayerHealth component found in scene!");
            }
            else
            {
                Debug.Log("PlayerHealthBarUnity6: Found PlayerHealth on " + playerHealth.gameObject.name);
            }
        }

        if (playerHealth != null)
        {
            previousHealth = playerHealth.CurrentHealth;
        }

        UpdateHealthBar();
    }

    private void Update()
    {
        if (playerHealth == null)
        {
            // Try to find it again
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            return;
        }

        // Check if health changed
        if (animateOnDamage && playerHealth.CurrentHealth < previousHealth && !isFlashing)
        {
            StartCoroutine(FlashDamage());
        }

        previousHealth = playerHealth.CurrentHealth;

        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (playerHealth == null)
        {
            return;
        }

        float healthPercentage = playerHealth.GetHealthPercentage();

        // Update fill width using RectTransform
        if (fillRectTransform != null && healthBarFill != null)
        {
            // Get the parent width (background)
            RectTransform parentRect = fillRectTransform.parent as RectTransform;
            if (parentRect != null)
            {
                float parentWidth = parentRect.rect.width;

                // Set the fill width based on health percentage
                fillRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, parentWidth * healthPercentage);
            }

            // Update color based on health
            if (!isFlashing)
            {
                // Calculate color transition
                // When health > threshold: green
                // When health < threshold: transition from red to yellow/green
                float colorLerpValue;
                if (healthPercentage > lowHealthThreshold)
                {
                    // Above threshold: always full health color (green)
                    colorLerpValue = 1f;
                }
                else
                {
                    // Below threshold: lerp from red (0%) to green (threshold%)
                    colorLerpValue = healthPercentage / lowHealthThreshold;
                }

                healthBarFill.color = Color.Lerp(lowHealthColor, fullHealthColor, colorLerpValue);
            }
        }

        // Update text
        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(playerHealth.CurrentHealth)} / {playerHealth.MaxHealth}";
        }

        // Debug output
        if (Time.frameCount % 60 == 0) // Every second
        {
            Debug.Log($"PlayerHealthBar: Health {playerHealth.CurrentHealth}/{playerHealth.MaxHealth} ({healthPercentage * 100:F0}%)");
        }
    }

    private System.Collections.IEnumerator FlashDamage()
    {
        isFlashing = true;

        // Flash to white/red
        if (healthBarFill != null)
        {
            Color originalColor = healthBarFill.color;
            healthBarFill.color = Color.white;

            yield return new WaitForSeconds(flashDuration);

            healthBarFill.color = originalColor;
        }

        isFlashing = false;
    }

    /// <summary>
    /// Manually set the player health reference
    /// </summary>
    public void SetPlayerHealth(PlayerHealth health)
    {
        playerHealth = health;
        if (playerHealth != null)
        {
            previousHealth = playerHealth.CurrentHealth;
            Debug.Log("PlayerHealthBarUnity6: PlayerHealth reference set to " + health.gameObject.name);
        }
        UpdateHealthBar();
    }

    // Debug in editor
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateHealthBar();
        }
    }
}
