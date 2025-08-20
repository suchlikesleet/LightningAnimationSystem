using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LightningAnimation
{
    /// <summary>
    /// High-performance animation system using Unity's Playables API
    /// Provides direct control over animation playback without Animator Controller overhead
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("Lightning Animation/Playable Animation Controller")]
    public class PlayableAnimationController : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Settings")]
        [SerializeField, Tooltip("Automatically stop animations when they complete (non-looping only)")]
        private bool autoStopOnComplete = true;
        
        [SerializeField, Tooltip("Maximum number of animations that can play simultaneously")]
        [Range(1, 8)]
        private int maxConcurrentAnimations = 4;
        
        [Header("Debug")]
        [SerializeField, Tooltip("Show debug information in console")]
        private bool enableDebugLogging = false;
        
        #endregion
        
        #region Private Fields
        
        // Core components
        private PlayableGraph playableGraph;
        private AnimationMixerPlayable mixerPlayable;
        private Animator animator;
        
        // Animation management
        private Dictionary<string, PlayableAnimationData> animationCache;
        private List<PlayableAnimationData> activeAnimations;
        private PlayableAnimationData currentAnimation;
        
        #endregion
        
        #region Events
        
        public event Action<string> OnAnimationEnd;
        public event Action<string> OnAnimationStart;
        
        #endregion
        
        #region Properties
        
        public bool AutoStopOnComplete => autoStopOnComplete;
        public int MaxConcurrentAnimations => maxConcurrentAnimations;
        public int ActiveAnimationCount => activeAnimations?.Count ?? 0;
        public int CachedAnimationCount => animationCache?.Count ?? 0;
        
        #endregion
        
        #region Animation Data Class
        
        private class PlayableAnimationData
        {
            public string name;
            public AnimationClipPlayable clipPlayable;
            public AnimationClip clip;
            public float length;
            public float currentTime;
            public float weight;
            public float targetWeight;
            public bool isPlaying;
            public bool isPaused;
            public bool isLooping;
            public Action onComplete;
            public int mixerIndex;
            
            // Blending
            public float blendSpeed = 5f;
            public bool isFadingIn;
            public bool isFadingOut;
            
            public void Reset()
            {
                currentTime = 0f;
                weight = 0f;
                targetWeight = 1f;
                isPlaying = false;
                isPaused = false;
                isFadingIn = false;
                isFadingOut = false;
                onComplete = null;
                blendSpeed = 5f;
            }
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            animator = GetComponent<Animator>();
            animationCache = new Dictionary<string, PlayableAnimationData>();
            activeAnimations = new List<PlayableAnimationData>();
            
            InitializePlayableGraph();
        }
        
        private void Update()
        {
            if (playableGraph.IsValid())
            {
                UpdateAnimations();
            }
        }
        
        private void OnDestroy()
        {
            CleanupPlayableGraph();
            CleanupEvents();
        }
        
        private void OnValidate()
        {
            maxConcurrentAnimations = Mathf.Clamp(maxConcurrentAnimations, 1, 8);
        }
        
        #endregion
        
        #region Initialization and Cleanup
        
        private void InitializePlayableGraph()
        {
            try
            {
                // Create the playable graph
                playableGraph = PlayableGraph.Create($"{gameObject.name}_LightningAnimation");
                playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                
                // Create mixer playable
                mixerPlayable = AnimationMixerPlayable.Create(playableGraph, maxConcurrentAnimations);
                
                // Create output and connect to animator
                var output = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);
                output.SetSourcePlayable(mixerPlayable);
                
                // Start the graph
                playableGraph.Play();
                
                DebugLog("Playable graph initialized successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize Playable Graph: {e.Message}", this);
            }
        }
        
        private void CleanupPlayableGraph()
        {
            if (playableGraph.IsValid())
            {
                playableGraph.Destroy();
                DebugLog("Playable graph destroyed");
            }
        }
        
        private void CleanupEvents()
        {
            OnAnimationEnd = null;
            OnAnimationStart = null;
        }
        
        #endregion
        
        #region Public API - Animation Management
        
        /// <summary>
        /// Add an animation clip to the system for later playback
        /// </summary>
        /// <param name="name">Unique name for the animation</param>
        /// <param name="clip">Animation clip to add</param>
        public void AddAnimation(string name, AnimationClip clip)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("Animation name cannot be null or empty!", this);
                return;
            }
            
            if (clip == null)
            {
                Debug.LogWarning($"Animation clip for '{name}' is null!", this);
                return;
            }
            
            if (animationCache.ContainsKey(name))
            {
                Debug.LogWarning($"Animation '{name}' already exists! Replacing...", this);
                RemoveAnimation(name);
            }
            
            var clipPlayable = AnimationClipPlayable.Create(playableGraph, clip);
            
            var animData = new PlayableAnimationData
            {
                name = name,
                clipPlayable = clipPlayable,
                clip = clip,
                length = clip.length,
                isLooping = clip.isLooping,
                mixerIndex = -1
            };
            
            animationCache[name] = animData;
            DebugLog($"Added animation: {name} (Length: {clip.length:F2}s, Looping: {clip.isLooping})");
        }
        
        /// <summary>
        /// Remove an animation from the system
        /// </summary>
        /// <param name="name">Name of animation to remove</param>
        public void RemoveAnimation(string name)
        {
            if (!animationCache.ContainsKey(name)) return;
            
            var animData = animationCache[name];
            
            // Stop if currently playing
            if (animData.isPlaying)
            {
                StopInternal(animData);
            }
            
            // Dispose playable
            if (animData.clipPlayable.IsValid())
            {
                animData.clipPlayable.Destroy();
            }
            
            animationCache.Remove(name);
            DebugLog($"Removed animation: {name}");
        }
        
        /// <summary>
        /// Get count of cached animations
        /// </summary>
        public int GetAnimationCount()
        {
            return animationCache.Count;
        }
        
        /// <summary>
        /// Get all animation names
        /// </summary>
        public string[] GetAnimationNames()
        {
            var names = new string[animationCache.Count];
            animationCache.Keys.CopyTo(names, 0);
            return names;
        }
        
        #endregion
        
        #region Public API - Playback Control
        
        /// <summary>
        /// Play animation by name
        /// </summary>
        /// <param name="animationName">Name of animation to play</param>
        /// <param name="onComplete">Optional callback when animation completes</param>
        public void Play(string animationName, Action onComplete = null)
        {
            if (!ValidateAnimationName(animationName)) return;
            
            var animData = animationCache[animationName];
            PlayInternal(animData, onComplete, 1f, 0f);
        }
        
        /// <summary>
        /// Play animation by clip reference (auto-adds if not cached)
        /// </summary>
        /// <param name="clip">Animation clip to play</param>
        /// <param name="onComplete">Optional callback when animation completes</param>
        public void Play(AnimationClip clip, Action onComplete = null)
        {
            if (clip == null)
            {
                Debug.LogWarning("Cannot play null animation clip!", this);
                return;
            }
            
            // Auto-add if not cached
            if (!animationCache.ContainsKey(clip.name))
            {
                AddAnimation(clip.name, clip);
            }
            
            Play(clip.name, onComplete);
        }
        
        /// <summary>
        /// Play animation with crossfade transition
        /// </summary>
        /// <param name="animationName">Name of animation to play</param>
        /// <param name="fadeTime">Duration of crossfade in seconds</param>
        /// <param name="onComplete">Optional callback when animation completes</param>
        public void PlayWithCrossfade(string animationName, float fadeTime = 0.3f, Action onComplete = null)
        {
            if (!ValidateAnimationName(animationName)) return;
            
            var animData = animationCache[animationName];
            
            // Fade out current animation
            if (currentAnimation != null && currentAnimation.isPlaying && currentAnimation != animData)
            {
                FadeOut(currentAnimation, fadeTime);
            }
            
            // Fade in new animation
            PlayInternal(animData, onComplete, 0f, fadeTime);
            FadeIn(animData, fadeTime);
        }
        
        /// <summary>
        /// Stop specific animation
        /// </summary>
        /// <param name="animationName">Name of animation to stop</param>
        public void Stop(string animationName)
        {
            if (!animationCache.ContainsKey(animationName)) return;
            
            var animData = animationCache[animationName];
            StopInternal(animData);
        }
        
        /// <summary>
        /// Stop all animations
        /// </summary>
        public void StopAll()
        {
            for (int i = activeAnimations.Count - 1; i >= 0; i--)
            {
                StopInternal(activeAnimations[i]);
            }
        }
        
        /// <summary>
        /// Pause specific animation
        /// </summary>
        /// <param name="animationName">Name of animation to pause</param>
        public void Pause(string animationName)
        {
            if (!animationCache.ContainsKey(animationName)) return;
            
            var animData = animationCache[animationName];
            if (animData.isPlaying && !animData.isPaused)
            {
                animData.isPaused = true;
                DebugLog($"Paused animation: {animationName}");
            }
        }
        
        /// <summary>
        /// Resume specific animation
        /// </summary>
        /// <param name="animationName">Name of animation to resume</param>
        public void Resume(string animationName)
        {
            if (!animationCache.ContainsKey(animationName)) return;
            
            var animData = animationCache[animationName];
            if (animData.isPlaying && animData.isPaused)
            {
                animData.isPaused = false;
                DebugLog($"Resumed animation: {animationName}");
            }
        }
        
        #endregion
        
        #region Public API - Speed Control
        
        /// <summary>
        /// Set playback speed for specific animation
        /// </summary>
        /// <param name="animationName">Name of animation</param>
        /// <param name="speed">Playback speed multiplier</param>
        public void SetSpeed(string animationName, float speed)
        {
            if (!animationCache.ContainsKey(animationName)) return;
            
            var animData = animationCache[animationName];
            animData.clipPlayable.SetSpeed(speed);
        }
        
        /// <summary>
        /// Set global playback speed for all animations
        /// </summary>
        /// <param name="speed">Global speed multiplier</param>
        public void SetGlobalSpeed(float speed)
        {
            if (playableGraph.IsValid())
            {
                playableGraph.GetRootPlayable(0).SetSpeed(speed);
            }
        }
        
        /// <summary>
        /// Get current global speed
        /// </summary>
        public float GetGlobalSpeed()
        {
            if (playableGraph.IsValid())
            {
                return (float)playableGraph.GetRootPlayable(0).GetSpeed();
            }
            return 1f;
        }
        
        #endregion
        
        #region Public API - Query Methods
        
        /// <summary>
        /// Check if a specific animation is currently playing
        /// </summary>
        /// <param name="animationName">Name of animation to check</param>
        public bool IsPlaying(string animationName)
        {
            return animationCache.ContainsKey(animationName) && 
                   animationCache[animationName].isPlaying;
        }
        
        /// <summary>
        /// Check if any animation is currently playing
        /// </summary>
        public bool IsPlaying()
        {
            return currentAnimation != null && currentAnimation.isPlaying;
        }
        
        /// <summary>
        /// Get the name of the currently playing primary animation
        /// </summary>
        public string GetCurrentAnimation()
        {
            return currentAnimation?.name;
        }
        
        /// <summary>
        /// Get the length of an animation in seconds
        /// </summary>
        /// <param name="animationName">Name of animation</param>
        public float GetAnimationLength(string animationName)
        {
            return animationCache.ContainsKey(animationName) ? 
                   animationCache[animationName].length : 0f;
        }
        
        /// <summary>
        /// Get normalized time (0-1) of an animation
        /// </summary>
        /// <param name="animationName">Name of animation</param>
        public float GetNormalizedTime(string animationName)
        {
            if (!animationCache.ContainsKey(animationName)) return 0f;
            
            var animData = animationCache[animationName];
            return animData.length > 0 ? animData.currentTime / animData.length : 0f;
        }
        
        /// <summary>
        /// Check if an animation exists in the cache
        /// </summary>
        /// <param name="animationName">Name of animation to check</param>
        public bool HasAnimation(string animationName)
        {
            return animationCache.ContainsKey(animationName);
        }
        
        /// <summary>
        /// Check if animation is paused
        /// </summary>
        /// <param name="animationName">Name of animation to check</param>
        public bool IsPaused(string animationName)
        {
            return animationCache.ContainsKey(animationName) && 
                   animationCache[animationName].isPaused;
        }
        
        #endregion
        
        #region Internal Implementation
        
        private bool ValidateAnimationName(string animationName)
        {
            if (string.IsNullOrEmpty(animationName))
            {
                Debug.LogWarning("Animation name cannot be null or empty!", this);
                return false;
            }
            
            if (!animationCache.ContainsKey(animationName))
            {
                Debug.LogWarning($"Animation '{animationName}' not found! Use AddAnimation() first.", this);
                return false;
            }
            
            return true;
        }
        
        private void PlayInternal(PlayableAnimationData animData, Action onComplete, float initialWeight, float fadeTime)
        {
            // Stop if already playing
            if (animData.isPlaying)
            {
                StopInternal(animData);
            }
            
            // Find or assign mixer slot
            if (animData.mixerIndex == -1)
            {
                animData.mixerIndex = GetAvailableMixerSlot();
                if (animData.mixerIndex == -1)
                {
                    Debug.LogWarning("No available mixer slots! Consider increasing maxConcurrentAnimations.", this);
                    return;
                }
            }
            
            // Connect to mixer
            ConnectToMixer(animData);
            
            // Setup animation
            animData.Reset();
            animData.clipPlayable.SetTime(0);
            animData.clipPlayable.SetDone(false);
            animData.weight = initialWeight;
            animData.targetWeight = 1f;
            animData.isPlaying = true;
            animData.onComplete = onComplete;
            
            // Set mixer weight
            mixerPlayable.SetInputWeight(animData.mixerIndex, animData.weight);
            
            // Add to active list
            if (!activeAnimations.Contains(animData))
            {
                activeAnimations.Add(animData);
            }
            
            currentAnimation = animData;
            
            DebugLog($"Started playing: {animData.name}");
            OnAnimationStart?.Invoke(animData.name);
        }
        
        private void ConnectToMixer(PlayableAnimationData animData)
        {
            if (animData.mixerIndex >= 0)
            {
                // Always disconnect first, then reconnect to ensure clean state
                DisconnectFromMixer(animData.mixerIndex);
                playableGraph.Connect(animData.clipPlayable, 0, mixerPlayable, animData.mixerIndex);
            }
        }
        
        private void DisconnectFromMixer(int mixerIndex)
        {
            if (mixerIndex >= 0 && mixerIndex < maxConcurrentAnimations)
            {
                try
                {
                    playableGraph.Disconnect(mixerPlayable, mixerIndex);
                }
                catch (System.Exception)
                {
                    // Ignore disconnect errors - may already be disconnected
                }
            }
        }
        
        private void StopInternal(PlayableAnimationData animData)
        {
            if (!animData.isPlaying) return;
            
            animData.isPlaying = false;
            animData.isPaused = false;
            animData.isFadingIn = false;
            animData.isFadingOut = false;
            
            if (animData.mixerIndex >= 0)
            {
                mixerPlayable.SetInputWeight(animData.mixerIndex, 0f);
                DisconnectFromMixer(animData.mixerIndex);
                animData.mixerIndex = -1;
            }
            
            activeAnimations.Remove(animData);
            
            if (currentAnimation == animData)
            {
                currentAnimation = activeAnimations.Count > 0 ? activeAnimations[0] : null;
            }
            
            DebugLog($"Stopped animation: {animData.name}");
        }
        
        private void FadeIn(PlayableAnimationData animData, float fadeTime)
        {
            animData.weight = 0f;
            animData.targetWeight = 1f;
            animData.blendSpeed = fadeTime > 0 ? 1f / fadeTime : float.MaxValue;
            animData.isFadingIn = true;
            
            if (animData.mixerIndex >= 0)
            {
                mixerPlayable.SetInputWeight(animData.mixerIndex, 0f);
            }
        }
        
        private void FadeOut(PlayableAnimationData animData, float fadeTime)
        {
            animData.targetWeight = 0f;
            animData.blendSpeed = fadeTime > 0 ? 1f / fadeTime : float.MaxValue;
            animData.isFadingOut = true;
        }
        
        private int GetAvailableMixerSlot()
        {
            for (int i = 0; i < maxConcurrentAnimations; i++)
            {
                bool slotTaken = false;
                foreach (var anim in activeAnimations)
                {
                    if (anim.mixerIndex == i)
                    {
                        slotTaken = true;
                        break;
                    }
                }
                if (!slotTaken) return i;
            }
            return -1;
        }
        
        private void UpdateAnimations()
        {
            float deltaTime = Time.deltaTime;
            
            for (int i = activeAnimations.Count - 1; i >= 0; i--)
            {
                var animData = activeAnimations[i];
                
                if (!animData.isPlaying) continue;
                
                // Update blending
                UpdateBlending(animData, deltaTime);
                
                // Update time if not paused
                if (!animData.isPaused)
                {
                    animData.currentTime += deltaTime;
                    
                    // Check for completion (non-looping animations only)
                    if (!animData.isLooping && animData.currentTime >= animData.length)
                    {
                        DebugLog($"Animation completed: {animData.name}");
                        
                        OnAnimationEnd?.Invoke(animData.name);
                        animData.onComplete?.Invoke();
                        
                        if (autoStopOnComplete)
                        {
                            StopInternal(animData);
                        }
                    }
                }
            }
        }
        
        private void UpdateBlending(PlayableAnimationData animData, float deltaTime)
        {
            bool needsUpdate = false;
            
            if (animData.isFadingIn)
            {
                animData.weight = Mathf.MoveTowards(animData.weight, animData.targetWeight, 
                    animData.blendSpeed * deltaTime);
                if (animData.weight >= animData.targetWeight)
                {
                    animData.isFadingIn = false;
                }
                needsUpdate = true;
            }
            
            if (animData.isFadingOut)
            {
                animData.weight = Mathf.MoveTowards(animData.weight, animData.targetWeight, 
                    animData.blendSpeed * deltaTime);
                if (animData.weight <= animData.targetWeight)
                {
                    animData.isFadingOut = false;
                    if (animData.targetWeight <= 0f)
                    {
                        StopInternal(animData);
                    }
                }
                needsUpdate = true;
            }
            
            if (needsUpdate && animData.mixerIndex >= 0)
            {
                mixerPlayable.SetInputWeight(animData.mixerIndex, animData.weight);
            }
        }
        
        private void DebugLog(string message)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"[Lightning Animation] {message}", this);
            }
        }
        
        #endregion
    }
}