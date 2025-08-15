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
            Debug.Log("[PostLib] 📤 Enviando inicialização...");
            
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
            
            Debug.Log($"[PostLib] 📤 Features suportadas: {string.Join(", ", SupportedFeatures)}");
            PostBridge.Send(JsonConvert.SerializeObject(msg));
        }
        
        // JS → Unity 
        public static void Route(string json)
        {
            Debug.Log($"[PostLib] 📥 Roteando mensagem: {json}");
            
            try
            {
                var any = JsonConvert.DeserializeObject<BaseMessage>(json);
                if (any == null)
                {
                    Debug.LogError("[PostLib] ❌ Falha ao deserializar mensagem!");
                    return;
                }
                
                Debug.Log($"[PostLib] 🎯 Tipo da mensagem: {any.rawType}");
                
                if (messageHandler == null)
                {
                    Debug.LogError("[PostLib] ❌ messageHandler é NULL! Handlers não foram inicializados.");
                    return;
                }
                
                bool mapped = false;
                messageHandler.HandleMessage(any.rawType, json, ref mapped);
                
                if (!mapped)
                {
                    Debug.LogWarning($"[PostLib] ⚠️ Tipo não mapeado: {any.rawType}");
                    Debug.LogWarning($"[PostLib] ⚠️ JSON completo: {json}");
                }
                else
                {
                    Debug.Log($"[PostLib] ✅ Mensagem {any.rawType} processada com sucesso!");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PostLib] ❌ Erro no roteamento: {ex.Message}\n{ex.StackTrace}");
                Debug.LogError($"[PostLib] ❌ JSON problemático: {json}");
            }
        }
    }

    public class InitializeHandler : MessageHandler
    {
        public override void HandleMessage(string type, string message, ref bool catchMessage)
        {
            Debug.Log($"[PostLib] 🔄 InitializeHandler processando: {type}");
            
            switch (type)
            {
                case "ucip.basic.w2gInitializationResponse":
                    var init = JsonConvert.DeserializeObject<InitializationMessage>(message);
                    Debug.Log($"[PostLib] 🎉 Initialization features: {string.Join(", ", init.Features)}");
                    catchMessage = true;
                    Initialize(init);
                    break;
            }
            
            base.HandleMessage(type, message, ref catchMessage);
        }
    }
}