using UnityEngine;

namespace OculusGame.Collectibles
{
    /// <summary>
    /// Health pack that restores player health when collected.
    /// Features: Healing, visual effects, rotation animation, particle effects, sound effects.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HealthPack : MonoBehaviour
    {
        [Header("Health Settings")]
        [Tooltip("Amount of health to restore")]
        [SerializeField] private float healAmount = 25f;

        [Tooltip("Can heal beyond max health (overheal)")]
        [SerializeField] private bool allowOverheal = false;

        [Header("Player Detection")]
        [Tooltip("Tag to identify the player")]
        [SerializeField] private string playerTag = "Player";

        [Header("Visual Effects")]
        [Tooltip("Enable rotation animation")]
        [SerializeField] private bool rotateObject = true;

        [Tooltip("Rotation speed (degrees per second)")]
        [SerializeField] private float rotationSpeed = 90f;

        [Tooltip("Enable vertical bobbing animation")]
        [SerializeField] private bool bobUpDown = true;

        [Tooltip("Bobbing height")]
        [SerializeField] private float bobHeight = 0.3f;

        [Tooltip("Bobbing speed")]
        [SerializeField] private float bobSpeed = 2f;

        [Header("Collection Effects")]
        [Tooltip("Particle effect to spawn on collection")]
        [SerializeField] private GameObject collectEffect;

        [Tooltip("Sound to play when collected")]
        [SerializeField] private AudioClip collectSound;

        [Tooltip("Volume of collect sound")]
        [SerializeField] private float soundVolume = 0.7f;

        [Tooltip("Destroy immediately or with delay")]
        [SerializeField] private bool destroyImmediately = true;

        [Tooltip("Delay before destroying (if not immediate)")]
        [SerializeField] private float destroyDelay = 0.5f;

        [Header("Optional - Material Flash")]
        [Tooltip("Flash material when player is nearby")]
        [SerializeField] private bool enableFlash = true;

        [Tooltip("Flash color")]
        [SerializeField] private Color flashColor = new Color(0f, 1f, 0f, 1f); // Green

        [Tooltip("Flash speed")]
        [SerializeField] private float flashSpeed = 3f;

        // Private variables
        private Vector3 startPosition;
        private Renderer objectRenderer;
        private Material objectMaterial;
        private Color originalColor;
        private bool hasBeenCollected = false;

        private void Start()
        {
            // Store starting position for bobbing
            startPosition = transform.position;

            // Get renderer and material for flashing effect
            objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null && objectRenderer.material != null)
            {
                objectMaterial = objectRenderer.material;
                if (objectMaterial.HasProperty("_Color"))
                {
                    originalColor = objectMaterial.color;
                }
                else if (objectMaterial.HasProperty("_BaseColor"))
                {
                    originalColor = objectMaterial.GetColor("_BaseColor");
                }
            }

            // Ensure collider is a trigger
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Update()
        {
            if (hasBeenCollected)
                return;

            // Rotation animation
            if (rotateObject)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }

            // Bobbing animation
            if (bobUpDown)
            {
                float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }

            // Flash effect
            if (enableFlash && objectMaterial != null)
            {
                float lerp = Mathf.PingPong(Time.time * flashSpeed, 1f);
                Color newColor = Color.Lerp(originalColor, flashColor, lerp * 0.5f);

                if (objectMaterial.HasProperty("_Color"))
                {
                    objectMaterial.color = newColor;
                }
                else if (objectMaterial.HasProperty("_BaseColor"))
                {
                    objectMaterial.SetColor("_BaseColor", newColor);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if the colliding object is the player
            if (hasBeenCollected)
                return;

            if (other.CompareTag(playerTag))
            {
                Collect(other.gameObject);
            }
        }

        /// <summary>
        /// Handle collection of the health pack
        /// </summary>
        private void Collect(GameObject player)
        {
            if (hasBeenCollected)
                return;

            hasBeenCollected = true;

            // Find PlayerHealth component
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

            if (playerHealth == null)
            {
                // Try to find in parent
                playerHealth = player.GetComponentInParent<PlayerHealth>();
            }

            if (playerHealth == null)
            {
                // Try to find in children
                playerHealth = player.GetComponentInChildren<PlayerHealth>();
            }

            if (playerHealth != null)
            {
                // Check if player needs healing
                if (!allowOverheal && playerHealth.CurrentHealth >= playerHealth.MaxHealth)
                {
                    Debug.Log("[HealthPack] Player health is already full!");
                    hasBeenCollected = false; // Allow re-collection
                    return;
                }

                // Heal the player
                float healthBefore = playerHealth.CurrentHealth;
                playerHealth.Heal(healAmount);
                float healthAfter = playerHealth.CurrentHealth;
                float actualHealed = healthAfter - healthBefore;

                Debug.Log($"[HealthPack] Healed player for {actualHealed} HP! ({healthBefore} -> {healthAfter})");
            }
            else
            {
                Debug.LogWarning("[HealthPack] PlayerHealth component not found on player!");
            }

            // Spawn particle effect
            if (collectEffect != null)
            {
                GameObject effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
                Destroy(effect, 3f); // Clean up after 3 seconds
            }

            // Play sound effect
            if (collectSound != null)
            {
                // Create a temporary GameObject for the sound
                GameObject soundObject = new GameObject("HealthPackSound");
                soundObject.transform.position = transform.position;

                AudioSource audioSource = soundObject.AddComponent<AudioSource>();
                audioSource.clip = collectSound;
                audioSource.volume = soundVolume;
                audioSource.spatialBlend = 1f; // 3D sound
                audioSource.maxDistance = 20f;
                audioSource.Play();

                // Destroy sound object after sound finishes
                Destroy(soundObject, collectSound.length + 0.1f);
            }

            // Destroy or hide the health pack
            if (destroyImmediately)
            {
                Destroy(gameObject);
            }
            else
            {
                // Hide visuals but keep for sound
                if (objectRenderer != null)
                {
                    objectRenderer.enabled = false;
                }

                // Disable collider
                Collider col = GetComponent<Collider>();
                if (col != null)
                {
                    col.enabled = false;
                }

                Destroy(gameObject, destroyDelay);
            }
        }

        /// <summary>
        /// Manually trigger collection (for testing)
        /// </summary>
        [ContextMenu("Test Collect")]
        public void TestCollect()
        {
            // Find player in scene
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);

            if (player != null)
            {
                Collect(player);
            }
            else
            {
                Debug.LogWarning($"[HealthPack] No GameObject found with tag '{playerTag}'");
            }
        }

        /// <summary>
        /// Set heal amount dynamically
        /// </summary>
        public void SetHealAmount(float amount)
        {
            healAmount = amount;
        }

        /// <summary>
        /// Get heal amount
        /// </summary>
        public float GetHealAmount()
        {
            return healAmount;
        }

        private void OnDestroy()
        {
            // Clean up material instance
            if (objectMaterial != null && objectRenderer != null)
            {
                Destroy(objectMaterial);
            }
        }

        #region Editor Gizmos
        private void OnDrawGizmosSelected()
        {
            // Draw healing range visualization
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);

                if (col is SphereCollider sphereCol)
                {
                    Gizmos.DrawWireSphere(transform.position, sphereCol.radius * transform.localScale.x);
                }
                else if (col is BoxCollider boxCol)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(boxCol.center, boxCol.size);
                }
            }

            // Draw heal amount text in scene view (Unity Editor only)
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, $"Heal: +{healAmount} HP");
            #endif
        }
        #endregion
    }
}
