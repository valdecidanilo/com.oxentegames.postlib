using System.IO;
using UnityEditor;
using UnityEngine;

namespace PostLib.Editor
{
    /// <summary>Cria/atualiza o arquivo PostLib.jslib quando o projeto recompila.</summary>
    [InitializeOnLoad]
    internal static class PostLibPluginCreator
    {
        private const string PluginPath = "Assets/Plugins/WebGL/PostLib.jslib";

        private const string PluginSource = @"
mergeInto(LibraryManager.library, {
    JS_Receive: function () {
    window.addEventListener('message', function (e) {
        if (e.data && e.data._type)
            SendMessage('PostBridge', 'OnReceive', JSON.stringify(e.data));
        });
    },
    JS_Send: function (ptr) {
        const json = UTF8ToString(ptr);
        window.parent.postMessage(JSON.parse(json), '*');
    }
});
";

        static PostLibPluginCreator()
        {
            EnsurePluginFile();
        }

        private static void EnsurePluginFile()
        {
            var fullPath = Path.GetFullPath(PluginPath);

            if (!File.Exists(fullPath) || File.ReadAllText(fullPath) != PluginSource)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllText(fullPath, PluginSource);
                Debug.Log("[PostLib] PostLib.jslib atualizado em: " + PluginPath);
                AssetDatabase.Refresh();
            }
        }
    }
}