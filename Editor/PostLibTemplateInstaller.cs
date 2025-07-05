using System.IO;
using UnityEditor;
using UPMPackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;

namespace PostLib.Editor
{
    [InitializeOnLoad]
    internal static class PostLibTemplateInstaller
    {
        private const string TemplateName = "PostLibTemplate";
        private const string SettingsFileName = "PostLibSettings.asset";
        private const string TargetRoot = "Assets/WebGLTemplates";
        private const string SettingsTargetDir = "Assets/Resources";

        static PostLibTemplateInstaller()
        {
            EnsureTemplateInAssets();
            EditorApplication.delayCall += () => { CreateSettingsAssetIfNeeded(); };
        }

        /* ------------------------------------------------------------ */
        private static void EnsureTemplateInAssets()
        {
            string packagePath = GetPackageRoot();
            if (string.IsNullOrEmpty(packagePath))
            {
                Debug.LogWarning("[PostLib] Não foi possível localizar o pacote PostLib.");
                return;
            }

            string src = Path.Combine(packagePath, "WebGLTemplates", TemplateName);
            string dst = Path.Combine(TargetRoot, TemplateName);

            if (!Directory.Exists(src))
            {
                Debug.LogWarning($"[PostLib] Template não encontrado em {src}");
                return;
            }

            if (Directory.Exists(dst)) return;

            DirectoryCopy(src, dst, true);
            AssetDatabase.Refresh();
            Debug.Log($"[PostLib] WebGL template copiado para: {dst}");
        }

        /* ------------------------------------------------------------ */
        private static void CreateSettingsAssetIfNeeded()
        {
            string settingsAssetPath = "Assets/Resources/PostLibSettings.asset";
            string bootstrapDir = "Assets/PostLib";
            string bootstrapPath = Path.Combine(bootstrapDir, "PostBridgeBootstrap.cs");

            // Garante que a pasta Resources existe
            if (!Directory.Exists("Assets/Resources"))
                Directory.CreateDirectory("Assets/Resources");

            // Cria o ScriptableObject se não existir
            if (!File.Exists(settingsAssetPath))
            {
                var settings = ScriptableObject.CreateInstance<PostLibSettings>();
                AssetDatabase.CreateAsset(settings, settingsAssetPath);
                Debug.Log($"[PostLib] Asset de configuração criado: {settingsAssetPath}");
            }

            // Cria pasta de scripts se necessário
            if (!Directory.Exists(bootstrapDir))
                Directory.CreateDirectory(bootstrapDir);

            // Cria o arquivo bootstrap se não existir
            if (!File.Exists(bootstrapPath))
            {
                File.WriteAllText(bootstrapPath, @"using UnityEngine;

        namespace PostLib
        {
            internal static class PostBridgeBootstrap
            {
                [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
                public static void CreateBridgeIfMissing()
                {
                    if (Object.FindObjectOfType<PostBridge>() != null)
                        return;
                    Debug.Log(""[PostLib] Inicializado."");
                    var go = new GameObject(""PostBridge"");
                    go.AddComponent<PostBridge>();
                    Object.DontDestroyOnLoad(go);
                }
            }
        }");
                Debug.Log($"[PostLib] Bootstrap script criado: {bootstrapPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }


        private static string GetPackageRoot()
        {
            var asm = typeof(PostLibTemplateInstaller).Assembly;
            var info = UPMPackageInfo.FindForAssembly(asm);
            return info?.assetPath;
        }

        private static void DirectoryCopy(string sourceDir, string destDir, bool copySubDirs)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) return;

            Directory.CreateDirectory(destDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                if (file.Extension.Equals(".meta", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var destFile = Path.Combine(destDir, file.Name);
                file.CopyTo(destFile, true);
            }

            if (copySubDirs)
                foreach (DirectoryInfo sub in dir.GetDirectories())
                    DirectoryCopy(sub.FullName, Path.Combine(destDir, sub.Name), true);
        }
    }
}
