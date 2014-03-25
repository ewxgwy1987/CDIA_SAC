#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       IncomingMessageInfo.cs
// Revision:      1.0 -   12 Jun 2009, By Xu Jian
// =====================================================================================
//
#endregion


using System;
using PALS.Telegrams;

namespace BHS
{
    /// <summary>
    /// Consist of information related to one message, e.g. ChannelName, Message, or other
    /// informations needed by individual message handlers.
    /// </summary>
    public class IncomingMessageInfo
    {

        /// <summary>
        /// Sender of incoming message.
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// Receiver of incoming message.
        /// </summary>
        public string Receiver { get; set; }

        /// <summary>
        /// The name of communication Channel where the connection is opened/closed, or 
        /// message is received from. One Chain could have multiple channel connections.
        /// </summary>
        public string ChannelName { get; set; }
        
        /// <summary>
        /// Reference to the incoming message.
        /// </summary>
        public Telegram Message { get; set; }
        
        /// <summary>
        /// The Gateway application code extracted from incoming GRNF message.
        /// </summary>
        public string GRNF_AppCode { get; set; }

        /// <summary>
        /// class constructor.
        /// </summary>
        /// <param name="sender">Incoming message sender name</param>
        /// <param name="receiver">Incoming message receiver name</param>
        /// <param name="channelName">Incoming message channel name</param>
        /// <param name="message">Incoming message</param>
        public IncomingMessageInfo(string sender, string receiver, string channelName, Telegram message)
        {
            Sender = sender;
            Receiver = receiver;
            ChannelName = channelName;
            Message = message;

            GRNF_AppCode = string.Empty; 
        }
    }
    
}
