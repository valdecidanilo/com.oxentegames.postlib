using UnityEditor;
using UnityEngine;

namespace PostLib.Editor
{
    public class PostLibSettingsWindow : EditorWindow
    {
        private PostLibSettings settings;

        [MenuItem("Tools/PostLib/Settings")]
        private static void Open() =>
            GetWindow<PostLibSettingsWindow>("PostLib Settings");

        private void OnEnable()
        {
            settings = Resources.Load<PostLibSettings>("PostLibSettings");
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PostLibSettings>();
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateAsset(settings, "Assets/Resources/PostLibSettings.asset");
                AssetDatabase.SaveAssets();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("PostLib Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            settings.enablePostLib = EditorGUILayout.ToggleLeft("Ativar PostLib nas builds", settings.enablePostLib);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.HelpBox("Essa opção controla se os recursos do PostLib (JS, DevTools, etc.) serão incluídos na build WebGL.", MessageType.Info);
        }
    }
}