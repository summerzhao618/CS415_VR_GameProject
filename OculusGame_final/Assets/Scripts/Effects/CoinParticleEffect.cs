using UnityEngine;

/// <summary>
/// Creates a procedural particle effect for coin collection
/// Spawns a burst of golden particles when a coin is collected
/// Can be used directly or as a prefab
/// </summary>
public class CoinParticleEffect : MonoBehaviour
{
    [Header("Particle Settings")]
    [Tooltip("Number of particles to spawn")]
    [SerializeField] private int particleCount = 20;

    [Tooltip("Lifetime of particles (seconds)")]
    [SerializeField] private float particleLifetime = 1.5f;

    [Tooltip("Speed of particles")]
    [SerializeField] private float particleSpeed = 3f;

    [Tooltip("Size of particles")]
    [SerializeField] private float particleSize = 0.1f;

    [Header("Colors")]
    [Tooltip("Start color (gold)")]
    [SerializeField] private Color startColor = new Color(1f, 0.84f, 0f, 1f); // Gold

    [Tooltip("End color (fade to transparent)")]
    [SerializeField] private Color endColor = new Color(1f, 0.84f, 0f, 0f); // Transparent gold

    [Header("Audio")]
    [Tooltip("Sound to play on collection")]
    [SerializeField] private AudioClip collectSound;

    [Tooltip("Volume of collection sound")]
    [SerializeField] [Range(0f, 1f)] private float soundVolume = 0.5f;

    private ParticleSystem particleSystem;
    private AudioSource audioSource;

    private void Awake()
    {
        // Create particle system if it doesn't exist
        particleSystem = GetComponent<ParticleSystem>();
        if (particleSystem == null)
        {
            SetupParticleSystem();
        }

        // Setup audio
        if (collectSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = collectSound;
            audioSource.playOnAwake = false;
            audioSource.volume = soundVolume;
            audioSource.spatialBlend = 1f; // 3D sound
        }
    }

    private void Start()
    {
        // Play particle effect
        if (particleSystem != null)
        {
            particleSystem.Play();
        }

        // Play sound
        if (audioSource != null && collectSound != null)
        {
            audioSource.Play();
        }

        // Auto-destroy after effect completes
        Destroy(gameObject, particleLifetime + 0.5f);
    }

    /// <summary>
    /// Setup the particle system with coin collection settings
    /// </summary>
    private void SetupParticleSystem()
    {
        particleSystem = gameObject.AddComponent<ParticleSystem>();

        // Main module
        var main = particleSystem.main;
        main.startLifetime = particleLifetime;
        main.startSpeed = particleSpeed;
        main.startSize = particleSize;
        main.startColor = new ParticleSystem.MinMaxGradient(startColor, endColor);
        main.maxParticles = particleCount;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        main.loop = false;

        // Emission module - burst all at once
        var emission = particleSystem.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, particleCount)
        });

        // Shape module - emit in sphere
        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;
        shape.randomDirectionAmount = 1f;

        // Color over lifetime - fade out
        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(endColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        // Size over lifetime - shrink
        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Velocity over lifetime - slow down
        var velocityOverLifetime = particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-2f); // Gravity effect

        // Renderer
        var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateParticleMaterial();
    }

    /// <summary>
    /// Create a simple particle material
    /// </summary>
    private Material CreateParticleMaterial()
    {
        // Use Unity's default particle shader
        Shader particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader == null)
        {
            // Fallback to unlit shader
            particleShader = Shader.Find("Unlit/Color");
        }

        Material mat = new Material(particleShader);
        mat.SetColor("_Color", startColor);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive blending
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3000;

        return mat;
    }

    /// <summary>
    /// Spawn a coin collection effect at specified position
    /// </summary>
    public static GameObject SpawnEffect(Vector3 position, Quaternion rotation = default)
    {
        if (rotation == default)
        {
            rotation = Quaternion.identity;
        }

        GameObject effectObj = new GameObject("CoinCollectionEffect");
        effectObj.transform.position = position;
        effectObj.transform.rotation = rotation;

        effectObj.AddComponent<CoinParticleEffect>();

        return effectObj;
    }

    /// <summary>
    /// Spawn effect with custom settings
    /// </summary>
    public static GameObject SpawnEffect(Vector3 position, int particleCount, Color color, AudioClip sound = null)
    {
        GameObject effectObj = SpawnEffect(position);
        CoinParticleEffect effect = effectObj.GetComponent<CoinParticleEffect>();

        if (effect != null)
        {
            effect.particleCount = particleCount;
            effect.startColor = color;
            effect.collectSound = sound;
        }

        return effectObj;
    }
}
