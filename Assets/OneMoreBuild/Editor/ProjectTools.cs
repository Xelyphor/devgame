using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OneMoreBuild.Editor
{
    public static class ProjectTools
    {
        public const string ScenePath = "Assets/OneMoreBuild/Scenes/Studio.unity";
        [MenuItem("Ещё один билд/Открыть игровую сцену")]
        public static void OpenScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("Ещё один билд/Показать папку сохранений")]
        public static void OpenSaves() { EditorUtility.RevealInFinder(Application.persistentDataPath); }
        [MenuItem("Ещё один билд/Собрать для Linux")]
        public static void BuildLinux()
        {
            string folder = EditorUtility.SaveFolderPanel("Папка сборки", "", "OneMoreBuild");
            if (string.IsNullOrEmpty(folder)) return;
            var report = BuildPipeline.BuildPlayer(new[] { ScenePath }, Path.Combine(folder, "OneMoreBuild.x86_64"), BuildTarget.StandaloneLinux64, BuildOptions.None);
            Debug.Log("Linux build: " + report.summary.result);
        }
        // Command-line entry point when the actual Unity editor is available.
        public static void ValidateProject()
        {
            if (!File.Exists(ScenePath)) throw new System.Exception("Missing Studio scene");
            foreach (string name in new[] { "room", "developer0", "developer1", "developer2", "developer3", "upgrade0", "upgrade1", "upgrade2", "upgrade3", "upgrade4", "upgrade5" })
                if (Resources.Load<Texture2D>("Art/" + name) == null) throw new System.Exception("Missing art: " + name);
            Font font = Resources.Load<Font>("Fonts/DejaVuSans");
            if (font == null) throw new System.Exception("Missing Cyrillic font");
            foreach (char c in "Ещё один билд: Привет, мир! ёЁ")
                if (!font.HasCharacter(c)) throw new System.Exception("Missing glyph: " + c);
            if (!Simulation.IsValid(new GameState())) throw new System.Exception("Invalid initial state");
            Debug.Log("One More Build: project assets and initial state validated.");
        }
    }
}
