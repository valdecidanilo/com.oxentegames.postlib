using UnityEngine;

namespace PostLib.Utils
{
    public static class UrlOpener
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void JS_OpenUrlOverlay(string url);
#else
        // For Debug in Editor
        private static void JS_OpenUrl(string url) => Application.OpenURL(url);
#endif
        /// <summary>
        /// Opens the given URL in an overlay over the WebGL canvas, keeping the game loaded underneath.
        /// </summary>
        public static void Open(string url) => JS_OpenUrl(url);
    }
}
