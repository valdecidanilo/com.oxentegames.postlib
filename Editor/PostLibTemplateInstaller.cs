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
            if(!CanUpdateVersion()) return;

            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) return;
            
            Directory.CreateDirectory(destDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string extension = file.Extension;
                if (extension.Equals(".meta", System.StringComparison.OrdinalIgnoreCase))
                    continue;
                
                bool canOverride = extension.Equals(".css", System.StringComparison.OrdinalIgnoreCase) ||
                                   extension.Equals(".js", System.StringComparison.OrdinalIgnoreCase) ||
                                   extension.Equals(".yml", System.StringComparison.OrdinalIgnoreCase);
                var destFile = Path.Combine(destDir, file.Name);
                try { file.CopyTo(destFile, canOverride); }
                catch { Debug.LogWarning($"{file.Name} already exists"); }
            }

            if (copySubDirs)
                foreach (DirectoryInfo sub in dir.GetDirectories())
                    DirectoryCopy(sub.FullName, Path.Combine(destDir, sub.Name), true);
        }
        public bool CanUpdateVersion()
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var pathRemote = $"{GetPackageRoot()}/PostLibTemplate/version.yml";
            var yamlRemote = File.ReadAllText(pathRemote);
            var configRemote = deserializer.Deserialize<TemplateVersion>(yamlRemote);

            var pathLocal = $"{TargetRoot}/PostLibTemplate/version.yml";
            if(File.Exists(pathLocal)) return true;
            var yamlLocal = File.ReadAllText(pathLocal);
            var configLocal = deserializer.Deserialize<TemplateVersion>(yamlLocal);

            Version remoteVersion;
            Version localVersion;
            try
            {
                remoteVersion = new Version(configRemote.Version);
                localVersion  = new Version(configLocal.Version);
            }
            catch (FormatException ex)
            {
                Console.Error.WriteLine($"Erro ao parsear versão: {ex.Message}");
                return;
            }

            int cmp = localVersion.CompareTo(remoteVersion);
            if (cmp < 0)
            {
                Debug.Log($"[PostLib] Template desatualizado! Local: {localVersion}, Remoto: {remoteVersion}");
                return true;
            }
            else if (cmp == 0)
            {
                Debug.Log($"[PostLib] Você já está na última versão ({localVersion}).");
                return =  false;
            }
            else
            {
                Debug.Log($"[PostLib] Local ({localVersion}) é mais recente que o remoto ({remoteVersion}).");
                return false;
            }
        }
    }
    public class TemplateVersion
    {
        [YamlMember(Alias = "version")]
        public string Version { get; set; }
    }
}
