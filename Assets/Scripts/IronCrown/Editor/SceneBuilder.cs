#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IronCrown.Editor
{
    [InitializeOnLoad]
    public static class SceneBuilder
    {
        private const string SceneFolder = "Assets/Scenes";
        private const string ResourcesFolder = "Assets/Resources";
        private const string BaseMaterialAssetPath = "Assets/Resources/IronCrownBaseMat.mat";
        public const string ScenePath = "Assets/Scenes/IronCrownBattleRoyale.unity";

        static SceneBuilder()
        {
            EditorApplication.delayCall += EnsureSceneLoadedInInteractiveEditor;
        }

        private static void EnsureSceneLoadedInInteractiveEditor()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!File.Exists(ScenePath))
            {
                CreateAndSavePlayableScene();
            }
            else if (SceneManager.GetActiveScene().path != ScenePath && Object.FindFirstObjectByType<GameMatchManager>() == null)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }

        private static Material EnsureBaseMaterialAsset()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            Shader litShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Resources/IronCrownLit.shader");
            if (litShader == null) litShader = Shader.Find("Custom/IronCrownLit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(BaseMaterialAssetPath);
            if (mat == null)
            {
                mat = new Material(litShader);
                mat.name = "IronCrownBaseMat";
                mat.enableInstancing = true;
                AssetDatabase.CreateAsset(mat, BaseMaterialAssetPath);
            }
            else
            {
                mat.shader = litShader;
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
            }

            AssetDatabase.SaveAssets();
            return mat;
        }

        [MenuItem("Iron Crown/Build & Open Playable Scene", priority = 1)]
        public static void CreateAndSavePlayableScene()
        {
            if (!AssetDatabase.IsValidFolder(SceneFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            Material baseMat = EnsureBaseMaterialAsset();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Directional Golden-Hour Sunlight
            GameObject sunGo = new GameObject("GoldenHourSunLight");
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.93f, 0.80f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(46f, -38f, 0f);

            // 2. Main Camera (positioned overlooking Highkeep Citadel for Scene View clarity)
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 1500f;
            cam.fieldOfView = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.52f, 0.65f, 0.78f);
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 65f, -35f);
            camGo.transform.LookAt(new Vector3(0f, 28f, 95f));

            // 3. Serialized Shader Reference Anchor (prevents Unity from stripping IronCrownLit in standalone .exe builds)
            GameObject anchor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            anchor.name = "ShaderReferenceAnchor";
            anchor.transform.position = new Vector3(0f, -60f, 0f);
            anchor.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            if (baseMat != null)
            {
                anchor.GetComponent<MeshRenderer>().sharedMaterial = baseMat;
            }

            // 4. GameMatchManager Root + Editor Scene View Island Preview
            GameObject managerGo = new GameObject("IronCrown_GameMatchManager");
            GameMatchManager manager = managerGo.AddComponent<GameMatchManager>();
            manager.SelectedLobbySize = LobbySize.Lobby50;
            manager.SelectedDifficulty = BotDifficulty.Knight;
            manager.SelectedControlLayout = ControlLayoutMode.Auto;

            GameObject worldGo = new GameObject("IslandWorldGenerator");
            worldGo.transform.SetParent(managerGo.transform, false);
            worldGo.AddComponent<IslandWorldGenerator>();

            PlayerSettings.companyName = "IronCrownStudios";
            PlayerSettings.productName = "Iron Crown: Medieval Battle Royale";

            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[IronCrown] Successfully created and saved playable scene at: " + ScenePath);
        }

        [MenuItem("Iron Crown/Build Standalone Windows (.exe)", priority = 2)]
        public static void BuildStandaloneWindowsExe()
        {
            CreateAndSavePlayableScene();

            string buildDir = Path.Combine(Directory.GetCurrentDirectory(), "Build");
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            string exePath = Path.Combine(buildDir, "IronCrownRoyale.exe");
            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[IronCrown] Standalone build result: " + report.summary.result + " -> " + exePath);
        }

        public static void BuildAndVerifyProject()
        {
            CreateAndSavePlayableScene();
            BuildStandaloneWindowsExe();
        }
    }
}
#endif
