using UnityEngine;
using UnityEditor;
using LightningAnimation;

namespace LightningAnimation.Editor
{
    [CustomEditor(typeof(PlayableAnimationController))]
    public class PlayableAnimationControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty autoStopOnComplete;
        private SerializedProperty maxConcurrentAnimations;
        private SerializedProperty autoOptimizeWhenIdle;
        private SerializedProperty enableDebugLogging;
        private SerializedProperty editorAnimations;
        
        // Editor state
        private bool showSettings = true;
        private bool showEditorSetup = true;
        private bool showRuntimeInfo = true;
        private bool showAnimationList = true;
        private string testAnimationName = "";
        
        private void OnEnable()
        {
            // Find serialized properties - with null checks
            autoStopOnComplete = serializedObject.FindProperty("autoStopOnComplete");
            maxConcurrentAnimations = serializedObject.FindProperty("maxConcurrentAnimations");
            autoOptimizeWhenIdle = serializedObject.FindProperty("autoOptimizeWhenIdle");
            enableDebugLogging = serializedObject.FindProperty("enableDebugLogging");
            editorAnimations = serializedObject.FindProperty("editorAnimations");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var controller = (PlayableAnimationController)target;
            
            // Header
            DrawHeader();
            
            // Settings Section
            showSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showSettings, "Settings");
            if (showSettings)
            {
                DrawSettingsSection();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            EditorGUILayout.Space();
            
            // Editor Setup Section
            showEditorSetup = EditorGUILayout.BeginFoldoutHeaderGroup(showEditorSetup, "Editor Setup");
            if (showEditorSetup)
            {
                DrawEditorSetupSection();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            EditorGUILayout.Space();
            
            // Runtime Info Section (only in play mode)
            if (Application.isPlaying)
            {
                showRuntimeInfo = EditorGUILayout.BeginFoldoutHeaderGroup(showRuntimeInfo, "Runtime Information");
                if (showRuntimeInfo)
                {
                    DrawRuntimeInfoSection(controller);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                
                EditorGUILayout.Space();
                
                // Animation List Section
                if (controller.CachedAnimationCount > 0)
                {
                    showAnimationList = EditorGUILayout.BeginFoldoutHeaderGroup(showAnimationList, $"Available Animations ({controller.CachedAnimationCount})");
                    if (showAnimationList)
                    {
                        DrawAnimationListSection(controller);
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
                
                EditorGUILayout.Space();
                
                // Test Controls
                DrawTestControls(controller);
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see runtime information and controls.", MessageType.Info);
            }
            
            // Footer
            DrawFooter();
            
            serializedObject.ApplyModifiedProperties();
            
            // Repaint in play mode for live updates
            if (Application.isPlaying && controller.IsPlaying())
            {
                Repaint();
            }
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space();
            
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("⚡ Lightning Animation System", headerStyle);
            EditorGUILayout.LabelField($"Version {LightningAnimationInfo.Version}", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.Space();
        }
        
        private void DrawSettingsSection()
        {
            EditorGUI.indentLevel++;
            
            if (autoStopOnComplete != null)
                EditorGUILayout.PropertyField(autoStopOnComplete, new GUIContent("Auto Stop On Complete", "Automatically stop animations when they complete (non-looping only)"));
            
            if (maxConcurrentAnimations != null)
                EditorGUILayout.PropertyField(maxConcurrentAnimations, new GUIContent("Max Concurrent Animations", "Maximum number of animations that can play simultaneously"));
            
            if (autoOptimizeWhenIdle != null)
                EditorGUILayout.PropertyField(autoOptimizeWhenIdle, new GUIContent("Auto Optimize When Idle", "Pause the playable graph when no animations are playing to save performance"));
            
            if (enableDebugLogging != null)
                EditorGUILayout.PropertyField(enableDebugLogging, new GUIContent("Enable Debug Logging", "Show debug information in console"));
            
            EditorGUI.indentLevel--;
        }
        
        private void DrawEditorSetupSection()
        {
            EditorGUI.indentLevel++;
            
            if (editorAnimations != null)
            {
                EditorGUILayout.PropertyField(editorAnimations, new GUIContent("Preload Animations", "Animations to automatically load at startup"), true);
            }
            else
            {
                EditorGUILayout.HelpBox("Editor animations property not found. Please check the script.", MessageType.Warning);
            }
            
            EditorGUI.indentLevel--;
        }
        
        private void DrawRuntimeInfoSection(PlayableAnimationController controller)
        {
            EditorGUI.indentLevel++;
            
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Current Animation", controller.GetCurrentAnimation() ?? "None");
                EditorGUILayout.Toggle("Is Playing", controller.IsPlaying());
                EditorGUILayout.IntField("Active Animations", controller.ActiveAnimationCount);
                EditorGUILayout.IntField("Cached Animations", controller.CachedAnimationCount);
                EditorGUILayout.IntField("Queued Animations", controller.QueuedAnimationCount);
                
                string current = controller.GetCurrentAnimation();
                if (!string.IsNullOrEmpty(current))
                {
                    float progress = controller.GetNormalizedTime(current);
                    EditorGUILayout.Slider("Progress", progress, 0f, 1f);
                    
                    int loopCount = controller.GetLoopCount(current);
                    if (loopCount > 0)
                    {
                        EditorGUILayout.IntField("Loop Count", loopCount);
                    }
                    
                    float currentTime = controller.GetCurrentTime(current);
                    float length = controller.GetAnimationLength(current);
                    EditorGUILayout.LabelField("Time", $"{currentTime:F2}s / {length:F2}s");
                }
                
                float globalSpeed = controller.GetGlobalSpeed();
                EditorGUILayout.Slider("Global Speed", globalSpeed, 0f, 3f);
            }
            
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space();
            
            // Control buttons
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Stop All"))
                {
                    controller.StopAll();
                }
                
                GUI.enabled = controller.IsPlaying();
                
                if (GUILayout.Button("Pause All"))
                {
                    controller.PauseAll();
                }
                
                if (GUILayout.Button("Resume All"))
                {
                    controller.ResumeAll();
                }
                
                GUI.enabled = true;
            }
            
            // Speed control buttons
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Speed:", GUILayout.Width(50));
                
                if (GUILayout.Button("0.5x"))
                {
                    controller.SetGlobalSpeed(0.5f);
                }
                
                if (GUILayout.Button("1x"))
                {
                    controller.SetGlobalSpeed(1f);
                }
                
                if (GUILayout.Button("1.5x"))
                {
                    controller.SetGlobalSpeed(1.5f);
                }
                
                if (GUILayout.Button("2x"))
                {
                    controller.SetGlobalSpeed(2f);
                }
            }
        }
        
        private void DrawAnimationListSection(PlayableAnimationController controller)
        {
            string[] animNames = controller.GetAnimationNames();
            
            EditorGUI.indentLevel++;
            
            foreach (string animName in animNames)
            {
                DrawAnimationRow(controller, animName);
            }
            
            EditorGUI.indentLevel--;
        }
        
        private void DrawAnimationRow(PlayableAnimationController controller, string animName)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                bool isPlaying = controller.IsPlaying(animName);
                bool isPaused = controller.IsPaused(animName);
                
                // Status icon
                string status = isPlaying ? (isPaused ? "⏸️" : "▶️") : "⏹️";
                EditorGUILayout.LabelField(status, GUILayout.Width(20));
                
                // Animation name
                EditorGUILayout.LabelField(animName, GUILayout.MinWidth(100));
                
                // Progress bar
                if (isPlaying)
                {
                    float progress = controller.GetNormalizedTime(animName);
                    var rect = GUILayoutUtility.GetRect(60, 18);
                    EditorGUI.ProgressBar(rect, progress, $"{(progress * 100):F0}%");
                }
                else
                {
                    GUILayout.Space(64);
                }
                
                // Control buttons
                if (!isPlaying)
                {
                    if (GUILayout.Button("Play", GUILayout.Width(50)))
                    {
                        controller.Play(animName);
                    }
                }
                else
                {
                    if (isPaused)
                    {
                        if (GUILayout.Button("Resume", GUILayout.Width(50)))
                        {
                            controller.Resume(animName);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Pause", GUILayout.Width(50)))
                        {
                            controller.Pause(animName);
                        }
                    }
                }
                
                if (GUILayout.Button("Fade", GUILayout.Width(45)))
                {
                    controller.PlayWithCrossfade(animName, 0.3f);
                }
                
                GUI.enabled = isPlaying;
                if (GUILayout.Button("Stop", GUILayout.Width(45)))
                {
                    controller.Stop(animName);
                }
                GUI.enabled = true;
            }
        }
        
        private void DrawTestControls(PlayableAnimationController controller)
        {
            EditorGUILayout.LabelField("Test Controls", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                testAnimationName = EditorGUILayout.TextField("Animation Name:", testAnimationName);
                
                GUI.enabled = !string.IsNullOrEmpty(testAnimationName) && controller.HasAnimation(testAnimationName);
                
                if (GUILayout.Button("Play", GUILayout.Width(50)))
                {
                    controller.Play(testAnimationName, () => Debug.Log($"Animation '{testAnimationName}' completed!"));
                }
                
                if (GUILayout.Button("Queue", GUILayout.Width(50)))
                {
                    controller.PlayWithMode(testAnimationName, PlayableAnimationController.PlayMode.Queue);
                }
                
                if (GUILayout.Button("Loop x3", GUILayout.Width(60)))
                {
                    controller.PlayLooped(testAnimationName, 3, () => Debug.Log($"Animation '{testAnimationName}' finished 3 loops!"));
                }
                
                GUI.enabled = true;
            }
            
            if (!string.IsNullOrEmpty(testAnimationName) && !controller.HasAnimation(testAnimationName))
            {
                EditorGUILayout.HelpBox($"Animation '{testAnimationName}' not found in cache.", MessageType.Warning);
            }
        }
        
        private void DrawFooter()
        {
            EditorGUILayout.Space();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("📖 Documentation"))
                {
                    Application.OpenURL("https://github.com/yourusername/lightning-animation-system");
                }
                
                if (GUILayout.Button("🔧 Open Editor Window"))
                {
                    LightningAnimationWindow.ShowWindow();
                }
                
                if (GUILayout.Button("🔍 Validate Package"))
                {
                    PackageValidator.ValidatePackage();
                }
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Lightning Animation System v{LightningAnimationInfo.Version}", 
                EditorStyles.centeredGreyMiniLabel);
        }
    }
}