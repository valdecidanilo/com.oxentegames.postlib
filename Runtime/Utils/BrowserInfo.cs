using System.Runtime.InteropServices;
using UnityEngine;

namespace PostLib.Utils{
    public static class BrowserInfo {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string GetUserAgent();

        [DllImport("__Internal")]
        private static extern int IsMobileDevice();

        [DllImport("__Internal")]
        private static extern string GetPlatformHint();
#else
        // For Debug in Editor
        private static string GetUserAgent() => Application.unityVersion;
        private static int IsMobileDevice() => 0;
        private static string GetPlatformHint() => SystemInfo.operatingSystem;
#endif
        public static string UserAgent => GetUserAgent();
        /// <summary>
        /// (sec-ch-ua-mobile = ?1) 1 == 1 ? mobile : not mobile
        /// </summary>
        public static bool IsMobile => IsMobileDevice() == 1;
        public static bool IsDesktop => !IsMobile;

        /// <summary>
        /// (sec-ch-ua-platform) return =>  Windows/Android/iOS/Chrome OS/Chromium OS/Linux/macOS/Unknown
        /// </summary>
        public static string PlatformHint => GetPlatformHint();
    }
}