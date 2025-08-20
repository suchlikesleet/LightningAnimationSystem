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
        private SerializedProperty enableDebugLogging;
        private SerializedProperty editorAnimations;
        
        private void OnEnable()
        {
            autoStopOnComplete = serializedObject.FindProperty("autoStopOnComplete");
            maxConcurrentAnimations = serializedObject.FindProperty("maxConcurrentAnimations");
            enableDebugLogging = serializedObject.FindProperty("enableDebugLogging");
            editorAnimations = serializedObject.FindProperty("editorAnimations");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var controller = (PlayableAnimationController)target;
            
            // Header
            EditorGUILayout.Space();
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("⚡ Lightning Animation System", headerStyle);
            EditorGUILayout.Space();
            
            // Settings
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(autoStopOnComplete);
            EditorGUILayout.PropertyField(maxConcurrentAnimations);
            EditorGUILayout.PropertyField(enableDebugLogging);
            
            EditorGUILayout.Space();
            
            // Editor Animations
            EditorGUILayout.LabelField("Editor Setup", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(editorAnimations);
            
            EditorGUILayout.Space();
            
            // Runtime Info
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);
                
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Current Animation", controller.GetCurrentAnimation() ?? "None");
                    EditorGUILayout.Toggle("Is Playing", controller.IsPlaying());
                    EditorGUILayout.IntField("Active Animations", controller.ActiveAnimationCount);
                    EditorGUILayout.IntField("Cached Animations", controller.CachedAnimationCount);
                    
                    string current = controller.GetCurrentAnimation();
                    if (!string.IsNullOrEmpty(current))
                    {
                        float progress = controller.GetNormalizedTime(current);
                        EditorGUILayout.Slider("Progress", progress, 0f, 1f);
                    }
                }
                
                EditorGUILayout.Space();
                
                // Control buttons
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Stop All"))
                    {
                        controller.StopAll();
                    }
                    
                    GUI.enabled = controller.IsPlaying();
                    if (GUILayout.Button("Pause"))
                    {
                        string currentAnim = controller.GetCurrentAnimation();
                        if (!string.IsNullOrEmpty(currentAnim))
                        {
                            controller.Pause(currentAnim);
                        }
                    }
                    
                    if (GUILayout.Button("Resume"))
                    {
                        string currentAnim = controller.GetCurrentAnimation();
                        if (!string.IsNullOrEmpty(currentAnim))
                        {
                            controller.Resume(currentAnim);
                        }
                    }
                    GUI.enabled = true;
                }
                
                // Animation list
                if (controller.CachedAnimationCount > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Available Animations", EditorStyles.boldLabel);
                    
                    string[] animNames = controller.GetAnimationNames();
                    foreach (string animName in animNames)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(animName);
                            
                            if (GUILayout.Button("Play", GUILayout.Width(50)))
                            {
                                controller.Play(animName);
                            }
                            
                            if (GUILayout.Button("Crossfade", GUILayout.Width(70)))
                            {
                                controller.PlayWithCrossfade(animName, 0.3f);
                            }
                        }
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Runtime information and controls available during play mode.", MessageType.Info);
            }
            
            // Performance tips
            EditorGUILayout.Space();
            if (GUILayout.Button("📖 Open Documentation"))
            {
                Application.OpenURL("https://github.com/yourusername/lightning-animation-system");
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Lightning Animation System v{LightningAnimationInfo.Version}", 
                EditorStyles.centeredGreyMiniLabel);
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}