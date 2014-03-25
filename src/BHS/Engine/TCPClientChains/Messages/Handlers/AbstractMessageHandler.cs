#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       AbstractMessageHandler.cs
// Revision:      1.0 -   04 Jun 2009, By Xu Jian
// =====================================================================================
//
#endregion

using PALS.Telegrams;

namespace BHS.Engine.TCPClientChains.Messages.Handlers
{
    /// <summary>
    /// null
    /// </summary>
    abstract public class AbstractMessageHandler
    {
        /// <summary>
        /// The reference of Persistor class object
        /// </summary>
        public Engine.TCPClientChains.DataPersistor.Database.Persistor DBPersistor { get; set; }

        /// <summary>
        /// ID of class object
        /// </summary>
        public string ObjectID { get; set; }

        /// <summary>
        /// Telegram format of message associated to current message handler.
        /// </summary>
        public TelegramFormat MessageFormat { get; set; }

        /// <summary>
        /// Common class method, to be available in all message handler classes.
        /// </summary>
        /// <param name="msgInfo"></param>
        public void MessageReceived(IncomingMessageInfo msgInfo)
        {
            MessageHandling(msgInfo);
        }

        /// <summary>
        /// Abstract method and need to be overrided by all message handler classes.
        /// </summary>
        /// <param name="msgInfo"></param>
        abstract protected void MessageHandling(IncomingMessageInfo msgInfo);
    }
}
