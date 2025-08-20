using UnityEngine;
using UnityEditor;
using LightningAnimation;

namespace LightningAnimation.Editor
{
    public class LightningAnimationWindow : EditorWindow
    {
        private PlayableAnimationController selectedController;
        private Vector2 scrollPosition;
        
        [MenuItem("Window/Lightning Animation/Animation Controller")]
        public static void ShowWindow()
        {
            var window = GetWindow<LightningAnimationWindow>("Lightning Animation");
            window.titleContent = new GUIContent("⚡ Lightning Animation");
            window.Show();
        }
        
        private void OnGUI()
        {
            DrawHeader();
            DrawControllerSelection();
            
            if (selectedController != null)
            {
                DrawControllerInfo();
            }
            else
            {
                EditorGUILayout.HelpBox("Select a GameObject with PlayableAnimationController to see controls.", MessageType.Info);
            }
        }
        
        private void DrawHeader()
        {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("⚡ Lightning Animation System", headerStyle);
            EditorGUILayout.LabelField($"Version {LightningAnimationInfo.Version}", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();
        }
        
        private void DrawControllerSelection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Controller:", GUILayout.Width(70));
            
            var newController = EditorGUILayout.ObjectField(selectedController, typeof(PlayableAnimationController), true) 
                as PlayableAnimationController;
            
            if (newController != selectedController)
            {
                selectedController = newController;
            }
            
            if (GUILayout.Button("Auto-Select", GUILayout.Width(80)))
            {
                if (Selection.activeGameObject != null)
                {
                    selectedController = Selection.activeGameObject.GetComponent<PlayableAnimationController>();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }
        
        private void DrawControllerInfo()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Status
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Current", selectedController.GetCurrentAnimation() ?? "None");
                EditorGUILayout.Toggle("Playing", selectedController.IsPlaying());
                EditorGUILayout.IntField("Active Count", selectedController.ActiveAnimationCount);
                EditorGUILayout.IntField("Cache Count", selectedController.CachedAnimationCount);
            }
            
            EditorGUILayout.Space();
            
            // Controls
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Stop All"))
                {
                    selectedController.StopAll();
                }
                
                GUI.enabled = selectedController.IsPlaying();
                if (GUILayout.Button("Pause Current"))
                {
                    string current = selectedController.GetCurrentAnimation();
                    if (!string.IsNullOrEmpty(current))
                        selectedController.Pause(current);
                }
                
                if (GUILayout.Button("Resume Current"))
                {
                    string current = selectedController.GetCurrentAnimation();
                    if (!string.IsNullOrEmpty(current))
                        selectedController.Resume(current);
                }
                GUI.enabled = true;
            }
            
            EditorGUILayout.Space();
            
            // Speed Control
            EditorGUILayout.LabelField("Speed Control", EditorStyles.boldLabel);
            float currentSpeed = selectedController.GetGlobalSpeed();
            float newSpeed = EditorGUILayout.Slider("Global Speed", currentSpeed, 0.1f, 3f);
            if (Mathf.Abs(newSpeed - currentSpeed) > 0.01f)
            {
                selectedController.SetGlobalSpeed(newSpeed);
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("0.5x")) selectedController.SetGlobalSpeed(0.5f);
                if (GUILayout.Button("1x")) selectedController.SetGlobalSpeed(1f);
                if (GUILayout.Button("1.5x")) selectedController.SetGlobalSpeed(1.5f);
                if (GUILayout.Button("2x")) selectedController.SetGlobalSpeed(2f);
            }
            
            EditorGUILayout.Space();
            
            // Animation List
            if (selectedController.CachedAnimationCount > 0)
            {
                EditorGUILayout.LabelField("Available Animations", EditorStyles.boldLabel);
                
                string[] animNames = selectedController.GetAnimationNames();
                foreach (string animName in animNames)
                {
                    DrawAnimationRow(animName);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawAnimationRow(string animName)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                bool isPlaying = selectedController.IsPlaying(animName);
                string status = isPlaying ? "▶️" : "⏸️";
                
                EditorGUILayout.LabelField($"{status} {animName}", GUILayout.MinWidth(100));
                
                if (isPlaying)
                {
                    float progress = selectedController.GetNormalizedTime(animName);
                    EditorGUILayout.Slider(progress, 0f, 1f, GUILayout.Width(60));
                }
                else
                {
                    GUILayout.Space(64);
                }
                
                if (GUILayout.Button("Play", GUILayout.Width(50)))
                {
                    selectedController.Play(animName);
                }
                
                if (GUILayout.Button("Fade", GUILayout.Width(50)))
                {
                    selectedController.PlayWithCrossfade(animName, 0.3f);
                }
                
                if (GUILayout.Button("Stop", GUILayout.Width(50)))
                {
                    selectedController.Stop(animName);
                }
            }
        }
        
        private void OnSelectionChange()
        {
            if (Selection.activeGameObject != null)
            {
                var controller = Selection.activeGameObject.GetComponent<PlayableAnimationController>();
                if (controller != null)
                {
                    selectedController = controller;
                    Repaint();
                }
            }
        }
    }
}