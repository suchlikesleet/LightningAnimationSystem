using System;
using UnityEngine;

namespace LightningAnimation
{
    /// <summary>
    /// Extension methods for easier integration with GameObjects and Components
    /// </summary>
    public static class PlayableAnimationExtensions
    {
        #region GameObject Extensions
        
        /// <summary>
        /// Play animation on GameObject (requires PlayableAnimationController)
        /// </summary>
        public static void PlayAnimation(this GameObject gameObject, string animationName, Action onComplete = null)
        {
            var controller = gameObject.GetComponent<PlayableAnimationController>();
            if (controller != null)
            {
                controller.Play(animationName, onComplete);
            }
            else
            {
                Debug.LogWarning($"No PlayableAnimationController found on {gameObject.name}!", gameObject);
            }
        }
        
        /// <summary>
        /// Play animation by clip reference on GameObject
        /// </summary>
        public static void PlayAnimation(this GameObject gameObject, AnimationClip clip, Action onComplete = null)
        {
            var controller = gameObject.GetComponent<PlayableAnimationController>();
            if (controller != null)
            {
                controller.Play(clip, onComplete);
            }
            else
            {
                Debug.LogWarning($"No PlayableAnimationController found on {gameObject.name}!", gameObject);
            }
        }
        
        /// <summary>
        /// Play animation with crossfade on GameObject
        /// </summary>
        public static void PlayAnimationWithCrossfade(this GameObject gameObject, string animationName, 
            float fadeTime = 0.3f, Action onComplete = null)
        {
            var controller = gameObject.GetComponent<PlayableAnimationController>();
            if (controller != null)
            {
                controller.PlayWithCrossfade(animationName, fadeTime, onComplete);
            }
            else
            {
                Debug.LogWarning($"No PlayableAnimationController found on {gameObject.name}!", gameObject);
            }
        }
        
        /// <summary>
        /// Stop all animations on GameObject
        /// </summary>
        public static void StopAllAnimations(this GameObject gameObject)
        {
            var controller = gameObject.GetComponent<PlayableAnimationController>();
            controller?.StopAll();
        }
        
        /// <summary>
        /// Stop specific animation on GameObject
        /// </summary>
        public static void StopAnimation(this GameObject gameObject, string animationName)
        {
            var controller = gameObject.GetComponent<PlayableAnimationController>();
            controller?.Stop(animationName);
        }
        
        /// <summary>
        /// Check if GameObject is playing any animation
        /// </summary>
        public static bool IsPlayingAnimation(this GameObject gameObject)
        {
            var controller = gameObject.GetComponent<PlayableAnimationController>();
            return controller != null && controller.IsPlaying();
        }
        
        /// <summary>
        /// Check if GameObject is playing specific animation
        /// </summary>
        public static bool IsPlayingAnimation(this GameObject gameObject, string animationName)
        {
            var controller = gameObject.GetComponent<PlayableAnimationController>();
            return controller != null && controller.IsPlaying(animationName);
        }
        
        /// <summary>
        /// Get current animation name from GameObject
        /// </summary>
        public static string GetCurrentAnimation(this GameObject gameObject)
        {
            var controller = gameObject.GetComponent<PlayableAnimationController>();
            return controller?.GetCurrentAnimation();
        }
        
        #endregion
        
        #region Component Extensions
        
        /// <summary>
        /// Play animation on Component's GameObject
        /// </summary>
        public static void PlayAnimation(this Component component, string animationName, Action onComplete = null)
        {
            component.gameObject.PlayAnimation(animationName, onComplete);
        }
        
        /// <summary>
        /// Play animation by clip on Component's GameObject
        /// </summary>
        public static void PlayAnimation(this Component component, AnimationClip clip, Action onComplete = null)
        {
            component.gameObject.PlayAnimation(clip, onComplete);
        }
        
        /// <summary>
        /// Play animation with crossfade on Component's GameObject
        /// </summary>
        public static void PlayAnimationWithCrossfade(this Component component, string animationName, 
            float fadeTime = 0.3f, Action onComplete = null)
        {
            component.gameObject.PlayAnimationWithCrossfade(animationName, fadeTime, onComplete);
        }
        
        /// <summary>
        /// Stop all animations on Component's GameObject
        /// </summary>
        public static void StopAllAnimations(this Component component)
        {
            component.gameObject.StopAllAnimations();
        }
        
        /// <summary>
        /// Check if Component's GameObject is playing any animation
        /// </summary>
        public static bool IsPlayingAnimation(this Component component)
        {
            return component.gameObject.IsPlayingAnimation();
        }
        
        /// <summary>
        /// Check if Component's GameObject is playing specific animation
        /// </summary>
        public static bool IsPlayingAnimation(this Component component, string animationName)
        {
            return component.gameObject.IsPlayingAnimation(animationName);
        }
        
        #endregion
        
        #region Utility Extensions
        
        /// <summary>
        /// Get or add PlayableAnimationController to GameObject
        /// </summary>
        public static PlayableAnimationController GetOrAddAnimationController(this GameObject gameObject)
        {
            var controller = gameObject.GetComponent<PlayableAnimationController>();
            if (controller == null)
            {
                // Ensure Animator component exists first
                if (gameObject.GetComponent<Animator>() == null)
                {
                    gameObject.AddComponent<Animator>();
                }
                controller = gameObject.AddComponent<PlayableAnimationController>();
            }
            return controller;
        }
        
        /// <summary>
        /// Safely get PlayableAnimationController (returns null if not found)
        /// </summary>
        public static PlayableAnimationController GetAnimationController(this GameObject gameObject)
        {
            return gameObject.GetComponent<PlayableAnimationController>();
        }
        
        /// <summary>
        /// Check if GameObject has PlayableAnimationController
        /// </summary>
        public static bool HasAnimationController(this GameObject gameObject)
        {
            return gameObject.GetComponent<PlayableAnimationController>() != null;
        }
        
        /// <summary>
        /// Setup animation controller with clips
        /// </summary>
        public static PlayableAnimationController SetupAnimationController(this GameObject gameObject, 
            params AnimationClip[] clips)
        {
            var controller = gameObject.GetOrAddAnimationController();
            
            foreach (var clip in clips)
            {
                if (clip != null)
                {
                    controller.AddAnimation(clip.name, clip);
                }
            }
            
            return controller;
        }
        
        /// <summary>
        /// Setup animation controller with named clips
        /// </summary>
        public static PlayableAnimationController SetupAnimationController(this GameObject gameObject, 
            params (string name, AnimationClip clip)[] namedClips)
        {
            var controller = gameObject.GetOrAddAnimationController();
            
            foreach (var (name, clip) in namedClips)
            {
                if (clip != null && !string.IsNullOrEmpty(name))
                {
                    controller.AddAnimation(name, clip);
                }
            }
            
            return controller;
        }
        
        #endregion
    }
}