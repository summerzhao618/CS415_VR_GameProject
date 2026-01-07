using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Screen-space UI health bar for player
/// Displays in corner of screen with text and bar
/// </summary>
public class PlayerHealthBar : MonoBehaviour
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

    private float previousHealth;
    private bool isFlashing = false;

    private void Start()
    {
        // Auto-find player health if not assigned
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();

            if (playerHealth == null)
            {
                Debug.LogWarning("PlayerHealthBar: No PlayerHealth component found in scene!");
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
        if (playerHealth == null || healthBarFill == null)
        {
            return;
        }

        float healthPercentage = playerHealth.GetHealthPercentage();

        // Update fill amount
        healthBarFill.fillAmount = healthPercentage;

        // Update color based on health
        if (!isFlashing)
        {
            healthBarFill.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercentage / lowHealthThreshold);
        }

        // Update text
        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(playerHealth.CurrentHealth)} / {playerHealth.MaxHealth}";
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
        }
        UpdateHealthBar();
    }
}
