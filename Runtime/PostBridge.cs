using System.Runtime.InteropServices;
using UnityEngine;
using Newtonsoft.Json;

namespace PostLib
{
    public class PostBridge : MonoBehaviour
    {
        // JS ↔ C# 
        [DllImport("__Internal")] private static extern void JS_Receive();
        [DllImport("__Internal")] private static extern void JS_Send(string json);
        [DllImport("__Internal")] private static extern void Unity_GameReady();
        [DllImport("__Internal")] private static extern void WebGLLoading_SetBootProgress(float progress);
        [DllImport("__Internal")] private static extern void WebGLLoading_SetBootStatus(string status);
        [DllImport("__Internal")] private static extern void WebGLLoading_SetBootProgressWithStatus(float progress, string status);
        [DllImport("__Internal")] private static extern void WebGLLoading_Complete();
        private void Awake()
        {
            Debug.Log("[PostLib] Configurando bridge...");
            
#if !UNITY_EDITOR && UNITY_WEBGL
            try
            {
                JS_Receive();
                Debug.Log("[PostLib] chamado com sucesso");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PostLib] Erro ao chamar JS_Receive: {ex.Message}");
            }
#else
            Debug.Log("[PostLib] Editor mode - JS_Receive() simulado");
#endif
            
            Debug.Log("[PostLib] Ativando window Listener JS.");
            MessageRouter.SendInitialization();
        }

        public void OnReceive(string json)
        {
            Debug.Log($"[PostLib] 📨 OnReceive chamado com: {json}");
            json = UnwrapIfQuoted(json);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[PostLib] JSON recebido está vazio!");
                return;
            }
            
            try
            {
                MessageRouter.Route(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PostLib] ❌ Erro em OnReceive: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static void Send(string json)
        {
            Debug.Log($"[PostLib] 📤 Enviando: {json}");
            
#if !UNITY_EDITOR && UNITY_WEBGL
            try
            {
                JS_Send(json);
                Debug.Log("[PostLib] JS_Send() executado com sucesso");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PostLib] Erro ao chamar JS_Send(): {ex.Message}");
            }
#else
            Debug.Log("[PostLib] Editor mode - JS_Send() simulado");
#endif
        }
        public static void NotifyGameReady()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Unity_GameReady();
#endif
        }
        public static void SetBootProgress(float progress)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLLoading_SetBootProgress(Mathf.Clamp01(progress));
#else
            Debug.Log($"[WebGLLoadingBridge] Boot progress: {progress:P0}");
#endif
        }

        public static void SetBootProgress(float progress, string status)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLLoading_SetBootProgressWithStatus(Mathf.Clamp01(progress), status ?? string.Empty);
#else
            Debug.Log($"[WebGLLoadingBridge] Boot progress: {progress:P0} | {status}");
#endif
        }

        public static void SetStatus(string status)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLLoading_SetBootStatus(status ?? string.Empty);
#else
            Debug.Log($"[WebGLLoadingBridge] Status: {status}");
#endif
        }

        public static void Complete()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLLoading_Complete();
#else
            Debug.Log("[WebGLLoadingBridge] Complete");
#endif
        }
        private static string UnwrapIfQuoted(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return json;
            json = json.Trim();

            if (json.Length >= 2 && json[0] == '"' && json[json.Length - 1] == '"')
            {
                try
                {
                    var inner = JsonConvert.DeserializeObject<string>(json);
                    if (!string.IsNullOrWhiteSpace(inner)) return inner;
                }
                catch {

                }
            }
            return json;
        }
    }
}
