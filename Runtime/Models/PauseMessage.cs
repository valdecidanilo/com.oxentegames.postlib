using Newtonsoft.Json;

namespace PostLib.Models
{
    [System.Serializable]
    public class PauseMessage : BaseMessage
    {
        [JsonProperty("pause")] public bool pause;
    }
}