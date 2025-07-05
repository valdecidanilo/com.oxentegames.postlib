using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PostLib.Editor
{
    internal class WebGLDevToolsStripper : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private const string PostLibStart = "<!-- POSTLIB_START -->";
        private const string PostLibEnd = "<!-- POSTLIB_END -->";
        private const string DevStart = "<!-- DEV_TOOLS_START -->";
        private const string DevEnd   = "<!-- DEV_TOOLS_END -->";

        private static readonly string TemplatePath =
            Path.Combine(Application.dataPath, "WebGLTemplates/PostLibTemplate/index.html");
        private static readonly string SourceDir =
            Path.Combine(Application.dataPath, "WebGLTemplates/PostLibTemplate/Source");

        private string _backupHtmlPath;
        private string _externalTempDir;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL) return;
            
            var isDev = (report.summary.options & BuildOptions.Development) != 0;
            if (isDev) return;

            if (!File.Exists(TemplatePath))
            {
                Debug.LogWarning($"[PostLib] Template não encontrado em {TemplatePath}");
                return;
            }

            _backupHtmlPath = TemplatePath + ".bak";
            File.Copy(TemplatePath, _backupHtmlPath, true);

            var html = File.ReadAllText(TemplatePath);
            if (!isDev)
            {
                html = Regex.Replace(
                    html,
                    $"{Regex.Escape(DevStart)}[\\s\\S]*?{Regex.Escape(DevEnd)}",
                    string.Empty,
                    RegexOptions.IgnoreCase);
                Debug.Log("[PostLib] Painel Dev‑Tools removido para build release.");
            }
            if (!PostLibSettings.Instance.enablePostLib)
            {
                html = Regex.Replace(
                    html,
                    $"{Regex.Escape(PostLibStart)}[\\s\\S]*?{Regex.Escape(PostLibEnd)}",
                    string.Empty,
                    RegexOptions.IgnoreCase);
                Debug.Log("[PostLib] PostLib desativado nesta build.");
            }

            if (Directory.Exists(SourceDir))
            {
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                _externalTempDir = Path.Combine(projectRoot, "Temp/PostLibTemplateSourceBackup");

                if (Directory.Exists(_externalTempDir))
                    Directory.Delete(_externalTempDir, true);

                Directory.CreateDirectory(Path.GetDirectoryName(_externalTempDir));
                Directory.Move(SourceDir, _externalTempDir);
                Debug.Log("[PostLib] Pasta Source/ movida temporariamente para fora da build.");
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (!string.IsNullOrEmpty(_backupHtmlPath) && File.Exists(_backupHtmlPath))
            {
                File.Copy(_backupHtmlPath, TemplatePath, true);
                File.Delete(_backupHtmlPath);
                Debug.Log("[PostLib] Template restaurado após build.");
            }

            if (!string.IsNullOrEmpty(_externalTempDir) && Directory.Exists(_externalTempDir))
            {
                if (Directory.Exists(SourceDir))
                    Directory.Delete(SourceDir, true);

                Directory.Move(_externalTempDir, SourceDir);
                Debug.Log("[PostLib] Pasta Source/ restaurada após build.");
            }
        }
    }
}
