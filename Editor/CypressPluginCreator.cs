using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace PostLib.Editor
{
    /// <summary>
    /// Gera/remove o Cypress.jslib conforme o define <c>POSTLIB_CYPRESS</c> do target WebGL.
    /// Mantém a pegada de produção em ZERO: sem o define, o arquivo é apagado e nenhum
    /// extern C# é compilado. Menu em PostLib &gt; Cypress para ligar/desligar.
    /// </summary>
    [InitializeOnLoad]
    internal static class CypressPluginCreator
    {
        private const string Define = "POSTLIB_CYPRESS";
        private const string PluginPath = "Assets/Plugins/WebGL/Cypress.jslib";

        private const string PluginSource = @"
mergeInto(LibraryManager.library, {

    // C# -> JS: publica o snapshot de estado em window.__cypress__.state
    Cypress_SetState: function (ptr) {
        var c = (window.__cypress__ = window.__cypress__ || {});
        try { c.state = JSON.parse(UTF8ToString(ptr)); } catch (e) { c.state = {}; }
        c.stateVersion = (c.stateVersion || 0) + 1;
    },

    // C# -> JS: empurra um evento pontual para window.__cypress__.events
    Cypress_Emit: function (evtPtr, dataPtr) {
        var c = (window.__cypress__ = window.__cypress__ || {});
        c.events = c.events || [];
        var data = null;
        try { data = JSON.parse(UTF8ToString(dataPtr)); } catch (e) {}
        c.events.push({ evt: UTF8ToString(evtPtr), data: data, seq: c.events.length });
    },

    // C# -> JS: sinaliza que a ponte está viva (jogo bootou)
    Cypress_Ready: function () {
        var c = (window.__cypress__ = window.__cypress__ || {});
        c.ready = true;
        if (typeof window.__onCypressReady === 'function') {
            try { window.__onCypressReady(); } catch (e) {}
        }
    },

    // C# -> JS: confirmação de um comando Exec (chaveada pelo id enviado)
    Cypress_Ack: function (idPtr, payloadPtr) {
        var c = (window.__cypress__ = window.__cypress__ || {});
        c.acks = c.acks || {};
        var payload = null;
        try { payload = JSON.parse(UTF8ToString(payloadPtr)); } catch (e) {}
        c.acks[UTF8ToString(idPtr)] = payload;
    }
});
";

        static CypressPluginCreator() => Sync();

        private static bool DefineEnabled()
        {
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.WebGL);
            foreach (var d in defines.Split(';'))
                if (d.Trim() == Define) return true;
            return false;
        }

        private static void Sync()
        {
            var full = Path.GetFullPath(PluginPath);

            if (DefineEnabled())
            {
                if (!File.Exists(full) || File.ReadAllText(full) != PluginSource)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                    File.WriteAllText(full, PluginSource);
                    Debug.Log("[PostLib] Cypress.jslib gerado em: " + PluginPath);
                    AssetDatabase.Refresh();
                }
            }
            else
            {
                if (File.Exists(full))
                {
                    File.Delete(full);
                    var meta = full + ".meta";
                    if (File.Exists(meta)) File.Delete(meta);
                    Debug.Log("[PostLib] Cypress.jslib removido (POSTLIB_CYPRESS desligado).");
                    AssetDatabase.Refresh();
                }
            }
        }

        // ---- Menu para ligar/desligar rapidamente o define no target WebGL ----

        [MenuItem("PostLib/Cypress/Ativar ponte E2E (WebGL)")]
        private static void Enable() => SetDefine(true);

        [MenuItem("PostLib/Cypress/Desativar ponte E2E (WebGL)")]
        private static void Disable() => SetDefine(false);

        [MenuItem("PostLib/Cypress/Ativar ponte E2E (WebGL)", true)]
        private static bool EnableValidate() => !DefineEnabled();

        [MenuItem("PostLib/Cypress/Desativar ponte E2E (WebGL)", true)]
        private static bool DisableValidate() => DefineEnabled();

        private static void SetDefine(bool on)
        {
            var target = NamedBuildTarget.WebGL;
            var defines = PlayerSettings.GetScriptingDefineSymbols(target);
            var list = new List<string>(defines.Split(new[] { ';' },
                System.StringSplitOptions.RemoveEmptyEntries));

            var has = list.Contains(Define);
            if (on && !has) list.Add(Define);
            else if (!on && has) list.Remove(Define);

            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", list));
            Debug.Log($"[PostLib] Ponte Cypress {(on ? "ATIVADA" : "DESATIVADA")} para WebGL. " +
                      "Aguarde o recompile.");
        }
    }
}
