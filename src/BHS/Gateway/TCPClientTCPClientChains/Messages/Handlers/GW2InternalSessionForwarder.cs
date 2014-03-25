#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       GW2InternalSessionForwarder.cs
// Revision:      1.0 -   02 Apr 2009, By Xu Jian
// =====================================================================================
//
#endregion

using System;
using System.Collections;
using PALS.Net;
using PALS.Telegrams;

namespace BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers
{
    /// <summary>
    /// Message Forwarder of Gateway Service application Gateway-Internal communication chain. 
    /// <para>
    /// This forwarder forwards the incoming messages received from BHS internal service applications 
    /// (e.g. SortEngine service, CCTVEngine service, BISEngine/FISEngine service, etc.), to 
    /// MessageHandler of gateway service application. Or send outgoing messages passed down from 
    /// MessageHandler to BHS external devices (e.g. PLCs, CCTV Server, BIS/FIS server, etc.) via 
    /// Gateway-External communication chain.
    /// </para>
    /// </summary>
    public class GW2InternalSessionForwarder : PALS.Net.Handlers.SessionHandler
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
        private ArrayList _connectedChannelList;
        // Creates a synchronized wrapper around the ArrayList.
        private ArrayList _syncdConnectedChannelList;

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
        public GW2InternalSessionForwarder(PALS.Common.IParameters Param)
        {
            if (!Init(ref Param))
                throw new Exception("Creating class " + _className +
                    " object failed! <BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.GW2InternalSessionForwarder.Constructor()>");
        }

        /// <summary>
        /// Class destructer.
        /// </summary>
        ~GW2InternalSessionForwarder()
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
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            // Release managed & unmanaged resources...
            if (disposing)
            {
                if (_logger.IsInfoEnabled)
                    _logger.Info("Class:[" + _className + "] object is being destroyed... <" + thisMethod + ">");
            }

            // Add codes here to release resource
            if (_syncdConnectedChannelList != null)
            {
                lock (_syncdConnectedChannelList.SyncRoot)
                {
                    _syncdConnectedChannelList.Clear();
                }
                _syncdConnectedChannelList = null;
            }

            if (disposing)
            {
                if (_logger.IsInfoEnabled)
                    _logger.Info("Class:[" + _className + "] object has been destroyed. <" + thisMethod + ">");
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

            _connectedChannelList = new ArrayList();
            _syncdConnectedChannelList = ArrayList.Synchronized(_connectedChannelList);
            _syncdConnectedChannelList.Clear();

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
            lock (_syncdConnectedChannelList.SyncRoot)
            {
                if (_syncdConnectedChannelList.Contains(channelName) == false)
                    _syncdConnectedChannelList.Add(channelName);
            }

            // Copy to a temporary variable to be thread-safe.
            EventHandler<MessageEventArgs> temp = OnConnected;
            // Event could be null if there are no subscribers, so check it before raise event
            if (temp != null)
            {
                // Raise OnReceived event upon message is received.
                temp(this, new MessageEventArgs(string.Empty, channelName, _syncdConnectedChannelList.Count, null));
            }
        }

        /// <summary>
        /// Overridden of base class ConnectionClosed() method.
        /// </summary>
        /// <param name="channelName"></param>
        public override void ConnectionClosed(string channelName)
        {
            lock (_syncdConnectedChannelList.SyncRoot)
            {
                if (_syncdConnectedChannelList.Contains(channelName) == true)
                    _syncdConnectedChannelList.Remove(channelName);
            }

            // Copy to a temporary variable to be thread-safe.
            EventHandler<MessageEventArgs> temp = OnDisconnected;
            // Event could be null if there are no subscribers, so check it before raise event
            if (temp != null)
            {
                // Raise OnReceived event upon message is received.
                temp(this, new MessageEventArgs(string.Empty, channelName, _syncdConnectedChannelList.Count, null));
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
                temp(this, new MessageEventArgs(string.Empty, channelName, _syncdConnectedChannelList.Count, message));
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
                string channel;
                lock (_syncdConnectedChannelList.SyncRoot)
                {
                    while (_syncdConnectedChannelList.Count > 0)
                    {
                        channel = string.Empty;
                        channel = (string)_syncdConnectedChannelList[0];
                        _syncdConnectedChannelList.RemoveAt(0);
                    }
                }
            }
            else
            {
                lock (_syncdConnectedChannelList.SyncRoot)
                {
                    if (_syncdConnectedChannelList.Contains(channelName))
                        _syncdConnectedChannelList.Remove(channelName);
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
                int count = _syncdConnectedChannelList.Count;
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
                    for (int i = 0; i < count; i++)
                    {
                        string channelName = (string)_syncdConnectedChannelList[i];
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
