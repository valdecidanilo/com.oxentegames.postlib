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
#if !UNITY_EDITOR && UNITY_WEBGL
            JS_Receive();
#endif
            MessageRouter.SendInitialization();
        }

        public void OnReceive(string json) => MessageRouter.Route(json);
        public static void Send(string json)
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            JS_Send(json);
#else
            Debug.Log($"[PostLib] {json}");
#endif
        }
    }
}