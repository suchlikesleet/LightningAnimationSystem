//Step 7: Package Validation and Export

//7.1 Create Package Validator (`Editor/Scripts/PackageValidator.cs`)
using UnityEngine;
using UnityEditor;
using System.IO;
using LightningAnimation;

namespace LightningAnimation.Editor
{
    public class PackageValidator : EditorWindow
    {
        [MenuItem("Lightning Animation/Validate Package")]
        public static void ValidatePackage()
        {
            bool isValid = true;
            int errors = 0;
            int warnings = 0;
            
            Debug.Log("🔍 Validating Lightning Animation System Package...");
            
            // Check folder structure
            string packagePath = "Assets/LightningAnimationSystem";
            if (!AssetDatabase.IsValidFolder(packagePath))
            {
                Debug.LogError("❌ Main package folder missing!");
                isValid = false;
                errors++;
            }
            
            // Check required files
            string[] requiredFiles = {
                "package.json",
                "README.md",
                "LICENSE",
                "CHANGELOG.md",
                "Runtime/LightningAnimationSystem.asmdef",
                "Runtime/Scripts/PlayableAnimationController.cs",
                "Runtime/Scripts/PlayableAnimationExtensions.cs"
            };
            
            foreach (string file in requiredFiles)
            {
                string fullPath = Path.Combine(packagePath, file);
                if (!File.Exists(fullPath))
                {
                    Debug.LogError($"❌ Required file missing: {file}");
                    isValid = false;
                    errors++;
                }
            }
            
            // Check assembly compilation
            try
            {
                var assembly = System.Reflection.Assembly.Load("LightningAnimationSystem");
                if (assembly == null)
                {
                    Debug.LogError("❌ Runtime assembly not found!");
                    isValid = false;
                    errors++;
                }
                else
                {
                    Debug.Log("✅ Runtime assembly compiled successfully");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Assembly compilation failed: {e.Message}");
                isValid = false;
                errors++;
            }
            
            // Check for sample scenes
            if (!AssetDatabase.IsValidFolder(packagePath + "/Samples~"))
            {
                Debug.LogWarning("⚠️ Samples folder missing - recommended for package");
                warnings++;
            }
            
            // Check package.json format
            string packageJsonPath = Path.Combine(packagePath, "package.json");
            if (File.Exists(packageJsonPath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(packageJsonPath);
                    JsonUtility.FromJson<object>(jsonContent);
                    Debug.Log("✅ package.json format valid");
                }
                catch
                {
                    Debug.LogError("❌ package.json format invalid!");
                    isValid = false;
                    errors++;
                }
            }
            
            // Final result
            Debug.Log($"\n📊 Validation Results:");
            Debug.Log($"Errors: {errors}");
            Debug.Log($"Warnings: {warnings}");
            
            if (isValid)
            {
                Debug.Log("🎉 Package validation PASSED! Ready for distribution.");
            }
            else
            {
                Debug.LogError("❌ Package validation FAILED! Fix errors before distribution.");
            }
        }
        
        [MenuItem("Lightning Animation/Export Package")]
        public static void ExportPackage()
        {
            string packagePath = "Assets/LightningAnimationSystem";
            string exportPath = EditorUtility.SaveFilePanel(
                "Export Lightning Animation System",
                "",
                $"LightningAnimationSystem-v{LightningAnimationInfo.Version}.unitypackage",
                "unitypackage"
            );
            
            if (!string.IsNullOrEmpty(exportPath))
            {
                AssetDatabase.ExportPackage(packagePath, exportPath, ExportPackageOptions.Recurse);
                Debug.Log($"📦 Package exported to: {exportPath}");
                EditorUtility.RevealInFinder(exportPath);
            }
        }
    }
}