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
    },
    Unity_CloseLoadingOverlayImmediately: function () {
        if (typeof window.WebGLLoading_HideOverlayForUnityPopup === 'function') {
            window.WebGLLoading_HideOverlayForUnityPopup();
            return;
        }

        if (typeof window.hideWebGLLoadingOverlay === 'function') {
            window.hideWebGLLoadingOverlay();
            return;
        }

        var overlay = document.getElementById('overlay-content');
        if (overlay) {
            overlay.style.display = 'none';
        }

        try {
            if (window.CoverManager && typeof window.CoverManager.remove === 'function') {
                window.CoverManager.remove();
            }
        } catch (e) {}
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
    },
    
    WebGLLoading_HideOverlayForUnityPopup: function () {
       if (typeof window.WebGLLoading_HideOverlayForUnityPopup === 'function') {
         window.WebGLLoading_HideOverlayForUnityPopup();
       }
    },
    
    // O Safari (iOS) so copia dentro do evento de toque do navegador, e o Unity
    // processa o toque no frame seguinte. Por isso a copia e armada no PointerDown
    // (WebGLClipboard_Arm) e iniciada no proximo pointerup/touchend do DOM, com um
    // ClipboardItem cujo texto e resolvido depois por WebGLClipboard_Copy.
    PostLibClipboard_State: function () {
      if (window.__postlibClipboard) return window.__postlibClipboard;

      var c = {
        armed: false,
        resolve: null,
        reject: null,
        timer: 0,
        timeoutMs: 15000
      };

      c.reset = function (rejectReason) {
        if (c.timer) { clearTimeout(c.timer); c.timer = 0; }
        var reject = c.reject;
        c.armed = false;
        c.resolve = null;
        c.reject = null;
        if (rejectReason && reject) reject(new Error(rejectReason));
      };

      c.startTimeout = function () {
        if (c.timer) clearTimeout(c.timer);
        c.timer = setTimeout(function () { c.reset('timeout'); }, c.timeoutMs);
      };

      c.execCopy = function (value) {
        var textarea = document.createElement('textarea');
        textarea.value = value;
        textarea.setAttribute('readonly', '');
        textarea.style.position = 'fixed';
        textarea.style.left = '0';
        textarea.style.top = '0';
        textarea.style.opacity = '0';
        textarea.style.fontSize = '16px';
        document.body.appendChild(textarea);

        var ok = false;
        try {
          textarea.focus();
          textarea.select();
          textarea.setSelectionRange(0, value.length);
          ok = document.execCommand('copy');
        } catch (e) {
          console.warn('[WebClipboard] fallback copy failed:', e);
        }

        document.body.removeChild(textarea);
        if (!ok) console.warn('[WebClipboard] copy rejected (no user gesture?)');
      };

      c.legacyCopy = function (value) {
        try {
          if (navigator.clipboard && navigator.clipboard.writeText && window.isSecureContext) {
            navigator.clipboard.writeText(value).catch(function () { c.execCopy(value); });
          } else {
            c.execCopy(value);
          }
        } catch (e) {
          c.execCopy(value);
        }
      };

      c.onGesture = function () {
        if (!c.armed) return;
        c.armed = false;

        // Sem ClipboardItem: o texto e copiado do jeito antigo quando chegar
        // (funciona no Chrome por causa da ativacao transitoria).
        if (!window.isSecureContext || !navigator.clipboard || !navigator.clipboard.write ||
            typeof ClipboardItem === 'undefined') {
          c.reset();
          return;
        }

        var textPromise = new Promise(function (resolve, reject) {
          c.resolve = resolve;
          c.reject = reject;
        });
        textPromise.catch(function () {});
        c.startTimeout();

        try {
          var item = new ClipboardItem({
            'text/plain': textPromise.then(function (t) {
              return new Blob([t], { type: 'text/plain' });
            })
          });
          navigator.clipboard.write([item]).catch(function () {
            // ClipboardItem com Promise nao suportado/bloqueado: tenta do jeito antigo.
            textPromise.then(c.legacyCopy, function () {});
          });
        } catch (e) {
          textPromise.then(c.legacyCopy, function () {});
        }
      };

      window.addEventListener('pointerup', c.onGesture, true);
      window.addEventListener('touchend', c.onGesture, true);

      window.__postlibClipboard = c;
      return c;
    },

    WebGLClipboard_Arm__deps: ['PostLibClipboard_State'],
    WebGLClipboard_Arm: function () {
      var c = _PostLibClipboard_State();
      c.reset('superseded');
      c.armed = true;
      c.startTimeout();
    },

    WebGLClipboard_Cancel__deps: ['PostLibClipboard_State'],
    WebGLClipboard_Cancel: function () {
      _PostLibClipboard_State().reset('cancelled');
    },

    WebGLClipboard_Copy__deps: ['PostLibClipboard_State'],
    WebGLClipboard_Copy: function (textPtr) {
      var text = UTF8ToString(textPtr);
      var c = _PostLibClipboard_State();

      // Copia ja iniciada dentro do gesto: so entrega o texto.
      if (c.resolve) {
        var resolve = c.resolve;
        c.reset();
        resolve(text);
        return;
      }

      // Armada mas o gesto nao chegou (ou nao armada): copia direto.
      c.reset();
      c.legacyCopy(text);
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
                //Debug.Log("[PostLib] PostLib.jslib atualizado em: " + PluginPath);
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
                //Debug.Log("[PostLib] UserAgent.jslib atualizado em: " + PluginUserAgentPath);
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
                //Debug.Log("[PostLib] WebBridge.jslib atualizado em: " + PluginWebBridgePath);
                AssetDatabase.Refresh();
            }
        }
    }
}
