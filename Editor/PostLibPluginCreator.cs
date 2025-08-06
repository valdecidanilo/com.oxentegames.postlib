using System.IO;
using UnityEditor;
using UnityEngine;

namespace PostLib.Editor
{
    /// <summary>Cria/atualiza os arquivos .jslib quando o projeto recompila.</summary>
    [InitializeOnLoad]
    internal static class PostLibPluginCreator
    {
        private const string PluginPath = "Assets/Plugins/WebGL/PostLib.jslib";
        private const string PluginUserAgentPath = "Assets/Plugins/WebGL/UserAgent.jslib";
        private const string PluginSource = @"
mergeInto(LibraryManager.library, {
    JS_Receive: function () {
    window.addEventListener('message', function (e) {
        console.log(e);
        SendMessage('PostBridge', 'OnReceive', JSON.stringify(e.data));
    });
    },
    JS_Send: function (ptr) {
        const jsonStr = UTF8ToString(ptr);
        console.log('[Postlib] raw JSON:', jsonStr);

        let data;
        try {
            data = JSON.parse(jsonStr);
        } catch (ex) {
            console.error('[PostLib] JSON inválido', ex);
            return;
        }

        window.top.postMessage(data, '*');
    }
});
";
        private const string PluginUserAgent = @"
mergeInto(LibraryManager.library, {
  GetUserAgent: function() {
    const ua = navigator.userAgent;
    const size = lengthBytesUTF8(ua) + 1;
    const ptr  = _malloc(size);
    stringToUTF8(ua, ptr, size);
    return ptr;
  },
  IsMobileDevice: function() {
    return (navigator.userAgentData?.mobile === true ||
            /Mobi|Android|iPhone|iPad|iPod/i.test(navigator.userAgent))
           ? 1 : 0;
  },
  GetPlatformHint: function() {
    const plat = navigator.userAgentData?.platform
                 || navigator.platform
                 || 'unknown';
    const size = lengthBytesUTF8(plat) + 1;
    const ptr  = _malloc(size);
    stringToUTF8(plat, ptr, size);
    return ptr;
  }
});
        ";
        static PostLibPluginCreator()
        {
            EnsurePluginFile();
            EnsurePluginUserAgentFile();
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
        private static void EnsurePluginUserAgentFile()
        {
            var fullPath = Path.GetFullPath(PluginUserAgentPath);

            if (!File.Exists(fullPath) || File.ReadAllText(fullPath) != PluginUserAgent)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllText(fullPath, PluginUserAgent);
                Debug.Log("[PostLib] UserAgent.jslib atualizado em: " + PluginUserAgentPath);
                AssetDatabase.Refresh();
            }
        }
    }
}