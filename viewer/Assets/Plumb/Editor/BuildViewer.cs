using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Plumb.Viewer.Editor
{
    /// <summary>
    /// Builds the viewer into <c>viewer-build/</c> at the repository root. Run from the menu or from the
    /// command line: <c>Unity -batchmode -quit -projectPath viewer -executeMethod Plumb.Viewer.Editor.BuildViewer.BuildMacOS</c>.
    /// </summary>
    public static class BuildViewer
    {
        private const string ScenePath = "Assets/Scenes/Viewer.unity";
        private const string ProductName = "Plumb Viewer";
        private const string IconPath = "Assets/Plumb/Icons/viewer-icon.png";

        // glTFast finds these by name at runtime; without this the build strips them and models render magenta.
        private static readonly string[] GltfShaders = { "glTF/PbrMetallicRoughness", "glTF/PbrSpecularGlossiness", "glTF/Unlit" };

        [MenuItem("Plumb/Build Viewer for macOS")]
        public static void BuildMacOS() => Build(BuildTarget.StandaloneOSX, "PlumbViewer.app");

        [MenuItem("Plumb/Build Viewer for Windows")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, Path.Combine("PlumbViewer", "PlumbViewer.exe"));

        private static void Build(BuildTarget target, string output)
        {
            EnsureScene();
            IncludeGltfShaders();
            ConfigurePlayer();

            var location = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "viewer-build", output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = location,
                target = target,
                options = BuildOptions.None,
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Viewer build {report.summary.result} with {report.summary.totalErrors} errors.");
            }

            Debug.Log($"Viewer built: {location}");
        }

        private static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static void IncludeGltfShaders()
        {
            var graphics = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            var settings = new SerializedObject(graphics);
            var included = settings.FindProperty("m_AlwaysIncludedShaders");

            var changed = false;
            foreach (var name in GltfShaders)
            {
                var shader = Shader.Find(name) ?? throw new BuildFailedException($"Shader {name} not found; is glTFast installed?");
                if (!Contains(included, shader))
                {
                    included.InsertArrayElementAtIndex(included.arraySize);
                    included.GetArrayElementAtIndex(included.arraySize - 1).objectReferenceValue = shader;
                    changed = true;
                }
            }

            // Saving only on a change keeps the committed GraphicsSettings.asset untouched by routine builds.
            if (changed)
            {
                settings.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }
        }

        private static bool Contains(SerializedProperty list, UnityEngine.Object item)
        {
            for (var i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == item)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = "Plumb";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "dev.plumb.viewer");
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 800;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            // The splash fades out over the first seconds of the scene and dims the overlay drawn on top of it.
            PlayerSettings.SplashScreen.show = false;

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath)
                ?? throw new BuildFailedException($"Viewer icon not found at {IconPath}; run tools/icons/render_icons.py.");

            // The default icon lives under Unknown; a Standalone override needs one texture per size and silently drops a single one.
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            if (PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any).FirstOrDefault() != icon)
            {
                throw new BuildFailedException($"Unity did not accept {IconPath} as the application icon.");
            }
        }
    }
}
