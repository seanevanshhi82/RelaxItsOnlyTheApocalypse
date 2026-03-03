using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Zombie Sound Effects")]
    [Tooltip("Sounds played when detecting a target (randomly chosen)")]
    public List<AudioClip> detectSounds = new(); // 3 options

    [Tooltip("Sounds played when near a dead body (randomly chosen)")]
    public List<AudioClip> deadSounds = new(); // 2 options

    [Tooltip("Sounds played when taking damage (33% chance, randomly chosen)")]
    public List<AudioClip> hitSounds = new(); // 2 options

    [Tooltip("Sounds played during attack (33% chance, randomly chosen)")]
    public List<AudioClip> attackSounds = new(); // 3 options

    [Tooltip("Sounds played on death (33% chance, randomly chosen)")]
    public List<AudioClip> deathSounds = new(); // 3 options

    [Header("Audio Settings")]
    [Tooltip("Global volume multiplier for all zombie sounds (0-1)")]
    [Range(0f, 1f)] public float voiceVolume = 0.8f;

    [Tooltip("Base vocal range multiplier (range = voiceVolume * this value)")]
    public float vocalRangeMultiplier = 5f;

    [Header("Cooldowns (prevents spam)")]
    [Tooltip("Minimum time between any detect sound (seconds)")]
    public float detectCooldown = 8f;

    [Tooltip("Minimum time between any hit sound (seconds)")]
    public float hitCooldown = 3f;

    [Tooltip("Minimum time between any attack sound (seconds)")]
    public float attackCooldown = 4f;

    [Tooltip("Minimum time between any death sound (seconds)")]
    public float deathCooldown = 10f;

    private float lastDetectTime;
    private float lastHitTime;
    private float lastAttackTime;
    private float lastDeathTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: keep alive across scenes
    }

    /// <summary>
    /// Plays a random detect sound if cooldown allows, alerts nearby zombies
    /// </summary>
    public void PlayDetectSound(Vector3 position, AudioSource source)
    {
        if (Time.time < lastDetectTime + detectCooldown) return;
        if (detectSounds.Count == 0 || source == null) return;

        lastDetectTime = Time.time;
        AudioClip clip = detectSounds[Random.Range(0, detectSounds.Count)];
        source.PlayOneShot(clip, voiceVolume);
        AlertNearbyZombies(position);
    }

    /// <summary>
    /// Plays a random dead body sound (no cooldown, situational)
    /// </summary>
    public void PlayDeadSound(Vector3 position, AudioSource source)
    {
        if (deadSounds.Count == 0 || source == null) return;
        AudioClip clip = deadSounds[Random.Range(0, deadSounds.Count)];
        source.PlayOneShot(clip, voiceVolume * 0.7f); // Slightly quieter
    }

    /// <summary>
    /// 33% chance to play random hit sound if cooldown allows
    /// </summary>
    public void PlayHitSound(Vector3 position, AudioSource source)
    {
        if (Random.value > 0.33f) return;
        if (Time.time < lastHitTime + hitCooldown) return;
        if (hitSounds.Count == 0 || source == null) return;

        lastHitTime = Time.time;
        AudioClip clip = hitSounds[Random.Range(0, hitSounds.Count)];
        source.PlayOneShot(clip, voiceVolume);
        AlertNearbyZombies(position);
    }

    /// <summary>
    /// 33% chance to play random attack sound if cooldown allows
    /// </summary>
    public void PlayAttackSound(Vector3 position, AudioSource source)
    {
        if (Random.value > 0.33f) return;
        if (Time.time < lastAttackTime + attackCooldown) return;
        if (attackSounds.Count == 0 || source == null) return;

        lastAttackTime = Time.time;
        AudioClip clip = attackSounds[Random.Range(0, attackSounds.Count)];
        source.PlayOneShot(clip, voiceVolume);
        AlertNearbyZombies(position);
    }

    /// <summary>
    /// 33% chance to play random death sound if cooldown allows
    /// </summary>
    public void PlayDeathSound(Vector3 position, AudioSource source)
    {
        if (Random.value > 0.33f) return;
        if (Time.time < lastDeathTime + deathCooldown) return;
        if (deathSounds.Count == 0 || source == null) return;

        lastDeathTime = Time.time;
        AudioClip clip = deathSounds[Random.Range(0, deathSounds.Count)];
        source.PlayOneShot(clip, voiceVolume);
        AlertNearbyZombies(position);
    }

    /// <summary>
    /// Alerts all zombies within vocal range to set Alert = true and move toward sound source
    /// </summary>
    private void AlertNearbyZombies(Vector3 soundPosition)
    {
        float vocalRange = voiceVolume * vocalRangeMultiplier;

        Collider[] nearby = Physics.OverlapSphere(soundPosition, vocalRange, LayerMask.GetMask("Enemy"));
        foreach (var col in nearby)
        {
            BasicInfectedAI ai = col.GetComponent<BasicInfectedAI>();
            if (ai != null)
            {
                ai.alert = true;
                ai.lastKnownTargetPos = soundPosition;
                ai.MoveTo(soundPosition); // Move toward sound
                Debug.Log(col.gameObject.name + " alerted by sound at " + soundPosition);
            }
        }
    }

    // Optional: Call this from BasicInfectedAI when alert state changes
    public void OnAlertChanged(bool newAlertState)
    {
        // Future: global sound/animation tweaks if needed
    }
}