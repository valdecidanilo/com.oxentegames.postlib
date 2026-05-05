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
        private const string PluginWebBridgePath = "Assets/Plugins/WebGL/WebBridge.jslib";
        private const string PluginUserAgentPath = "Assets/Plugins/WebGL/UserAgent.jslib";
        private const string PluginSource = @"
mergeInto(LibraryManager.library, {
    JS_Receive: function () {
    window.addEventListener('message', function (e) {
      if(e.data.internal === true) return;
        const payload = typeof e.data === 'string' ? e.data : JSON.stringify(e.data);
        window.unityInstance.SendMessage('PostBridge', 'OnReceive', payload);
    });
    },
    JS_Send: function (ptr) {
      var jsonStr = UTF8ToString(ptr);
      window.parent.postMessage(JSON.parse(jsonStr), '*');
    },
    Unity_GameReady: function () {
      if (typeof window.OnUnityGameReady === 'function') {
        window.OnUnityGameReady();
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
        private const string PluginWebBridge = @"
mergeInto(LibraryManager.library, {

    OpenUrl: function(url) {
        window.open(UTF8ToString(url), '_blank');
    },

    OpenTopUrl: function(url) {
        window.top.location.href = UTF8ToString(url);
    },

    ReloadUrl: function() {
        window.location.reload();
    },

    IsOnline: function() {
        return navigator.onLine ? 1 : 0;
    },
    
    WebGLLoading_SetBootProgress: function (progress) {
       if (typeof window !== 'undefined' && window.LoadingManager) {
         window.LoadingManager.setBootProgress(progress);
       }
     },
    
    WebGLLoading_SetBootStatus: function (statusPtr) {
       if (typeof window !== 'undefined' && window.LoadingManager) {
         const status = UTF8ToString(statusPtr);
         window.LoadingManager.setStatus(status);
       }
    },

    WebGLLoading_SetBootProgressWithStatus: function (progress, statusPtr) {
       if (typeof window !== 'undefined' && window.LoadingManager) {
         const status = UTF8ToString(statusPtr);
         window.LoadingManager.setBootProgress(progress, status);
       }
    },
   
    WebGLLoading_Complete: function () {
       if (typeof window !== 'undefined') {
         if (typeof window.OnUnityGameReady === 'function') {
           window.OnUnityGameReady();
         } else if (window.LoadingManager) {
           window.LoadingManager.complete();
         }
       }
    }
});
        ";
        static PostLibPluginCreator()
        {
            EnsurePluginFile();
            EnsurePluginUserAgentFile();
            EnsurePluginWebBridgeFile();
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
        private static void EnsurePluginWebBridgeFile()
        {
            var fullPath = Path.GetFullPath(PluginWebBridgePath);

            if (!File.Exists(fullPath) || File.ReadAllText(fullPath) != PluginWebBridge)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllText(fullPath, PluginWebBridge);
                Debug.Log("[PostLib] WebBridge.jslib atualizado em: " + PluginWebBridgePath);
                AssetDatabase.Refresh();
            }
        }
    }
}