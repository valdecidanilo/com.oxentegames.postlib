using UnityEngine;

namespace PostLib
{
    internal static class PostBridgeBootstrap
    {
        public static void CreateBridgeIfMissing()
        {
            if (Object.FindObjectOfType<PostBridge>() != null)
                return;
            Debug.Log("[PostLib] Inicializado.");
            var go = new GameObject("PostBridge");
            go.AddComponent<PostBridge>();
            Object.DontDestroyOnLoad(go);
        }
    }
}