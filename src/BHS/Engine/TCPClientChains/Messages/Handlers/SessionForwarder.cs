#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       SessionForwarder.cs
// Revision:      1.0 -   02 Apr 2009, By Xu Jian
// =====================================================================================
//
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using PALS.Net;
using PALS.Telegrams;

namespace BHS.Engine.TCPClientChains.Messages.Handlers
{
    /// <summary>
    /// Message Forwarder of Engine Service application TCPClient communication chain. 
    /// <para>
    /// This forwarder forwards the incoming messages sent from Gateway service applications 
    /// to MessageHandler of Engine service application (e.g. SortEngine service, CCTVEngine 
    /// service, BISEngine/FISEngine service, etc.). Or send outgoing messages passed down from 
    /// MessageHandler to Gateway service TCPClient communication chain.
    /// </para>
    /// </summary>
    public class SessionForwarder : PALS.Net.Handlers.SessionHandler
    {
        
        #region Class fields and Properties Declaration

        // The name of current class 
        private static readonly string _className =
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
        // Create a logger for use in this class
        private static readonly log4net.ILog _logger =
                    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Upon ConnectionOpened() method is invoked by bottom chain class,
        // the ChannelName will be stored into this ArrayList.
        // Once the bottom protocol layer connection is closed, its ChannelName
        // will be removed from this ArrayList accordingly.
        private List<string> _channelList;

        private Object _thisLock = new Object();

        /// <summary>
        /// Event will be raised when specific channel connection of Gateway-Internal Engine 
        /// Service Application chain is opened.
        /// </summary>
        public event EventHandler<MessageEventArgs> OnConnected;
        /// <summary>
        /// Event will be raised when specific channel connection of Gateway-Internal Engine 
        /// Service Application chain is closed.
        /// </summary>
        public event EventHandler<MessageEventArgs> OnDisconnected;
        /// <summary>
        /// Event will be raised when message is received from engine service applications.
        /// </summary>
        public event EventHandler<MessageEventArgs> OnReceived;

        #endregion

        #region Class Constructor, Dispose, & Destructor

        /// <summary>
        /// Class constructer.
        /// </summary>
        public SessionForwarder(PALS.Common.IParameters Param)
        {
            if (!Init(ref Param))
                throw new Exception("Creating class " + _className +
                    " object failed! <BHS.Engine.TCPClientChains.Messages.Handlers.SessionForwarder.Constructor()>");
        }

        /// <summary>
        /// Class destructer.
        /// </summary>
        ~SessionForwarder()
        {
            Dispose(false);
        }

        /// <summary>
        /// Class method to be called by class wrapper for release resources explicitly.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        // Dispose(bool disposing) executes in two distinct scenarios. If disposing equals true, 
        // the method has been called directly or indirectly by a user's code. Managed and 
        // unmanaged resources can be disposed.
        // If disposing equals false, the method has been called by the runtime from inside the 
        // finalizer and you should not reference other objects. Only unmanaged resources can be disposed.
        private void Dispose(bool disposing)
        {
            // Release managed & unmanaged resources...
            if (disposing)
            {
                if (_logger.IsInfoEnabled)
                    _logger.Info("Class:[" + _className + "] object is being destroyed... <" + _className + ".Dispose()>");
            }

            // Add codes here to release resource
            if (_channelList != null)
            {
                _channelList.Clear();
                _channelList = null;
            }

            if (disposing)
            {
                if (_logger.IsInfoEnabled)
                    _logger.Info("Class:[" + _className + "] object has been destroyed. <" + _className + ".Dispose()>");
            }
        }

        #endregion
        
        #region Class Overrides Method Declaration.

        /// <summary>
        /// Overridden of base class Init() method.
        /// </summary>
        /// <param name="Param"></param>
        /// <returns></returns>
        protected override bool Init(ref PALS.Common.IParameters Param)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            if (_logger.IsInfoEnabled)
                _logger.Info("Class:[" + _className + "] object is initializing... <" + thisMethod + ">");

            // Create ArrayList object for store opened channel connection name list
            _channelList = new List<string>();

            if (_logger.IsInfoEnabled)
                _logger.Info("Class:[" + _className + "] object has been initialized. <" + thisMethod + ">");

            return true;
        }

        /// <summary>
        /// Overridden of base class ConnectionOpened() method.
        /// </summary>
        /// <param name="channelName"></param>
        public override void ConnectionOpened(string channelName)
        {
            lock (_thisLock)
            {
                if (_channelList.Contains(channelName) == false)
                    _channelList.Add(channelName);
            }

            // Copy to a temporary variable to be thread-safe.
            EventHandler<MessageEventArgs> temp = OnConnected;
            // Event could be null if there are no subscribers, so check it before raise event
            if (temp != null)
            {
                // Raise OnReceived event upon message is received.
                temp(this, new MessageEventArgs(string.Empty, channelName, _channelList.Count, null));
            }
        }

        /// <summary>
        /// Overridden of base class ConnectionClosed() method.
        /// </summary>
        /// <param name="channelName"></param>
        public override void ConnectionClosed(string channelName)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            // Ignore the OnDisconnected event if it is not in the opened channel list.
            if (_channelList.Contains(channelName) == true)
            {
                lock (_thisLock)
                {
                    _channelList.Remove(channelName);
                }
            }

            // Copy to a temporary variable to be thread-safe.
            EventHandler<MessageEventArgs> temp = OnDisconnected;
            // Event could be null if there are no subscribers, so check it before raise event
            if (temp != null)
            {
                // Raise OnReceived event upon message is received.
                temp(this, new MessageEventArgs(string.Empty, channelName, _channelList.Count, null));
            }
        
        }


        /// <summary>
        /// Forward incoming message to upper layer by OnReceived() event firing upon message is received.
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="message"></param>
        public override void MessageReceived(string channelName, ref Telegram message)
        {
            // Copy to a temporary variable to be thread-safe.
            EventHandler<MessageEventArgs> temp = OnReceived;
            // Event could be null if there are no subscribers, so check it before raise event
            if (temp != null)
            {
                // Raise OnReceived event upon message is received.
                temp(this, new MessageEventArgs(string.Empty, channelName, _channelList.Count, message));
            }
        }

        /// <summary>
        /// Close the connection of specified name of channel. If value null is passed to this
        /// method, then all connections of this chain will be closed.
        /// </summary>
        /// <param name="channelName">null</param>
        public override void Disconnect(string channelName)
        {
            if (channelName == string.Empty)
            {
                // If no channel name is given, then close all connections of the chain.
                lock (_thisLock)
                {
                    _channelList.Clear();
                }
            }
            else
            {
                lock (_thisLock)
                {
                    if (_channelList.Contains(channelName))
                        _channelList.Remove(channelName);
                }
            }

            // Invoke next chaine class Disconnect() method to close the channel connection at next chain layer.
            if (m_HasNextChain)
                ((PALS.Net.Common.AbstractProtocolChain)m_NextChain).Disconnect(channelName);
        }

        /// <summary>
        /// Send outgoing message to engine service via all opened channel connections.
        /// <para>If no any channel connection is not opened, then the outgoing message will 
        /// be discarded.</para>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool Send(PALS.Telegrams.Telegram message)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            bool rtn = false;

            if (message == null)
                return rtn;

            try
            {
                int count = _channelList.Count;
                if (count == 0)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("No connection is opened to internal Engine Service applications! Message will be discarded. [Msg(APP):" +
                                message.ToString(PALS.Utilities.HexToStrMode.ToAscPaddedHexString) + "]. <"
                                + thisMethod + ">");
                    rtn = false;
                }
                else
                {
                    // Send message to all opened channels.
                    for (int i = 0; i < count; i++)
                    {
                        string channelName = _channelList[i];
                        message.ChannelName = channelName;

                        if (_logger.IsDebugEnabled)
                            _logger.Debug("[Channel:" + channelName + "] -> [Msg(APP):" +
                                    message.ToString(PALS.Utilities.HexToStrMode.ToAscPaddedHexString) + "]. <"
                                    + thisMethod + ">");

                        // Call base class Sent() method to send message
                        this.Send(channelName, ref message);
                    }

                    rtn = true;
                }

                return rtn;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Sending message failed! [Msg(APP):" +
                            message.ToString(PALS.Utilities.HexToStrMode.ToAscPaddedHexString) + "]. <"
                            + thisMethod + ">", ex);
                return false;
            }
        }

        #endregion
        
        #region Class Method Declaration.

        // Add class methods here...

        #endregion

    }
}
