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
        private void Awake()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            JS_Receive();
#endif
            Debug.Log("[PostLib] Escutando mensagens.");
            MessageRouter.SendInitialization();
        }

        public void OnReceive(object jsonObject) => MessageRouter.Route(JsonConvert.SerializeObject(jsonObject));
        public static void Send(string json)
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            JS_Send(json);
#endif
            Debug.Log($"[PostLib] {json}");
        }
    }
}