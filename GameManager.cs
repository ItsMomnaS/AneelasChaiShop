using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;
using System.Collections;
using UnityEngine.Video;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public RecipeDatabase recipeDB;
    private SpriteRenderer character;
    public float cFadeDuration = 0.5f;

    private DialogueRunner runner;
    public VideoPlayer videoPlayer;

    // Background Music System
    [Header("Background Music")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;
    [Tooltip("Scenes where background music should play")]
    public string[] musicScenes = {"Story", "Tea_making", "Minigame"};
    [Tooltip("Scenes where music should be muted/stopped")]
    public string[] silentScenes = {"Menu"};
    
    private AudioSource musicSource;
    private bool musicEnabled = true;

    // This string controls which Yarn node is triggered after scene load
    private string queuedDialogueNode = "";

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // RE-ENABLED FOR SCENE TRANSITIONS
        DontDestroyOnLoad(gameObject);

        // Initial attempt to assign runner — will reassign again after scene load
        runner = FindAnyObjectByType<DialogueRunner>();

        // Initialize background music
        InitializeBackgroundMusic();
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (videoPlayer != null)
            videoPlayer.loopPointReached += OnVideoFinished;

        // Start playing music if we're in a music scene
        CheckAndControlMusic();
    }

    // Initialize the background music AudioSource
    private void InitializeBackgroundMusic()
    {
        // Get existing AudioSource or create new one
        AudioSource[] audioSources = GetComponents<AudioSource>();
        
        // Use second AudioSource for music if video player is using the first one
        if (audioSources.Length > 1)
        {
            musicSource = audioSources[1];
        }
        else if (audioSources.Length == 1 && videoPlayer != null)
        {
            // Add second AudioSource for music if video is using the first one
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            // Use existing or create new AudioSource
            musicSource = GetComponent<AudioSource>();
            if (musicSource == null)
                musicSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure the music source
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.playOnAwake = false; // We'll control when it plays
            musicSource.priority = 64; // Lower priority than sound effects
        }
        else
        {
            Debug.LogWarning("[GameManager] Background music clip not assigned!");
        }
    }

    // Check if current scene should have music and control playback
    private void CheckAndControlMusic()
    {
        if (musicSource == null || backgroundMusic == null || !musicEnabled) return;

        string currentScene = SceneManager.GetActiveScene().name;
        
        // Check if scene should be silent
        bool shouldBeSilent = System.Array.Exists(silentScenes, scene => scene == currentScene);
        if (shouldBeSilent)
        {
            if (musicSource.isPlaying)
            {
                FadeOutMusic(1f);
                Debug.Log($"[GameManager] Fading out music for silent scene: {currentScene}");
            }
            return;
        }

        // Check if scene should have music
        bool shouldPlayMusic = System.Array.Exists(musicScenes, scene => scene == currentScene);

        if (shouldPlayMusic && !musicSource.isPlaying)
        {
            FadeInMusic(1f);
            Debug.Log($"[GameManager] Starting background music for scene: {currentScene}");
        }
        else if (!shouldPlayMusic && musicSource.isPlaying)
        {
            FadeOutMusic(1f);
            Debug.Log($"[GameManager] Stopping background music for scene: {currentScene}");
        }
    }

    // Public methods to control music
    public void SetMusicEnabled(bool enabled)
    {
        musicEnabled = enabled;
        if (!enabled && musicSource != null && musicSource.isPlaying)
        {
            FadeOutMusic(0.5f);
        }
        else if (enabled)
        {
            CheckAndControlMusic();
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    public void FadeOutMusic(float duration = 1f)
    {
        if (musicSource != null && musicSource.isPlaying)
            StartCoroutine(FadeMusicCoroutine(musicSource.volume, 0f, duration, true));
    }

    public void FadeInMusic(float duration = 1f)
    {
        if (musicSource != null && backgroundMusic != null && musicEnabled)
        {
            if (!musicSource.isPlaying)
            {
                musicSource.volume = 0f;
                musicSource.Play();
            }
            StartCoroutine(FadeMusicCoroutine(musicSource.volume, musicVolume, duration, false));
        }
    }

    public void StopMusicImmediately()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
            Debug.Log("[GameManager] Music stopped immediately");
        }
    }

    public void PlayMusicImmediately()
    {
        if (musicSource != null && backgroundMusic != null && musicEnabled)
        {
            musicSource.volume = musicVolume;
            musicSource.Play();
            Debug.Log("[GameManager] Music started immediately");
        }
    }

    // Coroutine for smooth music fading
    private IEnumerator FadeMusicCoroutine(float fromVolume, float toVolume, float duration, bool stopAtEnd)
    {
        float elapsedTime = 0f;
        musicSource.volume = fromVolume;

        while (elapsedTime < duration)
        {
            musicSource.volume = Mathf.Lerp(fromVolume, toVolume, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        musicSource.volume = toVolume;

        if (stopAtEnd && toVolume <= 0f)
        {
            musicSource.Stop();
        }
    }

    // This function can be called from a Yarn command or UI button to load a new scene
    [YarnCommand("TransToScene")]
    public void TransToScene(string scene)
    {
        SceneTransitionManager.Instance.TransitionToScene(scene);
        Debug.Log($"Transitioning to scene: {scene}");
    }

    // Triggered when video finishes (like the intro trailer)
    private void OnVideoFinished(VideoPlayer vp)
    {
        if (vp == videoPlayer)
        {
            // FIXED: Updated scene name from "Minigame" to "Prequel"
            queuedDialogueNode = "Prequel";
            SceneTransitionManager.Instance.TransitionToScene("Minigame");
            Debug.Log("Intro video finished — transitioning to Prequel scene.");
        }
    }

    [YarnCommand("SetExpectedRecipe")]
    public void SetExpectedRecipe(string tea)
    {
        // Placeholder for logic to track expected recipe for Customer A
        // Could integrate with a future TeaManager script
        Debug.Log($"[SetExpectedRecipe] Placeholder command received for: {tea}");
    }

    // When a new scene is loaded, this hook assigns dialogue and character
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check music for the new scene
        CheckAndControlMusic();

        // Clean up duplicate event systems
        CleanupDuplicateEventSystems();

        // Set default dialogue nodes for each scene
        if (scene.name == "Minigame" && string.IsNullOrEmpty(queuedDialogueNode))
        {
            queuedDialogueNode = "Prequel";
        }
        else if (scene.name == "Story")
        {
            GameObject characterGO = GameObject.Find("Character");
            if (characterGO != null)
            {
                character = characterGO.GetComponent<SpriteRenderer>();
                // FORCE the correct dialogue for Story scene
                queuedDialogueNode = "AneelasFriend";
                Debug.Log("Story scene loaded - forcing AneelasFriend dialogue");
            }
            else
            {
                Debug.LogWarning("Character GameObject not found in Story scene");
            }
        }
        else if (scene.name == "Tea_making")
        {
            // Tea_making scene doesn't need dialogue - it uses instruction system
            queuedDialogueNode = null;
        }
        else if (scene.name == "Customer_Handoff")
        {
            GameObject characterGO = GameObject.Find("Character");
            if (characterGO != null)
                character = characterGO.GetComponent<SpriteRenderer>();
            else
                Debug.LogWarning("Character GameObject not found in Customer_Handoff scene");
            // Customer_Handoff scene uses queued dialogue
        }

        StartCoroutine(StartDialogueAfterSceneReady());
    }

    // Clean up duplicate event systems
    private void CleanupDuplicateEventSystems()
    {
        var eventSystems = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
        if (eventSystems.Length > 1)
        {
            Debug.Log($"[GameManager] Found {eventSystems.Length} EventSystems, removing duplicates");
            for (int i = 1; i < eventSystems.Length; i++)
            {
                if (eventSystems[i].gameObject != gameObject)
                {
                    Destroy(eventSystems[i].gameObject);
                }
            }
        }
    }

    private IEnumerator StartDialogueAfterSceneReady()
    {
        // Wait one frame to ensure scene objects are ready
        yield return null;

        runner = FindAnyObjectByType<DialogueRunner>();

        if (runner != null && !string.IsNullOrEmpty(queuedDialogueNode))
        {
            runner.StartDialogue(queuedDialogueNode);
            queuedDialogueNode = null;
        }
        else
        {
            Debug.LogWarning("DialogueRunner not found or queuedDialogueNode was null.");
        }
    }

    // Allows Yarn to queue a specific dialogue node before scene change
    [YarnCommand("QueueDialogueAfterScene")]
    public void QueueDialogueAfterScene(string nodeName)
    {
        queuedDialogueNode = nodeName;
        Debug.Log($"Queued dialogue node: {nodeName}");
    }

    // Yarn commands for music control
    [YarnCommand("StopMusic")]
    public void StopMusic()
    {
        FadeOutMusic(1f);
        Debug.Log("[GameManager] Music stopped via Yarn command");
    }

    [YarnCommand("StartMusic")]
    public void StartMusic()
    {
        CheckAndControlMusic();
        Debug.Log("[GameManager] Music started via Yarn command");
    }

    [YarnCommand("SetMusicVolume")]
    public void SetMusicVolumeYarn(float volume)
    {
        SetMusicVolume(volume);
        Debug.Log($"[GameManager] Music volume set to {volume} via Yarn command");
    }

    [YarnCommand("FadeOutMusic")]
    public void FadeOutMusicYarn(float duration = 2f)
    {
        FadeOutMusic(duration);
        Debug.Log($"[GameManager] Music fading out over {duration} seconds via Yarn command");
    }

    [YarnCommand("FadeInMusic")]
    public void FadeInMusicYarn(float duration = 2f)
    {
        FadeInMusic(duration);
        Debug.Log($"[GameManager] Music fading in over {duration} seconds via Yarn command");
    }

    // Loads a character sprite dynamically from Resources folder
    public void SetCharacter(string c)
    {
        if (character == null)
        {
            Debug.LogWarning("Character SpriteRenderer is null - cannot set character sprite");
            return;
        }

        Sprite sprite = Resources.Load<Sprite>($"Characters/{c}");

        if (sprite != null)
            character.sprite = sprite;
        else
            Debug.LogWarning($"Character sprite '{c}' not found in Resources/Characters.");
    }

    // Triggers a fade-in or fade-out for the character sprite
    [YarnCommand("SetCharacterVisible")]
    public void SetCharacterVisible(bool status)
    {
        if (character == null)
        {
            Debug.LogWarning("Character SpriteRenderer is null - cannot set visibility");
            return;
        }
        
        StartCoroutine(FadeCharacter(status));
    }

    private IEnumerator FadeCharacter(bool status)
    {
        if (character == null) yield break;

        float startAlpha = status ? 0f : 1f;
        float endAlpha = status ? 1f : 0f;
        float elapsedTime = 0f;

        if (status)
        {
            SetAlpha(0f);
            character.gameObject.SetActive(true);
        }

        while (elapsedTime < cFadeDuration)
        {
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / cFadeDuration);
            SetAlpha(alpha);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        SetAlpha(endAlpha);

        if (!status)
        {
            character.gameObject.SetActive(false);
        }
    }

    // Helper function to apply alpha transparency to character sprite
    private void SetAlpha(float alpha)
    {
        if (character == null) return;
        
        Color color = character.color;
        color.a = alpha;
        character.color = color;
    }

    // Cleanup when destroyed
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoFinished;
    }
}