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
      if(e.data.internal === true) return;
        console.log('[PNP -> PostLib]: ', e);
        try 
        {
            if (typeof unityInstance !== 'undefined') {
                unityInstance.SendMessage(
                    'PostBridge', 
                    'OnReceive', 
                    JSON.stringify(e.data)
                );
            } else {
                console.error('UnityInstance não está definido');
            }
        } catch (error) {
            console.error('Erro ao enviar mensagem para Unity:', error);
        }
    });
    },
    JS_Send: function (ptr) {
      var jsonStr = UTF8ToString(ptr);
      if (typeof console !== 'undefined' && console.log) console.log('[PostLib] raw JSON:', jsonStr);

      var data;
      try {
        data = JSON.parse(jsonStr);
      } catch (ex) {
        if (typeof console !== 'undefined' && console.error) console.error('[PostLib] JSON inválido', ex);
        return;
      }

      var isArray = (typeof Array.isArray === 'function')
        ? Array.isArray(data)
        : Object.prototype.toString.call(data) === '[object Array]';

      var msg;
      if (data && typeof data === 'object' && !isArray) {
        data.internal = true;
        msg = data;
      } else {
        msg = { payload: data, internal: true };
      }

      try {
        window.parent.postMessage(msg, '*');
      } catch (err) {
        if (typeof console !== 'undefined' && console.error) console.error('[PostLib] postMessage falhou:', err);
      }
    }
});
";
        private const string PluginUserAgent = @"
mergeInto(LibraryManager.library, {
  GetUserAgent: function() {
    return allocateUTF8(navigator.userAgent);
  },
  IsMobileDevice: function() {
    var uaData = navigator.userAgentData;
    var isHintMobile = uaData && uaData.mobile === true;
    var regexMobile = /Mobi|Android|iPhone|iPad|iPod/i.test(navigator.userAgent);
    return (isHintMobile || regexMobile) ? 1 : 0;
  },
  GetPlatformHint: function() {
    var uaData = navigator.userAgentData;
    var plat;
    if (uaData && uaData.platform) {
      plat = uaData.platform;
    } else if (navigator.platform) {
      plat = navigator.platform;
    } else {
      plat = 'unknown';
    }
    return allocateUTF8(plat);
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