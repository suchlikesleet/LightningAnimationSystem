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
        #region Enums
        
        public enum PlayMode
        {
            Single,      // Stop all other animations
            Additive,    // Play alongside other animations
            Queue        // Queue after current animation
        }
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Settings")]
        [SerializeField, Tooltip("Automatically stop animations when they complete (non-looping only)")]
        private bool autoStopOnComplete = true;
        
        [SerializeField, Tooltip("Maximum number of animations that can play simultaneously")]
        [Range(1, 8)]
        private int maxConcurrentAnimations = 4;
        
        [SerializeField, Tooltip("Stop all animations when none are playing to save performance")]
        private bool autoOptimizeWhenIdle = true;
        
        [Header("Editor Setup")]
        [SerializeField, Tooltip("Animations to automatically load at startup")]
        private AnimationClip[] editorAnimations = new AnimationClip[0];
        
        [Header("Debug")]
        [SerializeField, Tooltip("Show debug information in console")]
        private bool enableDebugLogging = false;
        
        #endregion
        
        #region Private Fields
        
        // Core components
        private PlayableGraph playableGraph;
        private AnimationMixerPlayable mixerPlayable;
        private Animator animator;
        private bool graphInitialized = false;
        
        // Animation management
        private Dictionary<string, PlayableAnimationData> animationCache;
        private List<PlayableAnimationData> activeAnimations;
        private Queue<QueuedAnimation> animationQueue;
        private PlayableAnimationData currentAnimation;
        
        // Performance
        private float lastActiveTime;
        private bool isGraphPaused = false;
        
        #endregion
        
        #region Events
        
        public event Action<string> OnAnimationStart;
        public event Action<string> OnAnimationEnd;
        public event Action<string> OnAnimationInterrupted;
        public event Action<string, int> OnAnimationLoop; // animation name, loop count
        
        #endregion
        
        #region Properties
        
        public bool AutoStopOnComplete => autoStopOnComplete;
        public int MaxConcurrentAnimations => maxConcurrentAnimations;
        public int ActiveAnimationCount => activeAnimations?.Count ?? 0;
        public int CachedAnimationCount => animationCache?.Count ?? 0;
        public int QueuedAnimationCount => animationQueue?.Count ?? 0;
        public bool IsAnyAnimationPlaying => activeAnimations?.Count > 0;
        
        #endregion
        
        #region Animation Data Classes
        
        private class PlayableAnimationData
        {
            public string name;
            public AnimationClipPlayable clipPlayable;
            public AnimationClip clip;
            public float length;
            public float currentTime;
            public float weight;
            public float targetWeight;
            public float speed = 1f;
            public bool isPlaying;
            public bool isPaused;
            public bool isLooping;
            public int loopCount;
            public int maxLoops = -1; // -1 = infinite
            public Action onComplete;
            public Action onInterrupted;
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
                speed = 1f;
                isPlaying = false;
                isPaused = false;
                isFadingIn = false;
                isFadingOut = false;
                loopCount = 0;
                maxLoops = -1;
                onComplete = null;
                onInterrupted = null;
                blendSpeed = 5f;
            }
            
            public void Cleanup()
            {
                onComplete = null;
                onInterrupted = null;
            }
        }
        
        private class QueuedAnimation
        {
            public string name;
            public Action onComplete;
            public float fadeTime;
            
            public QueuedAnimation(string name, Action onComplete, float fadeTime)
            {
                this.name = name;
                this.onComplete = onComplete;
                this.fadeTime = fadeTime;
            }
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            animator = GetComponent<Animator>();
            animationCache = new Dictionary<string, PlayableAnimationData>();
            activeAnimations = new List<PlayableAnimationData>();
            animationQueue = new Queue<QueuedAnimation>();
            
            InitializePlayableGraph();
            LoadEditorAnimations();
        }
        
        private void Update()
        {
            if (!graphInitialized || isGraphPaused) return;
            
            if (activeAnimations.Count > 0)
            {
                UpdateAnimations();
                lastActiveTime = Time.time;
            }
            else if (autoOptimizeWhenIdle && Time.time - lastActiveTime > 0.5f)
            {
                PauseGraph();
            }
        }
        
        private void OnDestroy()
        {
            CleanupAllAnimations();
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
                
                // Create mixer playable with proper input count
                mixerPlayable = AnimationMixerPlayable.Create(playableGraph, maxConcurrentAnimations);
                
                // Create output and connect to animator
                var output = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);
                output.SetSourcePlayable(mixerPlayable);
                
                // Start the graph
                playableGraph.Play();
                graphInitialized = true;
                
                DebugLog("Playable graph initialized successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Lightning Animation] Failed to initialize Playable Graph: {e.Message}", this);
                graphInitialized = false;
            }
        }
        
        private void LoadEditorAnimations()
        {
            if (editorAnimations == null || editorAnimations.Length == 0) return;
            
            int loadedCount = 0;
            foreach (var clip in editorAnimations)
            {
                if (clip != null)
                {
                    AddAnimation(clip.name, clip);
                    loadedCount++;
                }
            }
            
            if (loadedCount > 0)
            {
                DebugLog($"Loaded {loadedCount} editor animations");
            }
        }
        
        private void CleanupPlayableGraph()
        {
            if (playableGraph.IsValid())
            {
                playableGraph.Destroy();
                graphInitialized = false;
                DebugLog("Playable graph destroyed");
            }
        }
        
        private void CleanupAllAnimations()
        {
            // Clean up all animation data
            foreach (var animData in animationCache.Values)
            {
                animData.Cleanup();
                if (animData.clipPlayable.IsValid())
                {
                    animData.clipPlayable.Destroy();
                }
            }
            
            animationCache.Clear();
            activeAnimations.Clear();
            animationQueue.Clear();
            currentAnimation = null;
        }
        
        private void CleanupEvents()
        {
            OnAnimationStart = null;
            OnAnimationEnd = null;
            OnAnimationInterrupted = null;
            OnAnimationLoop = null;
        }
        
        private void PauseGraph()
        {
            if (graphInitialized && !isGraphPaused)
            {
                playableGraph.Stop();
                isGraphPaused = true;
                DebugLog("Graph paused for optimization");
            }
        }
        
        private void ResumeGraph()
        {
            if (graphInitialized && isGraphPaused)
            {
                playableGraph.Play();
                isGraphPaused = false;
                DebugLog("Graph resumed");
            }
        }
        
        #endregion
        
        #region Public API - Animation Management
        
        /// <summary>
        /// Add an animation clip to the system for later playback
        /// </summary>
        public void AddAnimation(string name, AnimationClip clip)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("[Lightning Animation] Animation name cannot be null or empty!", this);
                return;
            }
            
            if (clip == null)
            {
                Debug.LogWarning($"[Lightning Animation] Animation clip for '{name}' is null!", this);
                return;
            }
            
            if (!graphInitialized)
            {
                Debug.LogError("[Lightning Animation] Graph not initialized!", this);
                return;
            }
            
            if (animationCache.ContainsKey(name))
            {
                DebugLog($"Animation '{name}' already exists! Replacing...");
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
        public void RemoveAnimation(string name)
        {
            if (!animationCache.ContainsKey(name)) return;
            
            var animData = animationCache[name];
            
            // Stop if currently playing
            if (animData.isPlaying)
            {
                StopInternal(animData, true);
            }
            
            // Cleanup
            animData.Cleanup();
            
            // Dispose playable
            if (animData.clipPlayable.IsValid())
            {
                animData.clipPlayable.Destroy();
            }
            
            animationCache.Remove(name);
            DebugLog($"Removed animation: {name}");
        }
        
        /// <summary>
        /// Clear all animations
        /// </summary>
        public void ClearAllAnimations()
        {
            StopAll();
            CleanupAllAnimations();
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
        /// Play animation by name (stops current animation)
        /// </summary>
        public void Play(string animationName, Action onComplete = null)
        {
            PlayWithMode(animationName, PlayMode.Single, onComplete);
        }
        
        /// <summary>
        /// Play animation by clip reference (auto-adds if not cached)
        /// </summary>
        public void Play(AnimationClip clip, Action onComplete = null)
        {
            if (clip == null)
            {
                Debug.LogWarning("[Lightning Animation] Cannot play null animation clip!", this);
                return;
            }
            
            if (!animationCache.ContainsKey(clip.name))
            {
                AddAnimation(clip.name, clip);
            }
            
            Play(clip.name, onComplete);
        }
        
        /// <summary>
        /// Play animation with specific mode
        /// </summary>
        public void PlayWithMode(string animationName, PlayMode mode, Action onComplete = null)
        {
            if (!ValidateAnimationName(animationName)) return;
            
            var animData = animationCache[animationName];
            
            switch (mode)
            {
                case PlayMode.Single:
                    // Stop current animation first
                    if (currentAnimation != null && currentAnimation != animData && currentAnimation.isPlaying)
                    {
                        StopInternal(currentAnimation, true);
                    }
                    PlayInternal(animData, onComplete, 1f, 0f);
                    break;
                    
                case PlayMode.Additive:
                    // Play alongside current animations
                    PlayInternal(animData, onComplete, 1f, 0f);
                    break;
                    
                case PlayMode.Queue:
                    // Queue after current animation
                    if (currentAnimation != null && currentAnimation.isPlaying)
                    {
                        animationQueue.Enqueue(new QueuedAnimation(animationName, onComplete, 0f));
                        DebugLog($"Queued animation: {animationName}");
                    }
                    else
                    {
                        PlayInternal(animData, onComplete, 1f, 0f);
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Play animation with crossfade transition
        /// </summary>
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
        /// Play animation with loop control
        /// </summary>
        public void PlayLooped(string animationName, int loopCount = -1, Action onComplete = null)
        {
            if (!ValidateAnimationName(animationName)) return;
            
            var animData = animationCache[animationName];
            animData.maxLoops = loopCount;
            animData.isLooping = true; // Force looping
            
            Play(animationName, onComplete);
        }
        
        /// <summary>
        /// Stop specific animation
        /// </summary>
        public void Stop(string animationName)
        {
            if (!animationCache.ContainsKey(animationName)) return;
            
            var animData = animationCache[animationName];
            StopInternal(animData, false);
        }
        
        /// <summary>
        /// Stop all animations
        /// </summary>
        public void StopAll()
        {
            for (int i = activeAnimations.Count - 1; i >= 0; i--)
            {
                StopInternal(activeAnimations[i], false);
            }
            
            animationQueue.Clear();
        }
        
        /// <summary>
        /// Pause specific animation
        /// </summary>
        public void Pause(string animationName)
        {
            if (!animationCache.ContainsKey(animationName)) return;
            
            var animData = animationCache[animationName];
            if (animData.isPlaying && !animData.isPaused)
            {
                animData.isPaused = true;
                animData.clipPlayable.SetSpeed(0f);
                DebugLog($"Paused animation: {animationName}");
            }
        }
        
        /// <summary>
        /// Resume specific animation
        /// </summary>
        public void Resume(string animationName)
        {
            if (!animationCache.ContainsKey(animationName)) return;
            
            var animData = animationCache[animationName];
            if (animData.isPlaying && animData.isPaused)
            {
                animData.isPaused = false;
                animData.clipPlayable.SetSpeed(animData.speed);
                DebugLog($"Resumed animation: {animationName}");
            }
        }
        
        /// <summary>
        /// Pause all animations
        /// </summary>
        public void PauseAll()
        {
            foreach (var anim in activeAnimations)
            {
                if (anim.isPlaying && !anim.isPaused)
                {
                    anim.isPaused = true;
                    anim.clipPlayable.SetSpeed(0f);
                }
            }
        }
        
        /// <summary>
        /// Resume all animations
        /// </summary>
        public void ResumeAll()
        {
            foreach (var anim in activeAnimations)
            {
                if (anim.isPlaying && anim.isPaused)
                {
                    anim.isPaused = false;
                    anim.clipPlayable.SetSpeed(anim.speed);
                }
            }
        }
        
        #endregion
        
        #region Public API - Speed Control
        
        /// <summary>
        /// Set playback speed for specific animation
        /// </summary>
        public void SetSpeed(string animationName, float speed)
        {
            if (!animationCache.ContainsKey(animationName)) return;
            
            var animData = animationCache[animationName];
            animData.speed = Mathf.Max(0f, speed);
            
            if (!animData.isPaused)
            {
                animData.clipPlayable.SetSpeed(animData.speed);
            }
        }
        
        /// <summary>
        /// Set global playback speed for all animations
        /// </summary>
        public void SetGlobalSpeed(float speed)
        {
            if (playableGraph.IsValid())
            {
                playableGraph.GetRootPlayable(0).SetSpeed(Mathf.Max(0f, speed));
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
            return activeAnimations.Count > 0;
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
        public float GetAnimationLength(string animationName)
        {
            return animationCache.ContainsKey(animationName) ? 
                   animationCache[animationName].length : 0f;
        }
        
        /// <summary>
        /// Get normalized time (0-1) of an animation
        /// </summary>
        public float GetNormalizedTime(string animationName)
        {
            if (!animationCache.ContainsKey(animationName)) return 0f;
            
            var animData = animationCache[animationName];
            return animData.length > 0 ? (animData.currentTime % animData.length) / animData.length : 0f;
        }
        
        /// <summary>
        /// Get current time in seconds of an animation
        /// </summary>
        public float GetCurrentTime(string animationName)
        {
            if (!animationCache.ContainsKey(animationName)) return 0f;
            return animationCache[animationName].currentTime;
        }
        
        /// <summary>
        /// Check if an animation exists in the cache
        /// </summary>
        public bool HasAnimation(string animationName)
        {
            return animationCache.ContainsKey(animationName);
        }
        
        /// <summary>
        /// Check if animation is paused
        /// </summary>
        public bool IsPaused(string animationName)
        {
            return animationCache.ContainsKey(animationName) && 
                   animationCache[animationName].isPaused;
        }
        
        /// <summary>
        /// Get current loop count of an animation
        /// </summary>
        public int GetLoopCount(string animationName)
        {
            if (!animationCache.ContainsKey(animationName)) return 0;
            return animationCache[animationName].loopCount;
        }
        
        #endregion
        
        #region Internal Implementation
        
        private bool ValidateAnimationName(string animationName)
        {
            if (string.IsNullOrEmpty(animationName))
            {
                Debug.LogWarning("[Lightning Animation] Animation name cannot be null or empty!", this);
                return false;
            }
            
            if (!animationCache.ContainsKey(animationName))
            {
                Debug.LogWarning($"[Lightning Animation] Animation '{animationName}' not found! Use AddAnimation() first.", this);
                return false;
            }
            
            if (!graphInitialized)
            {
                Debug.LogError("[Lightning Animation] Graph not initialized!", this);
                return false;
            }
            
            return true;
        }
        
        private void PlayInternal(PlayableAnimationData animData, Action onComplete, float initialWeight, float fadeTime)
        {
            // Resume graph if paused
            ResumeGraph();
            
            // Stop if already playing
            if (animData.isPlaying)
            {
                StopInternal(animData, true);
            }
            
            // Find or assign mixer slot
            if (animData.mixerIndex == -1)
            {
                animData.mixerIndex = GetAvailableMixerSlot();
                if (animData.mixerIndex == -1)
                {
                    // Try to free up a slot by stopping the oldest animation
                    if (activeAnimations.Count >= maxConcurrentAnimations)
                    {
                        StopInternal(activeAnimations[0], true);
                        animData.mixerIndex = GetAvailableMixerSlot();
                    }
                    
                    if (animData.mixerIndex == -1)
                    {
                        Debug.LogWarning("[Lightning Animation] No available mixer slots!", this);
                        return;
                    }
                }
            }
            
            // Connect to mixer
            ConnectToMixer(animData);
            
            // Setup animation
            animData.Reset();
            animData.clipPlayable.SetTime(0);
            animData.clipPlayable.SetDone(false);
            animData.clipPlayable.SetSpeed(animData.speed);
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
            if (animData.mixerIndex >= 0 && animData.mixerIndex < maxConcurrentAnimations)
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
                catch
                {
                    // Ignore disconnect errors - may already be disconnected
                }
            }
        }
        
        private void StopInternal(PlayableAnimationData animData, bool wasInterrupted)
        {
            if (!animData.isPlaying) return;
            
            // Fire interrupted callback if applicable
            if (wasInterrupted && animData.onInterrupted != null)
            {
                animData.onInterrupted.Invoke();
                OnAnimationInterrupted?.Invoke(animData.name);
            }
            
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
            
            // Clean up callbacks
            animData.Cleanup();
            
            DebugLog($"Stopped animation: {animData.name} (Interrupted: {wasInterrupted})");
            
            // Process queued animations
            ProcessQueue();
        }
        
        private void ProcessQueue()
        {
            if (animationQueue.Count > 0 && (currentAnimation == null || !currentAnimation.isPlaying))
            {
                var queued = animationQueue.Dequeue();
                if (queued.fadeTime > 0)
                {
                    PlayWithCrossfade(queued.name, queued.fadeTime, queued.onComplete);
                }
                else
                {
                    Play(queued.name, queued.onComplete);
                }
            }
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
            animData.onInterrupted = animData.onComplete; // Preserve callback for interrupted animation
            animData.onComplete = null; // Clear complete callback since it's being interrupted
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
                    animData.currentTime += deltaTime * animData.speed;
                    
                    // Check for loop or completion
                    if (animData.currentTime >= animData.length)
                    {
                        if (animData.isLooping)
                        {
                            // Handle looping
                            animData.loopCount++;
                            OnAnimationLoop?.Invoke(animData.name, animData.loopCount);
                            
                            // Check if we've reached max loops
                            if (animData.maxLoops > 0 && animData.loopCount >= animData.maxLoops)
                            {
                                // Stop looping, finish animation
                                DebugLog($"Animation {animData.name} completed {animData.loopCount} loops");
                                OnAnimationEnd?.Invoke(animData.name);
                                animData.onComplete?.Invoke();
                                
                                if (autoStopOnComplete)
                                {
                                    StopInternal(animData, false);
                                }
                            }
                            else
                            {
                                // Continue looping
                                animData.currentTime = animData.currentTime % animData.length;
                                animData.clipPlayable.SetTime(animData.currentTime);
                            }
                        }
                        else
                        {
                            // Non-looping animation completed
                            DebugLog($"Animation completed: {animData.name}");
                            
                            OnAnimationEnd?.Invoke(animData.name);
                            animData.onComplete?.Invoke();
                            
                            if (autoStopOnComplete)
                            {
                                StopInternal(animData, false);
                            }
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
                        StopInternal(animData, true);
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
        
        #region Editor Support
        
        /// <summary>
        /// Get debug information for editor display
        /// </summary>
        public string GetDebugInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine($"Active Animations: {ActiveAnimationCount}");
            info.AppendLine($"Cached Animations: {CachedAnimationCount}");
            info.AppendLine($"Queued Animations: {QueuedAnimationCount}");
            info.AppendLine($"Graph Status: {(graphInitialized ? (isGraphPaused ? "Paused" : "Active") : "Not Initialized")}");
            
            if (currentAnimation != null)
            {
                info.AppendLine($"Current: {currentAnimation.name}");
                info.AppendLine($"Progress: {GetNormalizedTime(currentAnimation.name):P}");
                info.AppendLine($"Loop Count: {currentAnimation.loopCount}");
            }
            
            return info.ToString();
        }
        
        #endregion
    }
}