using UnityEngine;

namespace Postlib
{
    [CreateAssetMenu(menuName = "Server Config")]
    public class ServerConfig : ScriptableObject
    {
        public string version = "v1";
        public string baseUrl = "http://localhost:8000";
        public string gameName = "gameName";

        public virtual string URL => BuildUrl();

        protected virtual string BuildUrl()
        {
            var b = NormalizeBaseUrl(baseUrl);
            var v = NormalizePath(version);
            var g = NormalizePath(gameName);

            return $"{b}/{v}/{g}/";
        }

        protected virtual string NormalizeBaseUrl(string value)
        {
            return (value ?? "").Trim().TrimEnd('/');
        }

        protected virtual string NormalizePath(string value)
        {
            return (value ?? "").Trim().Trim('/');
        }

        public virtual void SetServerConfig(string currentVersion, string currentBaseUrl, string currentGameName)
        {
            version = currentVersion;
            baseUrl = currentBaseUrl;
            gameName = currentGameName;
        }

        public virtual bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(baseUrl)
                   && baseUrl.StartsWith("http")
                   && !string.IsNullOrWhiteSpace(version)
                   && !string.IsNullOrWhiteSpace(gameName);
        }
    }
}