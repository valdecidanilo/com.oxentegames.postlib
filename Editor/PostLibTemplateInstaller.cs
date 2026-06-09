using System;
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

        private const string AutoUpdatePrefsKey = "PostLib.AutoUpdateWebGLTemplate";

        private enum CopyMode
        {
            MissingOnly,
            OverwriteAll,
            UpdateIfPackageVersionIsNewer
        }

        static PostLibTemplateInstaller()
        {
            bool autoUpdate = EditorPrefs.GetBool(AutoUpdatePrefsKey, false);

            EnsureTemplateInAssets(
                autoUpdate 
                    ? CopyMode.UpdateIfPackageVersionIsNewer 
                    : CopyMode.MissingOnly
            );

            EditorApplication.delayCall += CreateSettingsAssetIfNeeded;
        }

        /* ------------------------------------------------------------ */
        private static void EnsureTemplateInAssets(CopyMode mode)
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

            bool overwrite = false;

            switch (mode)
            {
                case CopyMode.MissingOnly:
                    overwrite = false;
                    break;

                case CopyMode.OverwriteAll:
                    overwrite = true;
                    break;

                case CopyMode.UpdateIfPackageVersionIsNewer:
                    overwrite = CanUpdateVersion(src, dst);
                    break;
            }

            DirectoryCopy(src, dst, true, overwrite);
            AssetDatabase.Refresh();
        }

        /* ------------------------------------------------------------ */
        private static void CreateSettingsAssetIfNeeded()
        {
            string settingsAssetPath = Path.Combine(SettingsTargetDir, SettingsFileName);

            if (!Directory.Exists(SettingsTargetDir))
                Directory.CreateDirectory(SettingsTargetDir);

            if (!File.Exists(settingsAssetPath))
            {
                var settings = ScriptableObject.CreateInstance<PostLibSettings>();
                AssetDatabase.CreateAsset(settings, settingsAssetPath);
                Debug.Log($"[PostLib] Asset de configuração criado: {settingsAssetPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /* ------------------------------------------------------------ */
        private static string GetPackageRoot()
        {
            var asm = typeof(PostLibTemplateInstaller).Assembly;
            var info = UPMPackageInfo.FindForAssembly(asm);
            return info?.assetPath;
        }

        /* ------------------------------------------------------------ */
        private static void DirectoryCopy(
            string sourceDir,
            string destDir,
            bool copySubDirs,
            bool overwrite
        )
        {
            var dir = new DirectoryInfo(sourceDir);

            if (!dir.Exists)
                return;

            Directory.CreateDirectory(destDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                if (file.Extension.Equals(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;

                string destFile = Path.Combine(destDir, file.Name);

                if (File.Exists(destFile) && !overwrite)
                {
                    Debug.Log($"[PostLib] Mantido arquivo local: {destFile}");
                    continue;
                }

                file.CopyTo(destFile, overwrite);
                Debug.Log($"[PostLib] Copiado: {destFile}");
            }

            if (!copySubDirs)
                return;

            foreach (DirectoryInfo sub in dir.GetDirectories())
            {
                string nextDestDir = Path.Combine(destDir, sub.Name);
                DirectoryCopy(sub.FullName, nextDestDir, true, overwrite);
            }
        }

        /* ------------------------------------------------------------ */
        private static bool CanUpdateVersion(string sourceTemplateDir, string localTemplateDir)
        {
            string remotePath = Path.Combine(sourceTemplateDir, "version.txt");
            string localPath = Path.Combine(localTemplateDir, "version.txt");

            if (!File.Exists(remotePath))
                return false;

            if (!File.Exists(localPath))
                return true;

            string remoteText = File.ReadAllText(remotePath).Trim();
            string localText = File.ReadAllText(localPath).Trim();

            int comparison = CompareNumericVersion(localText, remoteText);

            return comparison < 0;
        }

        /* ------------------------------------------------------------ */
        private static int CompareNumericVersion(string v1, string v2)
        {
            var p1 = v1.Split('.');
            var p2 = v2.Split('.');

            int len = Math.Max(p1.Length, p2.Length);

            for (int i = 0; i < len; i++)
            {
                int n1 = i < p1.Length && int.TryParse(p1[i], out var x1) ? x1 : 0;
                int n2 = i < p2.Length && int.TryParse(p2[i], out var x2) ? x2 : 0;

                if (n1 < n2) return -1;
                if (n1 > n2) return 1;
            }

            return 0;
        }

        /* ------------------------------------------------------------ */
        [MenuItem("PostLib/WebGL Template/Instalar sem sobrescrever")]
        private static void InstallWithoutOverwrite()
        {
            EnsureTemplateInAssets(CopyMode.MissingOnly);
            Debug.Log("[PostLib] Template instalado sem sobrescrever arquivos locais.");
        }

        /* ------------------------------------------------------------ */
        [MenuItem("PostLib/WebGL Template/Atualizar sobrescrevendo arquivos locais")]
        private static void ForceUpdateTemplate()
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Atualizar PostLib Template",
                "Isso vai sobrescrever arquivos locais do template, incluindo CSS, JS e TXT. Deseja continuar?",
                "Sim, sobrescrever",
                "Cancelar"
            );

            if (!confirm)
                return;

            EnsureTemplateInAssets(CopyMode.OverwriteAll);
            Debug.Log("[PostLib] Template atualizado sobrescrevendo arquivos locais.");
        }

        /* ------------------------------------------------------------ */
        [MenuItem("PostLib/WebGL Template/Atualização automática ao abrir Unity")]
        private static void ToggleAutoUpdate()
        {
            bool current = EditorPrefs.GetBool(AutoUpdatePrefsKey, false);
            bool next = !current;

            EditorPrefs.SetBool(AutoUpdatePrefsKey, next);

            Debug.Log(
                next
                    ? "[PostLib] Atualização automática ativada."
                    : "[PostLib] Atualização automática desativada."
            );
        }

        [MenuItem("PostLib/WebGL Template/Atualização automática ao abrir Unity", true)]
        private static bool ToggleAutoUpdateValidate()
        {
            bool enabled = EditorPrefs.GetBool(AutoUpdatePrefsKey, false);
            Menu.SetChecked("PostLib/WebGL Template/Atualização automática ao abrir Unity", enabled);
            return true;
        }
    }
}