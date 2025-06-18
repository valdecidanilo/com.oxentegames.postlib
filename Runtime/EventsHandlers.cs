using System;
using UnityEngine;
using PostLib.Models;

namespace PostLib
{
    public class EventsHandlers
    {
        public static Action OnInterruptAutoplay;
        public static void HandleReceived(InitializationMessage msg)
        {
            Debug.Log($"[PostLib] Initialization features: {string.Join(", ", msg.Features)}");
            
        }
        public static void HandleInterruptAutoplay()
        {
            Debug.Log("[PostLib] InterruptAutoplay");
            OnInterruptAutoplay?.Invoke();
        }
    }
}