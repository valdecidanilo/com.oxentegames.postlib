using System;
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
        private const string PostLibEnd   = "<!-- POSTLIB_END -->";
        private const string DevStart     = "<!-- DEV_TOOLS_START -->";
        private const string DevEnd       = "<!-- DEV_TOOLS_END -->";

        private static readonly string TemplateDir   = Path.Combine(Application.dataPath, "WebGLTemplates/PostLibTemplate");
        private static readonly string TemplatePath  = Path.Combine(TemplateDir, "index.html");
        private static readonly string StyleCssPath  = Path.Combine(TemplateDir, "TemplateData/style.css");
        private static readonly string SourceDir     = Path.Combine(TemplateDir, "Source");

        private string _backupHtmlPath;
        private string _externalTempDir;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
                return;

            // Detecção melhorada de build de desenvolvimento
            bool isDevBuild = IsDevBuild(report);
            
            Debug.Log($"[PostLib] Platform: {report.summary.platform}");
            Debug.Log($"[PostLib] BuildOptions: {report.summary.options}");
            Debug.Log($"[PostLib] EditorUserBuildSettings.development: {EditorUserBuildSettings.development}");
            Debug.Log($"[PostLib] Detected as DEV BUILD: {isDevBuild}");

            var settings = PostLibSettings.Instance;
            if (settings == null) 
            { 
                Debug.LogWarning("[PostLib] Não foi possível carregar PostLibSettings.Instance."); 
                return; 
            }
            
            if (!File.Exists(TemplatePath)) 
            { 
                Debug.LogWarning($"[PostLib] index.html não encontrado em {TemplatePath}"); 
                return; 
            }

            try
            {
                // Backup do HTML para restauração no pós-build
                _backupHtmlPath = TemplatePath + ".bak";
                File.Copy(TemplatePath, _backupHtmlPath, true);

                string html = File.ReadAllText(TemplatePath);

                if (!settings.enablePostLib)
                {
                    // PostLib OFF → strip total + move Source/
                    html = StripPostLibRegion(html);
                    html = StripDevToolsRegion(html);
                    File.WriteAllText(TemplatePath, html);
                    Debug.Log("[PostLib] PostLib desativado: removidos POSTLIB e DEV_TOOLS.");
                    MoveSourceOut();
                    return;
                }

                if (isDevBuild)
                {
                    // DEV BUILD → manter/garantir DEV_TOOLS e NÃO mover Source/
                    html = RemoveCanvasTabIndex(html);
                    html = EnsureDevToolsBlock(html);
                    File.WriteAllText(TemplatePath, html);
                    Debug.Log("[PostLib] Build DEV detectado: DEV_TOOLS preservado/ativado, tabindex removido e Source/ mantida.");
                    // NÃO mover Source/ em dev builds - ela precisa estar disponível
                    return; // IMPORTANTE: não executar a lógica de release
                }
                else
                {
                    // RELEASE BUILD → remover apenas DEV_TOOLS; manter PostLib; mover Source/
                    Debug.Log("[PostLib] Build RELEASE detectado: processando remoção de DEV_TOOLS...");
                    CleanUnityContainerPositionProps();
                    html = StripDevToolsRegion(html);
                    File.WriteAllText(TemplatePath, html);
                    Debug.Log("[PostLib] Build RELEASE: DEV_TOOLS removido.");
                    MoveSourceOut(); // Só mover Source/ em release builds
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PostLib] Erro no Preprocess: {ex.Message}\n{ex.StackTrace}");
                TryRestoreTemplateImmediate();
            }
        }

        /// <summary>
        /// Detecção robusta de build de desenvolvimento
        /// </summary>
        private bool IsDevBuild(BuildReport report)
        {
            // Verificar se o Development Build está marcado no BuildOptions
            bool hasDevelopmentFlag = (report.summary.options & BuildOptions.Development) != 0;
            
            // Verificar se o Development Build está marcado nas configurações do editor
            bool editorDevSetting = EditorUserBuildSettings.development;
            
            // Verificar configurações específicas do WebGL
            bool webglDevMode = false;
            #if UNITY_2019_1_OR_NEWER
            webglDevMode = EditorUserBuildSettings.GetPlatformSettings("WebGL", "DevelopmentBuild") == "True";
            #endif
            
            // Log detalhado para debug
            Debug.Log($"[PostLib] Development Detection:");
            Debug.Log($"  - BuildOptions.Development flag: {hasDevelopmentFlag}");
            Debug.Log($"  - EditorUserBuildSettings.development: {editorDevSetting}");
            Debug.Log($"  - WebGL Platform DevelopmentBuild: {webglDevMode}");
            
            // É build de desenvolvimento se qualquer uma das condições for verdadeira
            return hasDevelopmentFlag || editorDevSetting || webglDevMode;
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
                return;

            bool isDevBuild = IsDevBuild(report);
            Debug.Log($"[PostLib] Postprocess - Detected as DEV BUILD: {isDevBuild}");

            // Restaurar HTML original
            if (!string.IsNullOrEmpty(_backupHtmlPath) && File.Exists(_backupHtmlPath))
            {
                try
                {
                    File.Copy(_backupHtmlPath, TemplatePath, true);
                    File.Delete(_backupHtmlPath);
                    Debug.Log("[PostLib] Template HTML restaurado após build.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PostLib] Falha ao restaurar template: {ex.Message}");
                }
            }

            // Restaurar pasta Source APENAS se foi uma release build (que a moveu)
            if (!isDevBuild && !string.IsNullOrEmpty(_externalTempDir) && Directory.Exists(_externalTempDir))
            {
                try
                {
                    if (Directory.Exists(SourceDir))
                        Directory.Delete(SourceDir, true);

                    Directory.CreateDirectory(Path.GetDirectoryName(SourceDir));
                    Directory.Move(_externalTempDir, SourceDir);
                    Debug.Log("[PostLib] Pasta Source/ restaurada após build de release.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[PostLib] Falha ao restaurar pasta Source/: {ex.Message}");
                }
            }
            else if (isDevBuild)
            {
                Debug.Log("[PostLib] Build DEV: pasta Source/ não foi movida, nada para restaurar.");
            }
        }

        /* =============================== Helpers =============================== */

        private static string StripPostLibRegion(string html)
        {
            // Remove tudo entre POSTLIB_START/END
            return Regex.Replace(
                html,
                $"{Regex.Escape(PostLibStart)}[\\s\\S]*?{Regex.Escape(PostLibEnd)}",
                string.Empty,
                RegexOptions.IgnoreCase
            );
        }

        private static string StripDevToolsRegion(string html)
        {
            // Remove tudo entre DEV_TOOLS_START/END
            return Regex.Replace(
                html,
                $"{Regex.Escape(DevStart)}[\\s\\S]*?{Regex.Escape(DevEnd)}",
                string.Empty,
                RegexOptions.IgnoreCase
            );
        }

        /// <summary>
        /// Remove atributo tabindex do &lt;canvas&gt; (robusto p/ aspas simples/duplas e variações).
        /// </summary>
        private static string RemoveCanvasTabIndex(string html)
        {
            // Remove somente o atributo tabindex=..., preservando o resto.
            var withoutTabIndex = Regex.Replace(
                html,
                @"(<canvas\b[^>]*?)\s+tabindex\s*=\s*(['""]?-?\d+['""]?)",
                "$1",
                RegexOptions.IgnoreCase
            );

            // Normaliza espaços duplicados dentro da tag <canvas ...>
            withoutTabIndex = Regex.Replace(
                withoutTabIndex,
                @"<canvas\b([^>]*)>",
                m =>
                {
                    var attrs = m.Groups[1].Value;
                    attrs = Regex.Replace(attrs, @"\s{2,}", " ");
                    return "<canvas" + attrs + ">";
                },
                RegexOptions.IgnoreCase
            );

            return withoutTabIndex;
        }

        private static string EnsureDevToolsBlock(string html)
        {
            var devRegionPattern = $"{Regex.Escape(DevStart)}[\\s\\S]*?{Regex.Escape(DevEnd)}";
            var hasDevRegion = Regex.IsMatch(html, devRegionPattern, RegexOptions.IgnoreCase);

            if (hasDevRegion)
            {
                Debug.Log("[PostLib] DEV_TOOLS region já existe, garantindo que não esteja comentado...");
                
                // Descomenta tags (<script>/<link>/<style>) que possam estar comentadas dentro do bloco
                html = Regex.Replace(
                    html,
                    $"{Regex.Escape(DevStart)}([\\s\\S]*?){Regex.Escape(DevEnd)}",
                    m =>
                    {
                        var inner = m.Groups[1].Value;

                        inner = Regex.Replace(inner,
                            @"<!--\s*(<(?:script|link|style)\b[\s\S]*?>)\s*-->",
                            "$1",
                            RegexOptions.IgnoreCase);

                        inner = Regex.Replace(inner, @"\n\s+\n", "\n");
                        return $"{DevStart}\n{inner}\n{DevEnd}";
                    },
                    RegexOptions.IgnoreCase
                );
            }
            else
            {
                Debug.Log("[PostLib] DEV_TOOLS region não encontrada, injetando bloco padrão...");
                
                // Injeta bloco padrão de DEV_TOOLS
                var injection = $@"
{DevStart}
<link rel=""stylesheet"" href=""Source/dev-tools.css"">
<script src=""Source/dev-tools.js""></script>
{DevEnd}";

                if (Regex.IsMatch(html, "</head>", RegexOptions.IgnoreCase))
                {
                    html = Regex.Replace(html, "</head>", $"{injection}\n</head>", RegexOptions.IgnoreCase);
                }
                else if (Regex.IsMatch(html, "</body>", RegexOptions.IgnoreCase))
                {
                    html = Regex.Replace(html, "</body>", $"{injection}\n</body>", RegexOptions.IgnoreCase);
                }
                else
                {
                    html += injection;
                }
            }

            return html;
        }

        /// <summary>
        /// Limpa 'position/top/left' apenas no bloco #unity-container do style.css (release).
        /// </summary>
        private static void CleanUnityContainerPositionProps()
        {
            try
            {
                if (!File.Exists(StyleCssPath))
                {
                    Debug.LogWarning($"[PostLib] style.css não encontrado em {StyleCssPath}");
                    return;
                }

                string css = File.ReadAllText(StyleCssPath);

                css = Regex.Replace(
                    css,
                    @"(#unity-container\s*{[^}]*?)\b(position|top|left)\s*:\s*[^;]+;\s*",
                    "$1",
                    RegexOptions.IgnoreCase
                );

                File.WriteAllText(StyleCssPath, css);
                Debug.Log("[PostLib] Release: removidas 'position/top/left' do #unity-container em style.css.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PostLib] Falha na limpeza de style.css: {ex.Message}");
            }
        }

        /// <summary>
        /// Move a pasta Source/ para fora do template (para não ir na build). Restaura no Postprocess.
        /// </summary>
        private void MoveSourceOut()
        {
            try
            {
                if (!Directory.Exists(SourceDir)) 
                {
                    Debug.Log("[PostLib] Pasta Source/ não encontrada, nada para mover.");
                    return;
                }

                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                _externalTempDir = Path.Combine(projectRoot, "Temp", "PostLibTemplateSourceBackup");

                if (Directory.Exists(_externalTempDir))
                    Directory.Delete(_externalTempDir, true);

                Directory.CreateDirectory(Path.GetDirectoryName(_externalTempDir));
                Directory.Move(SourceDir, _externalTempDir);

                Debug.Log($"[PostLib] Pasta Source/ movida temporariamente para: {_externalTempDir}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PostLib] Não foi possível mover Source/: {ex.Message}");
            }
        }

        /// <summary>
        /// Tenta restaurar o template imediatamente se houve erro no preprocess.
        /// </summary>
        private void TryRestoreTemplateImmediate()
        {
            try
            {
                if (!string.IsNullOrEmpty(_backupHtmlPath) && File.Exists(_backupHtmlPath))
                {
                    File.Copy(_backupHtmlPath, TemplatePath, true);
                    File.Delete(_backupHtmlPath);
                    Debug.Log("[PostLib] Template restaurado após erro no preprocess.");
                }

                if (!string.IsNullOrEmpty(_externalTempDir) && Directory.Exists(_externalTempDir))
                {
                    if (Directory.Exists(SourceDir))
                        Directory.Delete(SourceDir, true);

                    Directory.CreateDirectory(Path.GetDirectoryName(SourceDir));
                    Directory.Move(_externalTempDir, SourceDir);
                    Debug.Log("[PostLib] Pasta Source/ restaurada após erro no preprocess.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PostLib] Falha ao restaurar após erro: {ex.Message}");
            }
        }
    }
}