
using System.Collections.Generic;

namespace PostLib.Models
{
    public abstract class MessageHandler
    {
        /// <summary>
        /// Should pass the message even after catching it?
        /// </summary>
        protected bool passMessageAfterCatch;
        protected MessageHandler nextHandler;

        /// <summary>
        /// Add features message that this handler and the next support
        /// </summary>
        /// <param name="set">HashSet these features will be added to</param>
        public virtual void AddFeatures(HashSet<string> set)
        {
            nextHandler?.AddFeatures(set);
        }
        public MessageHandler AddHandler(MessageHandler newHandler)
        {
            if (nextHandler == null) nextHandler = newHandler;
            else nextHandler.AddHandler(newHandler);
            return newHandler;
        }

        public virtual void Initialize(InitializationMessage message)
        {
            nextHandler?.Initialize(message);
        }

        public virtual void HandleMessage(string type, string message, ref bool catchMessage)
        {
            if (!catchMessage || passMessageAfterCatch)
                nextHandler?.HandleMessage(type, message, ref catchMessage);
        }
    }
}