using System.IO;
using UnityEditor;

namespace PostLib.Editor
{
    /// <summary>Cria/atualiza os arquivos .jslib quando o projeto recompila.</summary>
    [InitializeOnLoad]
    internal static class PostLibPluginCreator
    {
        private const string PluginPath = "Assets/Plugins/WebGL/PostLib.jslib";
        private const string PluginUserAgentPath = "Assets/Plugins/WebGL/UserAgent.jslib";
        private const string PluginUrlOpenerPath = "Assets/Plugins/WebGL/UrlOpener.jslib";
        private const string PluginSource = @"
mergeInto(LibraryManager.library, {
    JS_Receive: function () {
    window.addEventListener('message', function (e) {
      if(e.data.internal === true) return;
        console.log('[PNP -> PostLib]: ', e);
        const payload = typeof e.data === 'string' ? e.data : JSON.stringify(e.data);
        console.log('[PNP -> PostLib]: ', payload);
        window.unityInstance.SendMessage('PostBridge', 'OnReceive', payload);
    });
    },
    JS_Send: function (ptr) {
      var jsonStr = UTF8ToString(ptr);
      window.parent.postMessage(JSON.parse(jsonStr), '*');
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
        private const string PluginUrlOpener = @"
mergeInto(LibraryManager.library, {
  JS_OpenUrlOverlay: function (ptr) {
    var url = UTF8ToString(ptr);
    var overlay = document.getElementById('postlib-url-overlay');
    var iframe;

    if (!overlay) {
      overlay = document.createElement('div');
      overlay.id = 'postlib-url-overlay';
      overlay.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.85);z-index:999999;display:flex;flex-direction:column;';

      var closeBtn = document.createElement('button');
      closeBtn.innerText = '✕';
      closeBtn.style.cssText = 'align-self:flex-end;flex-shrink:0;margin:8px;width:36px;height:36px;font-size:18px;line-height:1;background:#fff;color:#000;border:none;border-radius:50%;cursor:pointer;';
      closeBtn.onclick = function () { overlay.style.display = 'none'; };

      iframe = document.createElement('iframe');
      iframe.id = 'postlib-url-overlay-iframe';
      iframe.style.cssText = 'flex:1;width:100%;border:none;background:#fff;';

      overlay.appendChild(closeBtn);
      overlay.appendChild(iframe);
      document.body.appendChild(overlay);
    } else {
      iframe = document.getElementById('postlib-url-overlay-iframe');
    }

    iframe.src = url;
    overlay.style.display = 'flex';
  }
});
";
        static PostLibPluginCreator()
        {
            EnsurePluginFile();
            EnsurePluginUserAgentFile();
            EnsurePluginUrlOpenerFile();
        }

        private static void EnsurePluginFile()
        {
            var fullPath = Path.GetFullPath(PluginPath);

            if (!File.Exists(fullPath) || File.ReadAllText(fullPath) != PluginSource)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllText(fullPath, PluginSource);
                // Debug.Log("[PostLib] PostLib.jslib atualizado em: " + PluginPath);
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
                // Debug.Log("[PostLib] UserAgent.jslib atualizado em: " + PluginUserAgentPath);
                AssetDatabase.Refresh();
            }
        }
        private static void EnsurePluginUrlOpenerFile()
        {
            var fullPath = Path.GetFullPath(PluginUrlOpenerPath);

            if (!File.Exists(fullPath) || File.ReadAllText(fullPath) != PluginUrlOpener)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllText(fullPath, PluginUrlOpener);
                // Debug.Log("[PostLib] UrlOpener.jslib atualizado em: " + PluginUrlOpenerPath);
                AssetDatabase.Refresh();
            }
        }
    }
}