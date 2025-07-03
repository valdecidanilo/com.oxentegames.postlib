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
        public static MessageHandler messageHandler;
        
        // Unity → JS 
        public static void SendInitialization()
        {
            InitializeHandler initializeHandler = new();
            initializeHandler.AddHandler(messageHandler);
            messageHandler = initializeHandler;
            
            HashSet<string> SupportedFeatures = new();
            messageHandler.AddFeatures(SupportedFeatures);
            var msg = new InitializationMessage
            {
                rawType  = "ucip.basic.g2wInitializationRequest",
                Version  = "1.0.0",
                Features = SupportedFeatures.ToArray()
            };
            PostBridge.Send(JsonConvert.SerializeObject(msg));
        }
        // JS → Unity 
        public static void Route(string json)
        {
            var any = JsonConvert.DeserializeObject<BaseMessage>(json);
            bool mapped = false;
            messageHandler.HandleMessage(any.rawType, json, ref mapped);
            if (!mapped) Debug.LogWarning($"[PostLib] Tipo não mapeado: {any.rawType}");
        }
    }

    public class InitializeHandler : MessageHandler
    {
        public override void HandleMessage(string type, string message, ref bool catchMessage)
        {
            switch (type)
            {
                case "ucip.basic.w2gInitializationResponse":
                    var init = JsonConvert.DeserializeObject<InitializationMessage>(message);
                    Debug.Log($"[PostLib] Initialization features: {string.Join(", ", init.Features)}");
                    catchMessage = true;
                    Initialize(init);
                    break;
            }
            
            base.HandleMessage(type, message, ref catchMessage);
        }
    }
}