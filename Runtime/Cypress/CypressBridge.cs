#if POSTLIB_CYPRESS
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PostLib.Cypress
{
    /// <summary>
    /// Ponte de testes E2E para Cypress (e qualquer runner baseado em DOM/JS).
    ///
    /// Unity WebGL renderiza tudo dentro de um único &lt;canvas&gt;: não existe DOM para
    /// o Cypress consultar ou clicar. Esta ponte resolve isso expondo uma API de teste
    /// em <c>window.__cypress__</c>:
    ///
    ///   • Cypress -> Unity (ações):  win.unityInstance.SendMessage('CypressBridge', 'Exec', json)
    ///   • Unity -> Cypress (estado): C# escreve em window.__cypress__.state via Cypress.jslib
    ///
    /// TODO o tipo só é compilado quando o define <c>POSTLIB_CYPRESS</c> está presente,
    /// garantindo pegada zero em builds de produção.
    /// </summary>
    public class CypressBridge : MonoBehaviour
    {
        public const string GameObjectName = "CypressBridge";

        // ---- Externs implementados em Cypress.jslib (só existem no build WebGL) ----
        [DllImport("__Internal")] private static extern void Cypress_SetState(string json);
        [DllImport("__Internal")] private static extern void Cypress_Emit(string evt, string json);
        [DllImport("__Internal")] private static extern void Cypress_Ready();
        [DllImport("__Internal")] private static extern void Cypress_Ack(string id, string json);

        // Registro de ações e provedores de estado (case-insensitive).
        private static readonly Dictionary<string, Action<string>> _actions =
            new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Func<object>> _states =
            new Dictionary<string, Func<object>>(StringComparer.OrdinalIgnoreCase);

        private static CypressBridge _instance;

        /// <summary>Último snapshot serializado — útil para inspeção no Editor.</summary>
        public static string LastStateJson { get; private set; }

        /// <summary>True quando a ponte está viva (define ativo e GameObject criado).</summary>
        public static bool IsActive => _instance != null;

        // ============================================================
        //  API EXPLÍCITA (recomendada — sem reflection, sem code stripping)
        // ============================================================

        /// <summary>Registra uma ação chamável pelo Cypress. O argumento chega como string.</summary>
        public static void RegisterAction(string name, Action<string> handler)
        {
            if (string.IsNullOrEmpty(name) || handler == null) return;
            _actions[name] = handler;
        }

        /// <summary>Registra um provedor de estado. O valor é serializado para JSON no snapshot.</summary>
        public static void RegisterState(string name, Func<object> provider)
        {
            if (string.IsNullOrEmpty(name) || provider == null) return;
            _states[name] = provider;
        }

        public static void UnregisterAction(string name) => _actions.Remove(name);
        public static void UnregisterState(string name) => _states.Remove(name);

        // ============================================================
        //  API DECLARATIVA (atributos [CypressAction] / [CypressState])
        // ============================================================

        /// <summary>
        /// Varre <paramref name="target"/> e registra todos os membros marcados com
        /// [CypressAction] / [CypressState]. Chame uma vez (ex.: no Awake do objeto).
        /// Cobre membros de instância e estáticos.
        /// </summary>
        public static void Bind(object target)
        {
            if (target == null) return;
            var type = target.GetType();
            const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic |
                                   BindingFlags.Instance | BindingFlags.Static;

            foreach (var m in type.GetMethods(F))
            {
                var action = m.GetCustomAttribute<CypressActionAttribute>();
                if (action != null)
                {
                    var name = string.IsNullOrEmpty(action.Name) ? m.Name : action.Name;
                    var ps = m.GetParameters();
                    var self = m.IsStatic ? null : target;
                    RegisterAction(name, arg =>
                    {
                        var args = ps.Length == 0
                            ? null
                            : new[] { ConvertArg(ps[0].ParameterType, arg) };
                        m.Invoke(self, args);
                    });
                }

                var state = m.GetCustomAttribute<CypressStateAttribute>();
                if (state != null && m.GetParameters().Length == 0 && m.ReturnType != typeof(void))
                {
                    var name = string.IsNullOrEmpty(state.Name) ? m.Name : state.Name;
                    var self = m.IsStatic ? null : target;
                    RegisterState(name, () => m.Invoke(self, null));
                }
            }

            foreach (var p in type.GetProperties(F))
            {
                var state = p.GetCustomAttribute<CypressStateAttribute>();
                if (state != null && p.CanRead)
                {
                    var name = string.IsNullOrEmpty(state.Name) ? p.Name : state.Name;
                    var self = p.GetGetMethod(true).IsStatic ? null : target;
                    RegisterState(name, () => p.GetValue(self));
                }
            }

            foreach (var f in type.GetFields(F))
            {
                var state = f.GetCustomAttribute<CypressStateAttribute>();
                if (state != null)
                {
                    var name = string.IsNullOrEmpty(state.Name) ? f.Name : state.Name;
                    var self = f.IsStatic ? null : target;
                    RegisterState(name, () => f.GetValue(self));
                }
            }
        }

        private static object ConvertArg(Type t, string raw)
        {
            if (t == typeof(string)) return raw;
            if (string.IsNullOrEmpty(raw))
                return t.IsValueType ? Activator.CreateInstance(t) : null;

            var ci = CultureInfo.InvariantCulture;
            if (t == typeof(int)) return int.Parse(raw, ci);
            if (t == typeof(long)) return long.Parse(raw, ci);
            if (t == typeof(float)) return float.Parse(raw, ci);
            if (t == typeof(double)) return double.Parse(raw, ci);
            if (t == typeof(decimal)) return decimal.Parse(raw, ci);
            if (t == typeof(bool)) return bool.Parse(raw);
            if (t.IsEnum) return Enum.Parse(t, raw, ignoreCase: true);

            // Qualquer outro tipo: tenta desserializar o argumento como JSON.
            return JsonConvert.DeserializeObject(raw, t);
        }

        // ============================================================
        //  Ciclo de vida
        // ============================================================

        private void Awake()
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            JsReady();
            PublishState();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // ============================================================
        //  JS -> Unity: ponto de entrada único chamado via SendMessage
        //  Formato: {"id":"<opcional>","action":"<nome>","arg":<opcional>}
        // ============================================================
        public void Exec(string json)
        {
            string id = null;
            try
            {
                var cmd = JObject.Parse(json);
                id = (string)cmd["id"];
                var action = (string)cmd["action"];
                var argToken = cmd["arg"];
                var arg = argToken == null || argToken.Type == JTokenType.Null
                    ? string.Empty
                    : (argToken.Type == JTokenType.String ? (string)argToken : argToken.ToString(Formatting.None));

                if (string.IsNullOrEmpty(action))
                {
                    Ack(id, new { ok = false, error = "missing 'action'" });
                }
                else if (_actions.TryGetValue(action, out var handler))
                {
                    handler(arg);
                    Ack(id, new { ok = true, action });
                }
                else
                {
                    Ack(id, new { ok = false, error = $"unknown action '{action}'" });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Cypress] Exec falhou: {ex.Message}\n{ex.StackTrace}");
                Ack(id, new { ok = false, error = ex.Message });
            }

            // Republica o estado após qualquer comando, para o Cypress reasserir.
            PublishState();
        }

        /// <summary>Recalcula e publica o snapshot de estado em window.__cypress__.state.</summary>
        public void PublishState(string _ = null)
        {
            var snapshot = new Dictionary<string, object>();
            foreach (var kv in _states)
            {
                try { snapshot[kv.Key] = kv.Value(); }
                catch (Exception ex) { snapshot[kv.Key] = "ERR: " + ex.Message; }
            }

            LastStateJson = JsonConvert.SerializeObject(snapshot);
            JsSetState(LastStateJson);
        }

        /// <summary>
        /// Empurra um evento pontual para window.__cypress__.events (ex.: "spin_end",
        /// "big_win"). Útil para o Cypress esperar por acontecimentos discretos.
        /// </summary>
        public static void Emit(string evt, object data = null)
        {
            if (_instance == null) return;
            var json = data == null ? "null" : JsonConvert.SerializeObject(data);
            _instance.JsEmit(evt, json);
        }

        private void Ack(string id, object payload)
        {
            if (string.IsNullOrEmpty(id)) return;
            JsAck(id, JsonConvert.SerializeObject(payload));
        }

        // ---- Wrappers de plataforma: no Editor os externs não existem ----
        private void JsSetState(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { Cypress_SetState(json); } catch (Exception ex) { Debug.LogError("[Cypress] SetState: " + ex.Message); }
#endif
        }

        private void JsEmit(string evt, string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { Cypress_Emit(evt, json); } catch (Exception ex) { Debug.LogError("[Cypress] Emit: " + ex.Message); }
#else
            Debug.Log($"[Cypress] (editor) emit {evt}: {json}");
#endif
        }

        private void JsReady()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { Cypress_Ready(); } catch (Exception ex) { Debug.LogError("[Cypress] Ready: " + ex.Message); }
#else
            Debug.Log("[Cypress] (editor) bridge pronta.");
#endif
        }

        private void JsAck(string id, string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { Cypress_Ack(id, json); } catch (Exception ex) { Debug.LogError("[Cypress] Ack: " + ex.Message); }
#endif
        }
    }
}
#endif
