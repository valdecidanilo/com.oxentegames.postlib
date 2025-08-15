using System.Runtime.InteropServices;
using UnityEngine;

namespace PostLib
{
    public class PostBridge : MonoBehaviour
    {
        // JS ↔ C# 
        [DllImport("__Internal")] private static extern void JS_Receive();
        [DllImport("__Internal")] private static extern void JS_Send(string json);
        
        private void Awake()
        {
            Debug.Log("[PostLib] 🌉 PostBridge.Awake() - Configurando bridge...");
            
#if !UNITY_EDITOR && UNITY_WEBGL
            try
            {
                JS_Receive();
                Debug.Log("[PostLib] ✅ JS_Receive() chamado com sucesso");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PostLib] ❌ Erro ao chamar JS_Receive(): {ex.Message}");
            }
#else
            Debug.Log("[PostLib] ℹ️ Editor mode - JS_Receive() simulado");
#endif
            
            Debug.Log("[PostLib] 🔗 Ativando window Listener JS.");
            MessageRouter.SendInitialization();
        }

        public void OnReceive(string json)
        {
            Debug.Log($"[PostLib] 📨 OnReceive chamado com: {json}");
            
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[PostLib] ⚠️ JSON recebido está vazio!");
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
                Debug.Log("[PostLib] ✅ JS_Send() executado com sucesso");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PostLib] ❌ Erro ao chamar JS_Send(): {ex.Message}");
            }
#else
            Debug.Log("[PostLib] ℹ️ Editor mode - JS_Send() simulado");
#endif
        }
    }
}
