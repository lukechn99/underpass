using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Underpass.Editor
{
    // Cloud/local build entry points. No credentials or provider-specific code in gameplay.
    public sealed class TrialBuild : IPreprocessBuildWithReport
    {
        const string Scene = "Assets/Scenes/Trial.unity";
        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report) => Prepare();

        [MenuItem("Underpass/Prepare trial")]
        public static void Prepare()
        {
            PlayerSettings.companyName = "Underpass";
            PlayerSettings.productName = "Underpass";
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.lukechn99.underpass");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            MakeMaterial("Assets/Resources/TrialSolid.mat", "Standard", new Color(.2f,.3f,.4f));
            MakeMaterial("Assets/Resources/TrialUnlit.mat", "Unlit/Color", Color.yellow);
            AssetDatabase.SaveAssets();
        }

        static void MakeMaterial(string path, string shaderName, Color color)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException("Missing trial shader: " + shaderName);
            AssetDatabase.CreateAsset(new Material(shader) { color = color }, path);
        }

        [MenuItem("Underpass/Build Web trial")]
        public static void Web()
        {
            Prepare(); Directory.CreateDirectory("Build/Web");
            var report = BuildPipeline.BuildPlayer(new[] { Scene }, "Build/Web/index.html", BuildTarget.WebGL, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Web trial build failed: " + report.summary.result);
        }

        [MenuItem("Underpass/Export iOS trial")]
        public static void IOS()
        {
            Prepare();
            // Signing and upload belong to the configured cloud service/Xcode workflow.
            var report = BuildPipeline.BuildPlayer(new[] { Scene }, "Build/iOS", BuildTarget.iOS, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("iOS export failed: " + report.summary.result);
        }
    }
}
