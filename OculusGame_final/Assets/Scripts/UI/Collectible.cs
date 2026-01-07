using UnityEngine;

/// <summary>
/// Collectible item that player can pick up
/// Adds score and can optionally heal player
/// </summary>
[RequireComponent(typeof(Collider))]
public class Collectible : MonoBehaviour
{
    [Header("Collectible Settings")]
    [Tooltip("Points awarded when collected")]
    [SerializeField] private int pointValue = 10;

    [Tooltip("Amount to heal player (0 = no healing)")]
    [SerializeField] private float healAmount = 0f;

    [Tooltip("Tag of the player object")]
    [SerializeField] private string playerTag = "Player";

    [Header("Visual Settings")]
    [Tooltip("Rotate the collectible")]
    [SerializeField] private bool rotateObject = true;

    [Tooltip("Rotation speed (degrees per second)")]
    [SerializeField] private float rotationSpeed = 90f;

    [Tooltip("Bob up and down")]
    [SerializeField] private bool bobUpDown = true;

    [Tooltip("Bob height offset")]
    [SerializeField] private float bobHeight = 0.3f;

    [Tooltip("Bob speed")]
    [SerializeField] private float bobSpeed = 2f;

    [Header("Collection Effects")]
    [Tooltip("Particle effect to spawn on collection (optional)")]
    [SerializeField] private GameObject collectEffect;

    [Tooltip("Sound to play on collection (optional)")]
    [SerializeField] private AudioClip collectSound;

    [Tooltip("Whether to destroy collectible immediately or after effect")]
    [SerializeField] private bool destroyImmediately = false;

    [Tooltip("Delay before destroying if not immediate")]
    [SerializeField] private float destroyDelay = 0.5f;

    private Vector3 startPosition;
    private AudioSource audioSource;
    private bool isCollected = false;

    private void Awake()
    {
        // Ensure collider is trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Setup audio source if sound is assigned
        if (collectSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = collectSound;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }
    }

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        if (isCollected)
        {
            return;
        }

        // Rotate collectible
        if (rotateObject)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }

        // Bob up and down
        if (bobUpDown)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Prevent multiple collections
        if (isCollected)
        {
            return;
        }

        // Check if player collected this
        if (other.CompareTag(playerTag))
        {
            Collect(other.gameObject);
        }
    }

    private void Collect(GameObject player)
    {
        isCollected = true;

        Debug.Log($"Collectible picked up! Points: +{pointValue}");

        // Add score
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(pointValue);
        }
        else
        {
            Debug.LogWarning("Collectible: No ScoreManager found in scene!");
        }

        // Heal player if applicable
        if (healAmount > 0f)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = player.GetComponentInParent<PlayerHealth>();
            }

            if (playerHealth != null)
            {
                playerHealth.Heal(healAmount);
            }
        }

        // Play collection sound
        if (audioSource != null && collectSound != null)
        {
            audioSource.Play();
        }

        // Spawn particle effect
        if (collectEffect != null)
        {
            GameObject effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // Hide visual
        HideVisual();

        // Destroy collectible
        if (destroyImmediately)
        {
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    private void HideVisual()
    {
        // Disable renderer components
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            rend.enabled = false;
        }

        // Disable collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
    }

    /// <summary>
    /// Manually trigger collection (for testing)
    /// </summary>
    [ContextMenu("Collect This Item")]
    public void TestCollect()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            Collect(player);
        }
        else
        {
            Debug.LogWarning("No player found to test collection!");
        }
    }

    // Draw collection radius in editor
    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.yellow;
            if (col is SphereCollider sphereCol)
            {
                Gizmos.DrawWireSphere(transform.position, sphereCol.radius * transform.localScale.x);
            }
        }
    }
}
