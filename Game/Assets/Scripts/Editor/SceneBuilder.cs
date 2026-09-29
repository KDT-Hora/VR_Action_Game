using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VrAction.Game.Game;

namespace VrAction.Game.EditorTools
{
    /// <summary>Creates the three scenes (Sandbox, Tabletop, Room) and registers them in Build Settings.</summary>
    public static class SceneBuilder
    {
        [MenuItem("VrAction/Create Scenes")]
        public static void CreateScenes()
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            var list = new List<EditorBuildSettingsScene>();
            foreach (var src in new[] { GameBootstrap.Source.Sandbox, GameBootstrap.Source.Tabletop, GameBootstrap.Source.Room })
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var light = new GameObject("Directional Light");
                light.AddComponent<Light>().type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
                var b = new GameObject("Bootstrap").AddComponent<GameBootstrap>();
                b.SourceMode = src;
                string path = "Assets/Scenes/" + src + ".unity";
                EditorSceneManager.SaveScene(scene, path);
                list.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
        }
    }
}
