using UnityEngine;

namespace PostLib
{
    internal static class PostBridgeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
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