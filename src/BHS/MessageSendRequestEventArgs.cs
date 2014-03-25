#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       MessageSendRequestEventArgs.cs
// Revision:      1.0 -   02 Apr 2009, By Xu Jian
// =====================================================================================
//
#endregion

using System;
using PALS.Telegrams;

namespace BHS
{
    /// <summary>
    /// Define a class to hold the parameters of fired event whenever connection is 
    /// opened or closed, or message is received.
    /// </summary>
    public class MessageSendRequestEventArgs : EventArgs
    {

        /// <summary>
        /// Sender of outgoing message.
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// Receiver of outgoing message.
        /// </summary>
        public string Receiver { get; set; }

        /// <summary>
        /// The name of communication Channel where the connection is opened/closed, or 
        /// message is sent to. 
        /// </summary>
        public string ChannelName { get; set; }

        /// <summary>
        /// The outgoing message.
        /// </summary>
        public Telegram Message { get; set; }

        /// <summary>
        /// class constructor.
        /// </summary>
        /// <param name="sender">Sender of outgoing message.</param>
        /// <param name="receiver">Receiver of outgoing message.</param>
        /// <param name="channelName">The name of communication Channel where the connection is 
        /// opened/closed, or message is received from.</param>
        /// <param name="message">The received message</param>
        public MessageSendRequestEventArgs(string sender, string receiver, string channelName, Telegram message)
        {
            Sender = sender;
            Receiver = receiver;
            ChannelName = channelName;
            Message = message;
        }
    }
    
}
