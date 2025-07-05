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
            // Verifica se já existe
            if (GameObject.Find("LoaderSettings") != null)
            {
                Debug.LogWarning("[LoaderSettings] Já existe um objeto chamado 'LoaderSettings' na cena.");
                return;
            }

            // Cria objeto principal
            GameObject root = new GameObject("LoaderSettings");
            var loaderScript = root.AddComponent<LoaderSetings>();

            // Garante que está dentro de um Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            root.transform.SetParent(canvas.transform, false);

            // Cria objeto de imagem (logo)
            GameObject logoGO = new GameObject("Logo");
            logoGO.transform.SetParent(root.transform, false);
            Image logo = logoGO.AddComponent<Image>();
            loaderScript.GetType().GetField("logoLoader",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(loaderScript, logo);

            // Cria objeto de texto (descrição)
            GameObject textGO = new GameObject("Description");
            textGO.transform.SetParent(root.transform, false);
            TMP_Text tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "Descrição de carregamento...";
            tmp.fontSize = 24;
            loaderScript.GetType().GetField("descriptionLoader",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(loaderScript, tmp);

            // Seleciona no editor
            Selection.activeGameObject = root;

            Debug.Log("[LoaderSettings] Objeto criado na cena.");
        }
    }
}