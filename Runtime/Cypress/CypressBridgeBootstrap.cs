#if POSTLIB_CYPRESS
using UnityEngine;

namespace PostLib.Cypress
{
    /// <summary>
    /// Cria automaticamente o GameObject "CypressBridge" antes da primeira cena,
    /// sem exigir nenhuma configuração no jogo. Só é compilado sob POSTLIB_CYPRESS.
    /// </summary>
    public static class CypressBridgeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            if (Object.FindObjectOfType<CypressBridge>() != null) return;

            var go = new GameObject(CypressBridge.GameObjectName);
            go.AddComponent<CypressBridge>();
            Object.DontDestroyOnLoad(go);
            Debug.Log("[Cypress] Ponte E2E ativa (POSTLIB_CYPRESS). window.__cypress__ disponível.");
        }
    }
}
#endif
