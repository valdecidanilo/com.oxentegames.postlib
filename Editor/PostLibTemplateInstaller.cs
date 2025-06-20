using System.IO;
using UnityEditor;
using UnityEngine;

namespace PostLib.Editor
{
    [InitializeOnLoad]
    internal static class PostLibTemplateInstaller
    {
        private const string SourcePath  = "Packages/PostLib/WebGLTemplates";
        private const string TargetPath  = "Assets/WebGLTemplates";
        private const string TemplateName = "PostLibTemplate";

        static PostLibTemplateInstaller()
        {
            EnsureTemplateInAssets();
        }

        private static void EnsureTemplateInAssets()
        {
            var src = Path.Combine(SourcePath, TemplateName);
            var dst = Path.Combine(TargetPath, TemplateName);

            if (Directory.Exists(dst)) return;

            if (!Directory.Exists(src))
            {
                Debug.LogWarning($"[PostLib] Template não encontrado em {src}");
                return;
            }

            DirectoryCopy(src, dst, true);
            AssetDatabase.Refresh();
            Debug.Log($"[PostLib] WebGL template copiado para: {dst}");
        }

        private static void DirectoryCopy(string sourceDir, string destDir, bool copySubDirs)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) return;

            Directory.CreateDirectory(destDir);

            foreach (var file in dir.GetFiles())
                file.CopyTo(Path.Combine(destDir, file.Name), true);

            if (!copySubDirs) return;
            foreach (var sub in dir.GetDirectories())
                DirectoryCopy(sub.FullName, Path.Combine(destDir, sub.Name), true);
        }
    }
}