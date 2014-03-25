#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       MessageEventArgs.cs
// Revision:      1.0 -   02 Apr 2009, By Xu Jian
// =====================================================================================
//
#endregion


using System;

namespace BHS
{
    /// <summary>
    /// Define a class to hold the parameters of fired event whenever connection is 
    /// opened or closed, or message is received.
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        /// <summary>
        /// The name of communication Chain in which the event is fired. One Chain could have
        /// multiple channel connections.
        /// </summary>
        public string ChainName { get; set; }
        /// <summary>
        /// The name of communication Channel where the connection is opened/closed, or 
        /// message is received from. One Chain could have multiple channel connections.
        /// </summary>
        public string ChannelName { get; set; }
        /// <summary>
        /// The number of current opened channel connections.
        /// </summary>
        public int OpenedChannelCount { get; set; }
        /// <summary>
        /// The received message.
        /// </summary>
        public PALS.Telegrams.Telegram Message { get; set; }

        /// <summary>
        /// class constructor.
        /// </summary>
        /// <param name="chainName">The name of communication Chain in which the event is fired.</param>
        /// <param name="channelName">The name of communication Channel where the connection is 
        /// opened/closed, or message is received from.</param>
        /// <param name="openedChannelCount">The number of current opened channel connections.</param>
        /// <param name="message">The received message</param>
        public MessageEventArgs(string chainName, string channelName, int openedChannelCount, PALS.Telegrams.Telegram message)
        {
            ChainName = chainName;
            ChannelName = channelName;
            OpenedChannelCount = openedChannelCount;
            Message = message;
        }
    }
    
}
