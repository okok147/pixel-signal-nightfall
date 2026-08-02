using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Small data-driven audio layer for the prototype. It observes the private
/// simulation state without coupling the gameplay file to Unity audio setup,
/// then turns state changes into music, ambience, UI and combat feedback.
/// </summary>
[DefaultExecutionOrder(10001)]
public sealed class NightfallAudio : MonoBehaviour
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string AudioRoot = "Audio/Nightfall/";

    private Component simulation;
    private readonly Dictionary<string, FieldInfo> fieldCache = new Dictionary<string, FieldInfo>();
    private AudioSource musicSource;
    private AudioSource ambienceSource;
    private AudioSource sfxSource;

    private AudioClip musicClip;
    private AudioClip ambienceClip;
    private AudioClip shootClip;
    private AudioClip hitClip;
    private AudioClip bossBurstClip;
    private AudioClip levelUpClip;
    private AudioClip shardClip;
    private AudioClip selectClip;
    private AudioClip resultClip;
    private AudioClip uiClickClip;
    private AudioClip uiRolloverClip;

    private string lastMode = string.Empty;
    private int lastLevel;
    private int lastXp;
    private int lastKills;
    private int lastChests;
    private int lastProjectileCount;
    private int lastEnemyProjectileCount;
    private int lastEffectCount;
    private float lastHealth;
    private bool lastBossActive;
    private float shootCooldown;
    private float hitCooldown;
    private float effectCooldown;
    private float bossCooldown;
    private bool attached;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (GameObject.Find("Nightfall Audio") == null)
        {
            new GameObject("Nightfall Audio").AddComponent<NightfallAudio>();
        }
    }

    private void Awake()
    {
        musicClip = LoadClip("Nightfall_BGM");
        ambienceClip = LoadClip("Nightfall_Ambience");
        shootClip = LoadClip("SFX_Shoot");
        hitClip = LoadClip("SFX_Hit");
        bossBurstClip = LoadClip("SFX_BossBurst");
        levelUpClip = LoadClip("SFX_LevelUp");
        shardClip = LoadClip("SFX_Shard");
        selectClip = LoadClip("SFX_Select");
        resultClip = LoadClip("SFX_Result");
        uiClickClip = LoadClip("SFX_UI_Click");
        uiRolloverClip = LoadClip("SFX_UI_Rollover");

        musicSource = CreateSource("Nightfall Music", 0.24f, true);
        ambienceSource = CreateSource("Night Meadow Ambience", 0.11f, true);
        sfxSource = CreateSource("Nightfall SFX", 0.62f, false);

        if (musicClip != null)
        {
            musicSource.clip = musicClip;
            musicSource.Play();
        }
        if (ambienceClip != null)
        {
            ambienceSource.clip = ambienceClip;
            ambienceSource.Play();
        }
    }

    private AudioClip LoadClip(string name)
    {
        return Resources.Load<AudioClip>(AudioRoot + name);
    }

    private AudioSource CreateSource(string sourceName, float volume, bool loop)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = volume;
        return source;
    }

    private void Update()
    {
        if (!attached)
        {
            TryAttach();
            return;
        }

        shootCooldown = Mathf.Max(0f, shootCooldown - Time.unscaledDeltaTime);
        hitCooldown = Mathf.Max(0f, hitCooldown - Time.unscaledDeltaTime);
        effectCooldown = Mathf.Max(0f, effectCooldown - Time.unscaledDeltaTime);
        bossCooldown = Mathf.Max(0f, bossCooldown - Time.unscaledDeltaTime);

        string mode = GetString("mode");
        if (!string.Equals(mode, lastMode)) HandleModeChange(lastMode, mode);

        int level = GetInt("level");
        int xp = GetInt("xp");
        int kills = GetInt("kills");
        int chests = GetInt("chestsOpened");
        int projectiles = GetListCount("projectiles");
        int enemyProjectiles = GetListCount("enemyProjectiles");
        int effects = GetListCount("effects");
        float health = GetFloat("playerHealth");
        bool bossActive = GetBool("bossActive");

        if (projectiles > lastProjectileCount && shootCooldown <= 0f)
        {
            PlaySfx(shootClip, 0.10f);
            shootCooldown = 0.13f;
        }
        if (enemyProjectiles > lastEnemyProjectileCount && shootCooldown <= 0f)
        {
            PlaySfx(shootClip, 0.075f);
            shootCooldown = 0.16f;
        }
        if (effects > lastEffectCount && effectCooldown <= 0f)
        {
            PlaySfx(hitClip, 0.075f);
            effectCooldown = 0.12f;
        }
        if (health < lastHealth - 0.1f && hitCooldown <= 0f)
        {
            PlaySfx(hitClip, 0.16f);
            hitCooldown = 0.45f;
        }
        if (xp > lastXp) PlaySfx(shardClip, 0.10f);
        if (level > lastLevel) PlaySfx(levelUpClip, 0.24f);
        if (kills > lastKills && hitCooldown <= 0f) PlaySfx(hitClip, 0.11f);
        if (chests > lastChests) PlaySfx(levelUpClip, 0.13f);
        if (bossActive && !lastBossActive && bossCooldown <= 0f)
        {
            PlaySfx(bossBurstClip, 0.32f);
            bossCooldown = 1.0f;
        }

        lastMode = mode;
        lastLevel = level;
        lastXp = xp;
        lastKills = kills;
        lastChests = chests;
        lastProjectileCount = projectiles;
        lastEnemyProjectileCount = enemyProjectiles;
        lastEffectCount = effects;
        lastHealth = health;
        lastBossActive = bossActive;
    }

    private void TryAttach()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour == this || behaviour.GetType().Name != "PixelSurvivorGame") continue;
            simulation = behaviour;
            attached = true;
            lastMode = GetString("mode");
            lastLevel = GetInt("level");
            lastXp = GetInt("xp");
            lastKills = GetInt("kills");
            lastChests = GetInt("chestsOpened");
            lastProjectileCount = GetListCount("projectiles");
            lastEnemyProjectileCount = GetListCount("enemyProjectiles");
            lastEffectCount = GetListCount("effects");
            lastHealth = GetFloat("playerHealth");
            lastBossActive = GetBool("bossActive");
            break;
        }
    }

    private void HandleModeChange(string previousMode, string nextMode)
    {
        if (nextMode == "LevelUp")
        {
            PlaySfx(levelUpClip, 0.24f);
            PlaySfx(uiRolloverClip, 0.06f);
            PauseMusic();
        }
        else if (nextMode == "Paused")
        {
            PauseMusic();
        }
        else if (nextMode == "Won" || nextMode == "Lost")
        {
            PlaySfx(resultClip, 0.22f);
            PauseMusic();
        }
        else if (nextMode == "Playing")
        {
            PlaySfx(previousMode == "LevelUp" ? selectClip : uiClickClip, 0.14f);
            ResumeMusic();
        }
        else if (nextMode == "Menu")
        {
            ResumeMusic();
        }
    }

    private void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying) musicSource.Pause();
        if (ambienceSource != null && ambienceSource.isPlaying) ambienceSource.Pause();
    }

    private void ResumeMusic()
    {
        if (musicSource != null && !musicSource.isPlaying) musicSource.UnPause();
        if (ambienceSource != null && !ambienceSource.isPlaying) ambienceSource.UnPause();
    }

    private void PlaySfx(AudioClip clip, float volume)
    {
        if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip, volume);
    }

    private FieldInfo GetField(string name)
    {
        FieldInfo field;
        if (fieldCache.TryGetValue(name, out field)) return field;
        field = simulation == null ? null : simulation.GetType().GetField(name, PrivateInstance);
        fieldCache[name] = field;
        return field;
    }

    private object GetValue(string name)
    {
        FieldInfo field = GetField(name);
        return field == null ? null : field.GetValue(simulation);
    }

    private string GetString(string name)
    {
        object value = GetValue(name);
        return value == null ? string.Empty : value.ToString();
    }

    private int GetInt(string name)
    {
        object value = GetValue(name);
        return value is int ? (int)value : 0;
    }

    private float GetFloat(string name)
    {
        object value = GetValue(name);
        return value is float ? (float)value : 0f;
    }

    private bool GetBool(string name)
    {
        object value = GetValue(name);
        return value is bool && (bool)value;
    }

    private int GetListCount(string name)
    {
        object value = GetValue(name);
        IList list = value as IList;
        return list == null ? 0 : list.Count;
    }
}
