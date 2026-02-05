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

        private static string _requestType;
        private static string _responseType;
        private static string _version = "1.0.0";

        public static void SetInitializationData(string requestType, string responseType, string version = "1.0.0")
        {
            _requestType = requestType;
            _responseType = responseType;
            _version = version;
        }

        // Unity → JS 
        public static void SendInitialization()
        {
            if (string.IsNullOrEmpty(_requestType) || string.IsNullOrEmpty(_responseType))
            {
                Debug.Log("[PostLib] Erro ao enviar inicialização:\nrequestType/responseType estão nulos;" +
                    " modifique os valores chamando o método MessageRouter.SetInitializationData()");
                return;
            }
            Debug.Log("[PostLib] 📤 Enviando inicialização...");

            InitializeHandler initializeHandler = new(_responseType);
            initializeHandler.AddHandler(messageHandler);
            messageHandler = initializeHandler;

            HashSet<string> SupportedFeatures = new();
            messageHandler.AddFeatures(SupportedFeatures);

            var msg = new InitializationMessage
            {
                rawType = _requestType,
                Version = _version,
                Features = SupportedFeatures.ToArray()
            };
            //Não remover este log a baixo, se não version nao aparece.
            Debug.Log($"[PostLib] 📤 Features suportadas: {string.Join(", ", SupportedFeatures)} - version: {msg.Version}");
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
        private readonly string initializationResponseMessageType;

        public InitializeHandler(string responseType) => initializationResponseMessageType = responseType;

        public override void HandleMessage(string type, string message, ref bool catchMessage)
        {
            Debug.Log($"[PostLib] 🔄 InitializeHandler processando: {type}");

            if (type.Equals(initializationResponseMessageType))
            {
                var init = JsonConvert.DeserializeObject<InitializationMessage>(message);
                Debug.Log($"[PostLib] 🎉 Initialization features: {string.Join(", ", init.Features)}");
                catchMessage = true;
                Initialize(init);
            }

            base.HandleMessage(type, message, ref catchMessage);
        }
    }
}