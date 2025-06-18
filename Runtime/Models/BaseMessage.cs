using PostLib.Utils;
using Newtonsoft.Json;
namespace PostLib.Models
{
     [System.Serializable]
     public class BaseMessage
     {
         [JsonProperty("_type")]
         public string rawType { get; set; }
         private PostMappings _postMappings;
         public PostMappings type
         {
             get => EnumUtils.ParseEnumByValue<PostMappings>(rawType);
             set => _postMappings = value;
         }
     }

}