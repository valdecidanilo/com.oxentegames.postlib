using Newtonsoft.Json;

namespace PostLib.Models
{
    [System.Serializable]
    public class InitializationMessage : BaseMessage
    {
        [JsonProperty("version")] public string Version
        {
            get => "1.0.0";
            set => value = "1.0.0";
        }
        [JsonProperty("features")] public string[] Features { get; set; }
    }
}