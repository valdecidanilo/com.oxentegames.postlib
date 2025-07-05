using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PostLib.Editor
{
    public static class LoaderSettingsCreator
    {
        [MenuItem("Tools/PostLib/Loader Settings/Create LoaderSettings Object", false, 10)]
        private static void CreateLoaderSettingsObject()
        {
            if (GameObject.Find("LoaderSettings") != null)
            {
                Debug.LogWarning("[LoaderSettings] Já existe um objeto chamado 'LoaderSettings' na cena.");
                return;
            }

            var root = new GameObject("LoaderSettings");
            var loaderScript = root.AddComponent<LoaderSetings>();
            Selection.activeGameObject = root;

            Debug.Log("[LoaderSettings] Objeto criado na cena.");
        }
    }
}