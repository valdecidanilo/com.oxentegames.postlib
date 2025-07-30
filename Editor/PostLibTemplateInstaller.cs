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

            DirectoryCopy(src, dst, true);
            AssetDatabase.Refresh();
            Debug.Log($"[PostLib] WebGL template copiado para: {dst}");
        }

        /* ------------------------------------------------------------ */
        private static void CreateSettingsAssetIfNeeded()
        {
            string settingsAssetPath = "Assets/Resources/PostLibSettings.asset";

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
                string extension = file.Extension;
                if (extension.Equals(".meta", System.StringComparison.OrdinalIgnoreCase))
                    continue;
                bool canOverride = extension.Equals(".css", System.StringComparison.OrdinalIgnoreCase) ||
                                   extension.Equals(".js", System.StringComparison.OrdinalIgnoreCase);
                var destFile = Path.Combine(destDir, file.Name);
                try { file.CopyTo(destFile, canOverride); }
                catch { Debug.LogWarning($"{file.Name} already exists"); }
            }

            if (copySubDirs)
                foreach (DirectoryInfo sub in dir.GetDirectories())
                    DirectoryCopy(sub.FullName, Path.Combine(destDir, sub.Name), true);
        }
    }
}
