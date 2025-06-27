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
        private const string TargetRoot   = "Assets/WebGLTemplates";
        private const string TargetDir        = "Assets/Resources";

        static PostLibTemplateInstaller() => EnsureTemplateInAssets();

        /* ------------------------------------------------------------ */
        private static void EnsureTemplateInAssets()
        {
            EnsureBootstrapSettings();
            string packagePath = GetPackageRoot();
            if (string.IsNullOrEmpty(packagePath))
            {
                Debug.LogWarning("[PostLib] Não foi possível localizar o pacote PostLib.");
                return;
            }

            string src = Path.Combine(packagePath, "WebGLTemplates", TemplateName);
            string dst = Path.Combine(TargetRoot,         TemplateName);

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
        private static void EnsureBootstrapSettings()
        {
            string packageRoot = GetPackageRoot();
            if (string.IsNullOrEmpty(packageRoot))
            {
                Debug.LogWarning("[PostLib] Não foi possível localizar o pacote PostLib.");
                return;
            }

            string source = Path.Combine(packageRoot, "Editor", SettingsFileName);
            string dest   = Path.Combine(TargetDir,    SettingsFileName);

            if (!File.Exists(source))
            {
                Debug.LogWarning($"[PostLib] Arquivo não encontrado: {source}");
                return;
            }

            if (File.Exists(dest)) return;

            if (!Directory.Exists(TargetDir))
                Directory.CreateDirectory(TargetDir);

            File.Copy(source, dest, overwrite: true);
            AssetDatabase.Refresh();
            Debug.Log($"[PostLib] Arquivo bootstrap criado: {dest}");
        }
        
        private static string GetPackageRoot()
        {
            var asm = typeof(PostLibTemplateInstaller).Assembly;
            var info = UPMPackageInfo.FindForAssembly(asm);
            return info != null ? info.assetPath : null;
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
