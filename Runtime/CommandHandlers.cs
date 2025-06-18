using UnityEngine;
using PostLib.Models;

namespace PostLib
{
    public static class CommandHandlers
    {
        public static void HandleInitialization(InitializationMessage msg)
        {
            Debug.Log($"[PostLib] Initialization features: {string.Join(", ", msg.Features)}");
            // habilitar recursos de acordo com msg.features
        }
        public static void HandlePause(PauseMessage msg)
        {
            Debug.Log($"[PostLib] jogo {(msg.pause ? "PAUSADO" : "RETOMADO")}");
            Time.timeScale = msg.pause ? 0 : 1;
        }
        public static void HandleClose()
        {
            Debug.Log("[PostLib] Encerrando aplicação…");
#if UNITY_WEBGL && !UNITY_EDITOR
            Application.ExternalEval("window.close()");
#else
            Application.Quit();
#endif
        }
    }
}