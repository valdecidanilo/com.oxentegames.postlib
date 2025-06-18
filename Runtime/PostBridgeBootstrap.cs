using UnityEngine;

namespace PostLib
{
    internal static class PostBridgeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateBridgeIfMissing()
        {
            if (Object.FindObjectOfType<PostBridge>() != null)
                return;
#if UNITY_EDITOR
            Debug.Log("[PostLib] Inicializado.");
#endif
            var go = new GameObject("PostBridge");
            go.AddComponent<PostBridge>();
            Object.DontDestroyOnLoad(go);

        }
    }
}