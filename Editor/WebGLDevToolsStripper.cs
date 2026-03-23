using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PostLib.Editor
{
    internal class WebGLDevToolsStripper : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
                return;

            var settings = PostLibSettings.Instance;
            if (settings == null)
            {
                Debug.LogWarning("[PostLib] PostLibSettings não encontrado.");
                return;
            }

            if (!settings.enablePostLib)
            {
                Debug.Log("[PostLib] PostLib desativado. Build seguirá sem alterar template, sem mover arquivos e sem gerar backup.");
                return;
            }

            Debug.Log("[PostLib] Build WebGL iniciada sem modificar index.html, sem mover Source/ e sem processar DEV_TOOLS.");
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
                return;

            Debug.Log("[PostLib] Pós-build concluído sem restaurações, pois nenhum arquivo foi alterado.");
        }
    }
}