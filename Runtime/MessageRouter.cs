using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using PostLib.Models;
using PostLib.Utils;

namespace PostLib
{
    public static class MessageRouter
    {
        public static readonly HashSet<string> SupportedFeatures = new()
        {
            "basic",
            "pause",
            "autoplay"
        };
        
        // Unity → JS 
        public static void SendInitialization()
        {
            var msg = new InitializationMessage
            {
                type     = PostMappings.InitializationRequest,
                rawType  = PostMappings.InitializationRequest.GetEnumMemberValue(),
                Version  = "1.0.0",
                Features = SupportedFeatures.ToArray()
            };
            PostBridge.Send(JsonConvert.SerializeObject(msg));
        }
        // JS → Unity 
        public static void Route(string json)
        {
            var any = JsonConvert.DeserializeObject<BaseMessage>(json);
            switch (any.type)
            {
                case PostMappings.InitializationRequest:
                    var init = JsonConvert.DeserializeObject<InitializationMessage>(json);
                    CommandHandlers.HandleInitialization(init);
                    break;
                case PostMappings.InitializationResponse:
                    var response = JsonConvert.DeserializeObject<InitializationMessage>(json);
                    EventsHandlers.HandleReceived(response);
                    break;
                case PostMappings.AutoPlayInterruptCommand:
                    EventsHandlers.HandleInterruptAutoplay();
                    break;
                case PostMappings.GamePauseHandler:
                    var pause = JsonConvert.DeserializeObject<PauseMessage>(json);
                    CommandHandlers.HandlePause(pause);
                    break;
                case PostMappings.CloseGameHandler:
                    CommandHandlers.HandleClose();
                    break;
                default:
                    Debug.LogWarning($"[PostLib] Tipo não mapeado: {any.type.GetEnumMemberValue()}");
                    break;
            }
        }
    }
}