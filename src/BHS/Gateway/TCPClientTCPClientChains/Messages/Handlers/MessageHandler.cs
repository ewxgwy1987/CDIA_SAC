#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       MessageHandler.cs
// Revision:      1.0 -   02 Apr 2009, By Xu Jian
// =====================================================================================
//
#endregion

using System;
using System.Collections;
using System.Threading;
using PALS.Telegrams;
using PALS.Utilities;

namespace BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers
{
    /// <summary>
    /// Application layer message handler of Gateway Service application. The incoming application 
    /// message from both Gateway-Internal and Gateway-External communication chains will be forwarded 
    /// to this class by the OnReceived() event fired by most top class, GW2InternalSessionForwarder
    /// and GW2ExternalSessionForwarder, in both chains. 
    /// <para>
    /// If messages come from Gateway-Externall chain, then this MessageHandler class will forward them
    /// to Gateway-Internal chain for sending out to BHS Engine Service application. As per the 
    /// <![CDATA[TCPServer&Client]]> protocol design, all messages received by Gateway-External chain
    /// and reach this MessageHandler class shall be forward to Engine Service application for business
    /// logic handling.
    /// </para>
    /// <para>
    /// If messages come from Gateway-Internal chain, and this message needs to be sent to external
    /// devices (e.g. PLCs, CCTV server, BIS/FIS servers, etc.), then this MessageHandler class will 
    /// forward them to Gateway-Internal chain for sending out to external devices. As per the 
    /// <![CDATA[TCPServer&Client]]> protocol design, not all messages received by Gateway-Internal
    /// chain and reach this MessageHandler class shall be forward to external devices. For example:
    /// ServiceStatusRequest message shall be handled by this MessageHandler class itself, instead
    /// of forward to external devices.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// There is a class level internal buffer will be created when class is instantiated for storing
    /// incoming messages. All incoming messages will be stored in this internal buffer. One seperate
    /// thread is running in background to retrieve the incoming messages one by one from this buffer,
    /// and handle it according to the business rules.
    /// </para>
    /// <para>There is no outgoing message queue was implemented in this layer class. It because all 
    /// acknowledge required outgoing messages will be buffered by bottom ACK class. Such message won't 
    /// be lost in case of the sending process failure or it is not acknowledged. But for those acknowledge 
    /// unrequired message, they are not buffered in any layer. Such message will be sent and forget. 
    /// Hence, if the connection is broken at the time of Send() method is invoked, this acknowledge 
    /// unrequired message will be lost. Hence, all critical messages should be defined as the 
    /// acknowledge required message in the interface protocol design.
    /// </para>
    /// <para>
    /// As per the Gateway Service application design, this gateway application is acted as the TCP client
    /// of both connections to BHS internal Engine Service applications and to external devices. Hence, 
    /// the both Engine Service application and external devices are TCP server and always in the 
    /// listening mode to wait for the TCP connection request from Gateway service application.
    /// </para>
    /// <para>
    /// Gateway-Internal chain has the higher priority than Gateway-External chain. The connection 
    /// (Application layer and TCP Socket layer) between gateway service to external devices shall be 
    /// opened only after connection (both application and socket layers) between gateway service to 
    /// Engine Service application is opened. If gateway to Engine connection is interrupted, the 
    /// all connections of gateway to external devices have to be closed immediately. This design is 
    /// for gateway service application, which is running on the seperate SAC-COM server (B), can has the 
    /// chance to open the connection to external devices, when the Engine Service on the SAC-COM
    /// server (A) is dead but the gateway service on server (A) is still working. Otherwise, the 
    /// gateway service running on server (A) will continue hold the connection to external devices, 
    /// even its associated engine service has been dead.
    /// </para>
    /// <para>
    /// Upon channel conenction is opened, closed, or message is received, MessageHandler class will
    /// raise 
    /// OnConnected(object sender, MessageEventArgs e), 
    /// OnDisconnected(object sender, MessageEventArgs e),
    /// and OnReceived(object sender, MessageEventArgs e) 
    /// events to wrapper class. In the event MessageEventArgs type parameter e, the ChainName, 
    /// ChannelName, OpenedChannelCount, and Message will be forwarded to wrapper class.
    /// ChainName   - The name of communication Chain in which the event is fired. One Chain could have
    ///               multiple channel connections.
    /// ChannelName - The name of communication Channel where the connection is opened/closed, or 
    ///               message is received from. One Chain could have multiple channel connections.
    /// OpenedChannelCount - The number of current opened channel connections.
    /// Message     - The received message.
    /// </para>
    /// </remarks>
    public class MessageHandler
    {

        #region Class fields and Properties Declaration

        private const string SOURCE_GW2INTERNAL = "GW2INTERNAL";
        private const string SOURCE_GW2EXTERNAL = "GW2EXTERNAL";
        private const string CSNF_COMM_STATUS_OPENED = "01";
        private const string CSNF_COMM_STATUS_CLOSED = "00";
        private const int THREAD_INTERVAL = 10; // 10 millisecond

        // The name of current class
        private static readonly string _className =
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
        // Create a logger for use in this class
        private static readonly log4net.ILog _logger =
                    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Queue _incomingQueue;
        private Queue _syncdIncomingQueue;
        private Thread _handlingThread;
        private bool _isInternalChainOpened;
        private bool _isExternalChainOpened;

        private long _threadCounter;
        private long _noOfMsg2PLC;
        private long _noOfMsg2SortEngine;
        private PALS.Diagnostics.ClassStatus _perfMonitor;                



        /// <summary>
        /// Message forwarder of GW2Internal channel for forwarding incoming message to centrialized message handler.
        /// </summary>
        private GW2InternalSessionForwarder _forwarderGW2INTERNAL;
        /// <summary>
        /// Message forwarder of GW2External channel for forwarding incoming message to centrialized message handler.
        /// </summary>
        private GW2ExternalSessionForwarder _forwarderGW2EXTERNAL;

        // Upon ConnectionOpened() method is invoked by bottom chain class,
        // the ChannelName will be stored into this ArrayList.
        // Once the bottom protocol layer connection is closed, its ChannelName
        // will be removed from this ArrayList accordingly.
        private ArrayList _channelListGW2INTERNAL, _syncdChannelListGW2INTERNAL;
        // Creates a synchronized wrapper around the ArrayList.
        private ArrayList _channelListGW2EXTERNAL, _syncdChannelListGW2EXTERNAL;

        /// <summary>
        /// Event will be raised when message is received.
        /// </summary>
        public event EventHandler<MessageEventArgs> OnReceived;
        /// <summary>
        /// Event will be raised when specific channel connection of Gateway-External device chain is opened.
        /// </summary>
        public event EventHandler<MessageEventArgs> OnConnected;
        /// <summary>
        /// Event will be raised when specific channel connection of Gateway-External device chain is closed.
        /// </summary>
        public event EventHandler<MessageEventArgs> OnDisconnected;

        /// <summary>
        /// null
        /// </summary>
        public string ObjectID { get; set; }
        /// <summary>
        /// Property, object of MessageHandlerParameters class.
        /// </summary>
        public Messages.Handlers.MessageHandlerParameters ClassParameters { get; set; }
        /// <summary>
        /// Property, used to the reference of TCPClient class object in a communication channel.
        /// </summary>
        public PALS.Net.Transports.TCP.TCPClientParameters TCPClientParames { get; set;  }

        /// <summary>
        /// null
        /// </summary>
        public PALS.Diagnostics.ClassStatus PerfMonitor
        {
            get
            {
                try
                {
                    _perfMonitor.ObjectID = ObjectID;
                    PerfCounterRefresh();
                    return _perfMonitor;
                }
                catch (Exception ex)
                {
                    string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

                    if (_logger.IsErrorEnabled)
                        _logger.Error("Exception occurred! <" + thisMethod + ">", ex);

                    return null;
                }
            }
        }

        /// <summary>
        /// The Hashtable that contains the ClassStatus object of current class and all of its instance of sub classes.
        /// </summary> 
        public ArrayList PerfMonitorList { get; set; }

        #endregion
        
        #region Class Constructor, Dispose, & Destructor

        /// <summary>
        /// Class constructer.
        /// </summary>
        public MessageHandler(PALS.Common.IParameters param,
                    GW2InternalSessionForwarder forwarderGW2INTERNAL, 
                    GW2ExternalSessionForwarder forwarderGW2EXTERNAL)
        {
            if (param == null)
                throw new Exception("Constractor parameter can not be null! Creating class " + _className +
                    " object failed! <BHS.Gateway.Messages.Handlers.MessageHandler.Constructor()>");

            if ((forwarderGW2INTERNAL == null) || (forwarderGW2EXTERNAL == null))
                throw new Exception("Constractor parameter can not be null! Creating class " + _className +
                    " object failed! <BHS.Gateway.Messages.Handlers.MessageHandler.Constructor()>");

            ClassParameters = (Messages.Handlers.MessageHandlerParameters)param;
            _forwarderGW2INTERNAL = forwarderGW2INTERNAL;
            _forwarderGW2EXTERNAL = forwarderGW2EXTERNAL;

            // Call Init() method to perform class initialization tasks.
            Init();
        }

        /// <summary>
        /// Class destructer.
        /// </summary>
        ~MessageHandler()
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

            if (_perfMonitor != null)
            {
                _perfMonitor.Dispose();
                _perfMonitor = null;
            }

            // Terminate message handling thread.
            if (_handlingThread != null)
            {
                _handlingThread.Abort();
                _handlingThread.Join();
                _handlingThread = null;
            }
            
            // Release incoming message buffer
            if (_syncdIncomingQueue != null)
            {
                _syncdIncomingQueue.Clear();
                _syncdIncomingQueue = null;
            }

            // Add codes here to release resource
            if (_syncdChannelListGW2INTERNAL != null)
            {
                _syncdChannelListGW2INTERNAL.Clear();
                _syncdChannelListGW2INTERNAL = null;
            }

            if (_syncdChannelListGW2EXTERNAL != null)
            {
                _syncdChannelListGW2EXTERNAL.Clear();
                _syncdChannelListGW2EXTERNAL = null;
            }

            if (disposing)
            {
                if (_logger.IsInfoEnabled)
                    _logger.Info("Class:[" + _className + "] object has been destroyed. <" + thisMethod + ">");
            }
        }

        #endregion
        
        #region Class Method Declaration.

        private void Init()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            if (_logger.IsInfoEnabled)
                _logger.Info("Class:[" + _className + "] object is initializing... <" + thisMethod + ">");

            _isInternalChainOpened = false;
            _isExternalChainOpened = false;

            // Subscribe event handler to classes GW2INTERNALSessionForward & GW2EXTERNALSessionForward
            _forwarderGW2INTERNAL.OnReceived +=new EventHandler<MessageEventArgs>(ForwarderGW2INTERNAL_OnReceived);
            _forwarderGW2INTERNAL.OnConnected += new EventHandler<MessageEventArgs>(ForwarderGW2INTERNAL_OnConnected);
            _forwarderGW2INTERNAL.OnDisconnected += new EventHandler<MessageEventArgs>(ForwarderGW2INTERNAL_OnDisconnected);
            _forwarderGW2EXTERNAL.OnReceived += new EventHandler<MessageEventArgs>(ForwarderGW2EXTERNAL_OnReceived);
            _forwarderGW2EXTERNAL.OnConnected += new EventHandler<MessageEventArgs>(ForwarderGW2EXTERNAL_OnConnected);
            _forwarderGW2EXTERNAL.OnDisconnected += new EventHandler<MessageEventArgs>(ForwarderGW2EXTERNAL_OnDisconnected);

            // Create incoming message buffer
            _incomingQueue = new Queue();
            _syncdIncomingQueue = Queue.Synchronized(_incomingQueue);
            _syncdIncomingQueue.Clear();

            // Create ArrayList object for store opened channel connection name list
            _channelListGW2INTERNAL = new ArrayList();
            _syncdChannelListGW2INTERNAL = ArrayList.Synchronized(_channelListGW2INTERNAL);
            _syncdChannelListGW2INTERNAL.Clear();

            _channelListGW2EXTERNAL = new ArrayList();
            _syncdChannelListGW2EXTERNAL = ArrayList.Synchronized(_channelListGW2EXTERNAL);
            _syncdChannelListGW2EXTERNAL.Clear();

            // Create message handling thread
            _handlingThread = new System.Threading.Thread(new ThreadStart(MessageHandlingThread));
            _handlingThread.Name = _className + ".MessageHandlingThread";

            _threadCounter = 0;
            _noOfMsg2PLC = 0;
            _noOfMsg2SortEngine = 0;
            _perfMonitor = new PALS.Diagnostics.ClassStatus();

            // Start message handling thread;
            _handlingThread.Start();
            Thread.Sleep(0);
            
            if (_logger.IsInfoEnabled)
                _logger.Info("Class:[" + _className + "] object has been initialized. <" + thisMethod + ">");
        }

        private void ForwarderGW2INTERNAL_OnReceived(object sender, MessageEventArgs e)
        {
            PALS.Telegrams.Common.MessageAndSource msgSource = new PALS.Telegrams.Common.MessageAndSource();

            msgSource.Source = SOURCE_GW2INTERNAL;
            msgSource.Message = e.Message;

            lock (_incomingQueue.SyncRoot)
            {
                _incomingQueue.Enqueue(msgSource);
            }
            
            // Copy to a temporary variable to be thread-safe.
            EventHandler<MessageEventArgs> temp = OnReceived;
            // Event could be null if there are no subscribers, so check it before raise event
            if (temp != null)
            {
                e.ChainName = SOURCE_GW2INTERNAL;
                // Raise OnReceived event upon message is received.
                temp(this, e);
            }
        }

        private void ForwarderGW2EXTERNAL_OnReceived(object sender, MessageEventArgs e)
        {
            PALS.Telegrams.Common.MessageAndSource msgSource = new PALS.Telegrams.Common.MessageAndSource();

            msgSource.Source = SOURCE_GW2EXTERNAL;
            msgSource.Message = e.Message;

            lock (_incomingQueue.SyncRoot)
            {
                _incomingQueue.Enqueue(msgSource);
            }

            // Copy to a temporary variable to be thread-safe.
            EventHandler<MessageEventArgs> temp = OnReceived;
            // Event could be null if there are no subscribers, so check it before raise event
            if (temp != null)
            {
                e.ChainName = SOURCE_GW2EXTERNAL;
                // Raise OnReceived event upon message is received.
                temp(this, e);
            }
        }

        /// <summary>
        /// Gateway-Internal chain has the higher priority than Gateway-External chain. The connection 
        /// (Application layer and TCP Socket layer) between gateway service to external devices shall be 
        /// opened only after connection (both application and socket layers) between gateway service to 
        /// Engine Service application is opened. If gateway to Engine connection is interrupted, the 
        /// all connections of gateway to external devices have to be closed immediately. This design is 
        /// for gateway service application that is running on the seperate SAC-COM server (B) to has the 
        /// chance to open the connection to external devices, when the Engine Service on the SAC-COM
        /// server (A) is dead but the gateway service on server (A) is still working. Otherwise, the 
        /// gateway service running on server (A) will continue hold the connection to external devices, 
        /// even its associated engine service has been dead.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ForwarderGW2INTERNAL_OnConnected(object sender, MessageEventArgs e)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            lock (_syncdChannelListGW2INTERNAL.SyncRoot)
            {
                if (_syncdChannelListGW2INTERNAL.Contains(e.ChannelName) == false)
                    _syncdChannelListGW2INTERNAL.Add(e.ChannelName);
            }

            if (_logger.IsInfoEnabled)
                _logger.Info("Gateway-Internal Engine application connection <Channel:" +
                        e.ChannelName + "> has been opened, " +
                        "it is time to open Gateway-External device connection now... <" +
                        thisMethod + ">");

            // Due to the chain class support open multiple channel connections via single chain, hence, 
            // the number of opened channels will be checked before start GW2External chain connection,
            // so that the repeating of start GW2External chain connection can be prevented.
            if (_syncdChannelListGW2INTERNAL.Count == 1)
            {
                // Open TCPClient connection to external device by calling Connect() method of chain classes
                this._forwarderGW2EXTERNAL.Connect(string.Empty);

                // Set GW2EXTERNAL chain TCPClient class oject IsAutoReconnect property to true.
                //***************************************************************************************TCPClientParames.IsAutoReconnected = true;
            }

            // Copy to a temporary variable to be thread-safe.
            EventHandler<MessageEventArgs> temp = OnConnected;
            // Event could be null if there are no subscribers, so check it before raise event
            if (temp != null)
            {
                e.ChainName = SOURCE_GW2INTERNAL;
                // Raise OnConnected event upon channel connection is opened.
                temp(this, e);
            }

            _isInternalChainOpened = true;
            // if both internal and external chain connections are opened, then produce the 
            // "Gateway Ready Notification" (GRNF) message and send to Engine application (
            // via Routing service provided by MessageRouter) through Internal Chain.
            if (_isExternalChainOpened)
            {
                ProduceAndSendGRNF(ClassParameters.Sender, ClassParameters.Receiver);
            }
        }

        private void ForwarderGW2EXTERNAL_OnConnected(object sender, MessageEventArgs e)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            lock (_syncdChannelListGW2EXTERNAL.SyncRoot)
            {
                if (_syncdChannelListGW2EXTERNAL.Contains(e.ChannelName) == false)
                    _syncdChannelListGW2EXTERNAL.Add(e.ChannelName);
            }

            if (_logger.IsInfoEnabled)
                _logger.Info("Gateway-External device connection <Channel:" + e.ChannelName +
                    "> has been opened. <" + thisMethod + ">");

            // Copy to a temporary variable to be thread-safe.
            EventHandler<MessageEventArgs> temp = OnConnected;
            // Event could be null if there are no subscribers, so check it before raise event
            if (temp != null)
            {
                e.ChainName = SOURCE_GW2EXTERNAL;
                // Raise OnConnected event upon channel connection is opened.
                temp(this, e);
            }

            _isExternalChainOpened = true;
            // if both internal and external chain connections are opened, then produce the 
            // "Gateway Ready Notification" (GRNF) message and send to Engine application (
            // via Routing service provided by MessageRouter) through Internal Chain.
            if (_isInternalChainOpened)
            {
                ProduceAndSendGRNF(ClassParameters.Sender, ClassParameters.Receiver);
            }
        }

        private void ForwarderGW2INTERNAL_OnDisconnected(object sender, MessageEventArgs e)
        {
            // Ignore the OnDisconnected event if it is not in the opened channel list.
            if (_syncdChannelListGW2INTERNAL.Contains(e.ChannelName) == true)
            {
                _isInternalChainOpened = false;

                lock (_syncdChannelListGW2INTERNAL.SyncRoot)
                {
                    _syncdChannelListGW2INTERNAL.Remove(e.ChannelName);
                }

                string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

                if (_logger.IsErrorEnabled)
                    _logger.Error("Gateway-Internal Engine application connection <Channel:" +
                            e.ChannelName + "> is closed, " +
                            "hence Gateway-External device connection needs to be closed too... <" +
                            thisMethod + ">");

                // Set GW2EXTERNAL chain TCPClient class oject IsAutoReconnect property to false, so that
                // the re-connecting of GW2EXTERNAL chain will not be started automatically. It shall be 
                // started only when GW2INTERNAL chain connection is re-opened.
                //***************************************************************************************TCPClientParames.IsAutoReconnected = false;

                // Then stop all opened GW2EXTERNAL chain connections and wait for GW2INTERNAL connection 
                // to be re-opened. value string.Empty is passed to _forwarderGW2EXTERNAL.Disconnect() to 
                // close all channel connections of GW2EXTERNAL chain.
                _forwarderGW2EXTERNAL.Disconnect(string.Empty);
                
                // Copy to a temporary variable to be thread-safe.
                EventHandler<MessageEventArgs> temp = OnDisconnected;
                // Event could be null if there are no subscribers, so check it before raise event
                if (temp != null)
                {
                    e.ChainName = SOURCE_GW2INTERNAL;
                    // Raise OnDisconnected event upon channel connection is closed.
                    temp(this, e);
                }
            }
        }

        private void ForwarderGW2EXTERNAL_OnDisconnected(object sender, MessageEventArgs e)
        {
            // Ignore the OnDisconnected event if it is not in the opened channel list.
            if (_syncdChannelListGW2EXTERNAL.Contains(e.ChannelName) == true)
            {
                _isExternalChainOpened = false;

                lock (_syncdChannelListGW2EXTERNAL.SyncRoot)
                {
                    _syncdChannelListGW2EXTERNAL.Remove(e.ChannelName);
                }

                string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

                if (_logger.IsErrorEnabled)
                    _logger.Error("Gateway-External device connection <Channel:" +
                            e.ChannelName + "> has been closed. <" + thisMethod + ">");

                // Copy to a temporary variable to be thread-safe.
                EventHandler<MessageEventArgs> temp = OnDisconnected;
                // Event could be null if there are no subscribers, so check it before raise event
                if (temp != null)
                {
                    e.ChainName = SOURCE_GW2EXTERNAL;
                    // Raise OnDisconnected event upon channel connection is closed.
                    temp(this, e);
                }
            }
        }

        /// <summary>
        /// Close the specified connection of Gateway-Internal communication chain.
        /// If value null is passed to this method, then all connections of this chain will be closed.
        /// <para>
        /// Disconnect command will be passed to most top class GW2INTERNALSessionForwarder object
        /// in the chain, and then passed down to every chain classes to close each layer connections.
        /// </para>
        /// </summary>
        /// <param name="channelName">name of channel</param>
        public void DisconnectToInternal(string channelName)
        {
            _forwarderGW2INTERNAL.Disconnect(channelName);
        }

        /// <summary>
        /// Close the specified connection of Gateway-External communication chain.
        /// If value null is passed to this method, then all connections of this chain will be closed.
        /// <para>
        /// Disconnect command will be passed to most top class GW2ExternalSessionForwarder object
        /// in the chain, and then passed down to every chain classes to close each layer connections.
        /// </para>
        /// </summary>
        /// <param name="channelName">name of channel</param>
        public void DisconnectToExternal(string channelName)
        {
            _forwarderGW2EXTERNAL.Disconnect(channelName);
        }

        /// <summary>
        /// Sending message to BHS Internal Engine Service application via Gateway-Internal chain classes.
        /// <para>
        /// The message will be sent to all current opened connections of Gateway-Internal chain.
        /// </para>
        /// </summary>
        /// <param name="data"></param>
        public void SentToInternal(byte[] data)
        {
            if ((data == null) || (data.Length == 0))
                return;
            else
            {
                Telegram message = new Telegram(ref data);
                _noOfMsg2SortEngine = Functions.CounterIncrease(_noOfMsg2SortEngine);
                _forwarderGW2INTERNAL.Send(message);
            }
        }

        /// <summary>
        /// Sending message to BHS Internal Engine Service application via Gateway-Internal chain classes.
        /// <para>
        /// The message will be sent to all current opened connections of Gateway-Internal chain.
        /// </para>
        /// </summary>
        /// <param name="message"></param>
        public void SentToInternal(Telegram message)
        {
            if (message == null)
                return;
            else
            {
                _noOfMsg2SortEngine = Functions.CounterIncrease(_noOfMsg2SortEngine);
                _forwarderGW2INTERNAL.Send(message);
            }
        }

        /// <summary>
        /// Sending message to BHS External devices via Gateway-External chain classes.
        /// </summary>
        /// <para>
        /// The message will be sent to all current opened connections of Gateway-External chain.
        /// </para>
        /// <param name="data"></param>
        public void SentToExternal(byte[] data)
        {
            if ((data == null) || (data.Length == 0))
                return;
            else
            {
                Telegram message = new Telegram(ref data);
                _noOfMsg2PLC = Functions.CounterIncrease(_noOfMsg2PLC);
                _forwarderGW2EXTERNAL.Send(message);
            }
        }

        /// <summary>
        /// Sending message to BHS Internal Engine Service application via Gateway-Internal chain classes.
        /// <para>
        /// The message will be sent to all current opened connections of Gateway-Internal chain.
        /// </para>
        /// </summary>
        /// <param name="message"></param>
        public void SentToExternal(Telegram message)
        {
            if (message == null)
                return;
            else
            {
                _noOfMsg2PLC = Functions.CounterIncrease(_noOfMsg2PLC);
                _forwarderGW2EXTERNAL.Send(message);
            }
        }

        /// <summary>
        /// Message handling thread.
        /// This thread will be permanently running in background after application is started.
        /// </summary>
        private void MessageHandlingThread()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            if (_logger.IsInfoEnabled)
                _logger.Info("Message handling thread has been started. <" + thisMethod + ">");

            try
            {
                int count;
                PALS.Telegrams.Common.MessageAndSource msgSource;

                while (true)
                {
                    count = 0;
                    count = _incomingQueue.Count;

                    for (int i = 0; i < count; i++)
                    {
                        msgSource = null;
                        lock (_incomingQueue.SyncRoot)
                        {
                            msgSource = (PALS.Telegrams.Common.MessageAndSource)_incomingQueue.Dequeue();
                        }

                        // Incoming message handling.
                        IncomingMessageHandling(msgSource);
                    }
                    _threadCounter = Functions.CounterIncrease(_threadCounter);
                    Thread.Sleep(THREAD_INTERVAL);
                }
            }
            catch (ThreadAbortException ex)
            {
                Thread.ResetAbort();

                if (_logger.IsInfoEnabled)
                    _logger.Info("Message handling thread has been stopped. <" + thisMethod + ">", ex);
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Message handling thread failed. <" + thisMethod + ">", ex);

            }
        }

        /// <summary>
        /// According to interface protocol design (ItemTracking protocol and TCPServer2Client 
        /// protocol), there are 3 types of incoming messages:
        /// <para>ItemTracking message - Only come from Gateway-External communication chain. 
        /// E.g. come from PLC;</para> 
        /// <para>BagEvent (BEV) message - Only come from Gateway-Internal communication chain;
        /// E.g. come from SAC SortEngine Service;</para> 
        /// <para>Running Status Request (SRQ) and Reply (SRP) message - Only come from 
        /// Gateway-Internal communication chain. E.g. come from BHS Console Console Application;
        /// </para>
        /// </summary>
        /// <param name="msgSource"></param>
        private void IncomingMessageHandling(PALS.Telegrams.Common.MessageAndSource msgSource)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            string source;
            PALS.Telegrams.Telegram message;

            try
            {
                if (msgSource == null)
                {
                    return;
                }
                else
                {
                    source = msgSource.Source;
                    message = msgSource.Message;
                }

                if (message.Format == null)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("[Channel:" + message.ChannelName +
                                "] Telegram format is not defined for this incoming message! Message is discarded! [Msg(APP):" +
                                message.ToString(PALS.Utilities.HexToStrMode.ToAscPaddedHexString) + "]. <"
                                + thisMethod + ">");
                }
                else
                {
                    byte[] originType = message.GetFieldActualValue("Type");
                    string type = PALS.Utilities.Functions.ConvertByteArrayToString(
                            originType, -1, PALS.Utilities.HexToStrMode.ToAscString);

                    if ((source == SOURCE_GW2INTERNAL) & (type == ClassParameters.MessageType_SRQ))
                    {
                        // If SRQ message was received from BHSConsole via GW2INTERNAL chain,
                        // then SRP message will be created and sent to BHSConsole
                        // RunningStatusReply(message)
                    }
                    else if ((source == SOURCE_GW2INTERNAL) & (type == ClassParameters.MessageType_CSNF))
                    {
                        // Connection Status Notification (CSNF) message will be sent from MessageRouter to
                        // Gateway upon its associated affecting node is connected to MessageRouter.
                        IncomingCSNFMessageHandling(message.ChannelName, message);
                    }
                    else if ((source == SOURCE_GW2INTERNAL) & (type == ClassParameters.MessageType_INTM))
                    {
                        // All Intermediate messages that come from GW2INTERNAL will be decoded into 
                        // ItemTracking message and then forwarded to external device via GW2EXTERNAL chain.
                        IncomingIntermediateMessageHandling(message);
                    }
                    else if (source == SOURCE_GW2EXTERNAL)
                    {
                        // All ItemTracking messages that come from external devices via GW2EXTERNAL chain 
                        // will be encoded into INTM message, and then forwarded to SortEngine via GW2INTERNAL chain.
                        IncomingItemTrackingMessageHandling(type, originType, message);
                    }
                    else
                    {
                        // Undesired message will be discarded.
                        if (_logger.IsErrorEnabled)
                            _logger.Error("[Channel:" + message.ChannelName +
                                    "] Undesired message is received! it will be discarded... [Msg(APP):" +
                                    message.ToString(PALS.Utilities.HexToStrMode.ToAscPaddedHexString) + "]. <"
                                    + thisMethod + ">");
                    }
                }
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Incoming message handling is failed. <" + thisMethod + ">", ex);

            }
        }

        /// <summary>
        /// Perform Initializing or Finalizing tasks upon associated remote interface party is 
        /// conencted or disconnected to MessageRouter, by means of receive CSNF message.
        /// 
        /// If both internal and external chain connection of Gateway have opened, the Gateway
        /// will send "Gateway Ready Notification" (GRNF) message to remote.
        /// </summary>
        /// <param name="channelName">ChannelName.</param>
        /// <param name="message">INTM message.</param>
        private void IncomingCSNFMessageHandling(string channelName, PALS.Telegrams.Telegram message)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            string gwAppCode, status;

            //<telegram alias="CSNF" name="Connection_Status_Notification_Message" sequence="True" acknowledge="False">
            //  <field name="Type" offset="0" length="4" default="48,49,48,56"/>
            //  <field name="Length" offset="4" length="4" default="48,48,50,50"/>
            //  <field name="Sequence" offset="8" length="4" default="?"/>
            //  <field name="AppCode" offset="12" length="8" default="?"/>
            //  <field name="Status" offset="20" length="2" default="?"/>
            //</telegram>
            gwAppCode = (Functions.ConvertByteArrayToString(message.GetFieldActualValue("AppCode"),
                        -1, HexToStrMode.ToAscString)).Trim();
            status = (Functions.ConvertByteArrayToString(message.GetFieldActualValue("Status"),
                        -1, HexToStrMode.ToAscString)).Trim();


            switch (status)
            {
                case CSNF_COMM_STATUS_OPENED:
                    if (_logger.IsInfoEnabled)
                        _logger.Info("[Channel:" + channelName + "] <- [Msg(CSNF):" +
                                message.ToString(PALS.Utilities.HexToStrMode.ToAscPaddedHexString) +
                                "]. Gateway associated application (" + gwAppCode +
                                ") has connected to MessageRouter" + ". <" + thisMethod + ">");

                    // if both internal and external chain connections are opened, then produce the 
                    // "Gateway Ready Notification" (GRNF) message and send to Engine application (
                    // via Routing service provided by MessageRouter) through Internal Chain.
                    //if (_isExternalChainOpened & _isInternalChainOpened)
                    //{
                    //   ProduceAndSendGRNF(ClassParameters.Sender, ClassParameters.Receiver);
                    //}

                    break;
                case CSNF_COMM_STATUS_CLOSED:
                    if (_logger.IsErrorEnabled)
                        _logger.Error("[Channel:" + channelName + "] <- [Msg(CSNF):" +
                                message.ToString(PALS.Utilities.HexToStrMode.ToAscPaddedHexString) +
                                "]. Gateway associated application (" + gwAppCode +
                                ") has disconnected to MessageRouter" + "! <" + thisMethod + ">");

                    break;
            }
        }

        
        /// <summary>
        /// All Intermediate messages that come from GW2INTERNAL chain (e.g. SortEngine service) will 
        /// be decoded into ItemTracking message and then be forwarded to remote devices (e.g. PLC)
        /// via GW2EXTERNAL chain.
        /// </summary>
        /// <param name="message">Incoming BagEvent message.</param>
        private void IncomingIntermediateMessageHandling(PALS.Telegrams.Telegram message)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            byte[] originMsg;
            string sender, receiver, originMsgType;
            int temp;

            //  <telegram alias="INTM" name="Intermediate_Message" sequence="True" acknowledge="True">
            //    <field name="Type" offset="0" length="4" default="48,49,48,51"/>
            //    <field name="Length" offset="4" length="4" default="?"/>
            //    <field name="Sequence" offset="8" length="4" default="?"/>
            //    <field name="Sender" offset="12" length="8" default="?"/>
            //    <field name="Receiver" offset="20" length="8" default="?"/>
            //    <field name="OriginMsgType" offset="28" length="4" default="?"/>
            //    <field name="OriginMsg" offset="32" length="?" default="?"/>
            //  </telegram>
            sender = (Functions.ConvertByteArrayToString(message.GetFieldActualValue("Sender"),
                        -1, HexToStrMode.ToAscString)).Trim();
            receiver = (Functions.ConvertByteArrayToString(message.GetFieldActualValue("Receiver"),
                        -1, HexToStrMode.ToAscString)).Trim();
            originMsgType = (Functions.ConvertByteArrayToString(message.GetFieldActualValue("OriginMsgType"),
                        -1, HexToStrMode.ToAscString)).Trim();

            temp = message.Format.Field("OriginMsg").Length;
            message.Format.Field("OriginMsg").Length = message.RawData.Length -
                        message.Format.Field("OriginMsg").Offset;
            originMsg = message.GetFieldActualValue("OriginMsg");
            message.Format.Field("OriginMsg").Length = temp;

            if (_logger.IsDebugEnabled)
            {
                _logger.Debug("<- [Msg(INTM):" + message.ToString(HexToStrMode.ToAscPaddedHexString) +
                        "]. <" + thisMethod + ">");
                _logger.Debug("-> Forward original message to external. [Msg(" + originMsgType + "):" +
                        Functions.ConvertByteArrayToString(originMsg, -1, HexToStrMode.ToAscPaddedHexString) +
                        ">. <" + thisMethod + ">");
            }

            // ############################################################
            // To-Do-List (for future)
            // Verify the receiver name in the incoming INTM message. If it is not
            // the same as the local sender name of current application, then the
            // INTM Original message will not be sent to external.
            // ...
            // ############################################################
            
            // Forward decoded ItemTracking message to external device via GW2EXTERNAL chain.
            SentToExternal(originMsg);
        }

        /// <summary>
        /// All incoming ItemTracking messages that received from GW2EXTERNAL chain will 
        /// be encoded into INTM message and then forwarded to SortEngine via GW2INTERNAL chain.
        /// </summary>
        /// <param name="type">Incoming ItemTracking message type, string.</param>
        /// <param name="originType">Incoming ItemTracking message type, byte array.</param>
        /// <param name="message">Incoming ItemTracking message.</param>
        private void IncomingItemTrackingMessageHandling(string type, byte[] originType, PALS.Telegrams.Telegram message)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            Telegram msgINTM;

            msgINTM = ConstructINTMMessage(originType, message, ClassParameters.MessageFormat_INTM);

            if (msgINTM != null)
            {
                if (_logger.IsInfoEnabled)
                    _logger.Info("Sending INTM message to Engine service... <Sender:" +
                        ClassParameters.Sender + ", Receiver: " + ClassParameters.Receiver +
                        ", OriginMsgType: " + type + ", OriginMsg:" +
                        msgINTM.ToString(HexToStrMode.ToAscPaddedHexString) +
                        ">. <" + thisMethod + ">");

                // Forward encapsulated INTM message to BHS internal Engine service via GW2INTERNAL chain.
                SentToInternal(msgINTM);
            }
            else
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("INTM message encapsulation is failed. Orginal message will be discarded! [Msg(APP):" +
                            message.ToString(HexToStrMode.ToAscPaddedHexString) +
                            ">. <" + thisMethod + ">");
            }
        }

        /// <summary>
        /// Constructing BagEvent message by encapsulating ItemTracking message received from
        /// external device (e.g. PLC).
        /// </summary>
        /// <param name="originType"></param>
        /// <param name="message"></param>
        /// <param name="formatINTM"></param>
        /// <returns></returns>
        private Telegram ConstructINTMMessage(byte[] originType, Telegram message, TelegramFormat formatINTM)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            Telegram msgINTM = null;

            if (message == null)
                return null;

            if (formatINTM == null)
                return null;

            try
            {
                //  <telegram alias="INTM" name="Intermediate_Message" sequence="True" acknowledge="True">
                //    <field name="Type" offset="0" length="4" default="48,49,48,51"/>
                //    <field name="Length" offset="4" length="4" default="?"/>
                //    <field name="Sequence" offset="8" length="4" default="?"/>
                //    <field name="Sender" offset="12" length="8" default="?"/>
                //    <field name="Receiver" offset="20" length="8" default="?"/>
                //    <field name="OriginMsgType" offset="28" length="4" default="?"/>
                //    <field name="OriginMsg" offset="32" length="?" default="?"/>
                //  </telegram>

                byte[] data = null;
                msgINTM = new Telegram(ref data);
                msgINTM.Format = formatINTM;
                msgINTM.ChannelName = message.ChannelName;

                int fieldLen = 0;
                int msgLen = 0;
                bool temp;
                byte[] type, seq, sndr, rcvr, origin, len;

                fieldLen = msgINTM.Format.Field("Type").Length;
                msgLen = msgLen + fieldLen;
                type = msgINTM.GetFieldDefaultValue("Type");
                temp = msgINTM.SetFieldActualValue("Type", ref type, PALS.Telegrams.Common.PaddingRule.Right);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("INTM message \"Type\" field value assignment is failed! <" + 
                                thisMethod + ">");                
                    return null;
                }

                fieldLen = msgINTM.Format.Field("Sequence").Length;
                msgLen = msgLen + fieldLen;
                // The new sequence number will be calculated and assigned to the
                // "Sequence" field of outgoing application messages, if this message associated
                // TelegramFormat object is indicated that it is the new sequence number
                // required message. The sequence number is globally contained by the static class:
                // PALS.Utilities.SequenceNo. You can get the application global wide unique
                // new sequence number by calling SequenceNo.NewSequenceNo Shared property directly, 
                // without instantial the SequenceNo.
                seq = new byte[fieldLen];
                if (formatINTM.NeedNewSequence == true)
                {
                    long newSeq = SequenceNo.NewSequenceNo1;
                    seq = Functions.ConvertStringToFixLengthByteArray(
                            newSeq.ToString() , fieldLen, '0', Functions.PaddingRule.Left);
                }
                temp = msgINTM.SetFieldActualValue("Sequence", ref seq, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("INTM message \"Sequence\" field value assignment is failed! <" + 
                                thisMethod + ">");                
                    return null;
                }

                fieldLen = msgINTM.Format.Field("Sender").Length;
                msgLen = msgLen + fieldLen;
                sndr = Functions.ConvertStringToFixLengthByteArray(ClassParameters.Sender,
                            msgINTM.Format.Field("Sender").Length, ' ', Functions.PaddingRule.Right);
                temp = msgINTM.SetFieldActualValue("Sender", ref sndr, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("INTM message \"sndr\" field value assignment is failed! <" + 
                                thisMethod + ">");                
                    return null;
                }

                fieldLen = msgINTM.Format.Field("Receiver").Length;
                msgLen = msgLen + fieldLen;
                rcvr = Functions.ConvertStringToFixLengthByteArray(ClassParameters.Receiver,
                            msgINTM.Format.Field("Receiver").Length, ' ', Functions.PaddingRule.Right);
                temp = msgINTM.SetFieldActualValue("Receiver", ref rcvr, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("INTM message \"Receiver\" field value assignment is failed! <" + 
                                thisMethod + ">");                
                    return null;
                }

                fieldLen = msgINTM.Format.Field("OriginMsgType").Length;
                msgLen = msgLen + fieldLen;
                temp = msgINTM.SetFieldActualValue("OriginMsgType", ref originType, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("INTM message \"OriginMsgType\" field value assignment is failed! <" + 
                                thisMethod + ">");                
                    return null;
                }

                fieldLen = message.RawData.Length;
                msgLen = msgLen + fieldLen;
                msgINTM.Format.Field("OriginMsg").Length = fieldLen;
                origin = message.RawData;
                temp = msgINTM.SetFieldActualValue("OriginMsg", ref origin, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("INTM message \"OriginMsg\" field value assignment is failed! <" + 
                                thisMethod + ">");                
                    return null;
                }

                fieldLen = msgINTM.Format.Field("Length").Length;
                msgLen = msgLen + fieldLen;
                len = new byte[fieldLen];
                len = Functions.ConvertStringToFixLengthByteArray(msgLen.ToString(),
                            fieldLen, '0', Functions.PaddingRule.Left);
                temp = msgINTM.SetFieldActualValue("Length", ref len, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("INTM message \"Length\" field value assignment is failed! <" + 
                                thisMethod + ">");                
                    return null;
                }

                return msgINTM;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Constructing BEV message is failed! <" + thisMethod + ">", ex);

                return null;
            }
        }

        /// <summary>
        /// Upon  successfully  connected  to  external  device  and  MessageRouter,  Gateway  
        /// application  will produce a GRNF message and send to its associated Engine application. 
        /// Engine application can only send messages, which need to be forwarded to external 
        /// device by Gateway, after receives the GRNF message from Gateway.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="receiver"></param>
        private void ProduceAndSendGRNF(string sender, string receiver)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            try
            {
                // Construct CSNF message.
                Telegram GRNF = ConstructGRNFMessage(sender, receiver);

                if (GRNF != null)
                {
                    // Assign receiver as the channel name of outgoing GRNF message.
                    GRNF.ChannelName = receiver;

                    // Outgoing GRNF message will be encapsulated into INTM message and then sent to 
                    // MessageRouter (or Engine for direct connection to Engine) via GW2INTERNAL chain.
                    IncomingItemTrackingMessageHandling(ClassParameters.MessageType_GRNF,
                                GRNF.GetFieldDefaultValue("Type"), GRNF);
                }
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("System Exception! <" + thisMethod + ">", ex);

                return;
            }
        }

        /// <summary>
        /// Construct "Gateway Ready Notification" (GRNF) message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="receiver"></param>
        /// <returns></returns>
        private Telegram ConstructGRNFMessage(string sender, string receiver)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            if (sender.Trim() == string.Empty)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("No Sender Code specified, constructing GRNF message is failed! <" + thisMethod + ">");
                return null;
            }

            if (receiver.Trim() == string.Empty)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("No Receiver Code specified, constructing GRNF message is failed! <" + thisMethod + ">");
                return null;
            }

            try
            {
                //<telegram alias="GRNF" name="Gateway_Ready_Notification_Message" sequence="True" acknowledge="False">
                //  <field name="Type" offset="0" length="4" default="48,49,48,57"/>
                //  <field name="Length" offset="4" length="4" default="48,48,50,48"/>
                //  <field name="Sequence" offset="8" length="4" default="?"/>
                //  <field name="AppCode" offset="12" length="8" default="?"/>
                //</telegram>

                int fieldLen = 0;
                bool temp;
                byte[] type, len, seq, code;

                Telegram msgGRNF = null;
                byte[] data = null;
                msgGRNF = new Telegram(ref data);
                msgGRNF.Format = ClassParameters.MessageFormat_GRNF;
                // Assign receiver as the channel name of outgoing GRNF message.
                msgGRNF.ChannelName = receiver; 

                type = msgGRNF.GetFieldDefaultValue("Type");
                temp = msgGRNF.SetFieldActualValue("Type", ref type, PALS.Telegrams.Common.PaddingRule.Right);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("GRNF message \"Type\" field value assignment is failed! <" + thisMethod + ">");
                    return null;
                }

                len = msgGRNF.GetFieldDefaultValue("Length");
                temp = msgGRNF.SetFieldActualValue("Length", ref len, PALS.Telegrams.Common.PaddingRule.Right);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("GRNF message \"Length\" field value assignment is failed! <" + thisMethod + ">");
                    return null;
                }

                // The new sequence number will be calculated and assigned to the
                // "Sequence" field of outgoing application messages, if this message associated
                // TelegramFormat object is indicated that it is the new sequence number
                // required message. The sequence number is globally contained by the static class:
                // PALS.Utilities.SequenceNo. You can get the application global wide unique
                // new sequence number by calling SequenceNo.NewSequenceNo Shared property directly, 
                // without instantial the SequenceNo.
                fieldLen = msgGRNF.Format.Field("Sequence").Length;
                seq = new byte[fieldLen];
                if (msgGRNF.Format.NeedNewSequence == true)
                {
                    long newSeq = SequenceNo.NewSequenceNo1;
                    seq = Functions.ConvertStringToFixLengthByteArray(
                            newSeq.ToString(), fieldLen, '0', Functions.PaddingRule.Left);
                }
                temp = msgGRNF.SetFieldActualValue("Sequence", ref seq, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("GRNF message \"Sequence\" field value assignment is failed! <" +
                                thisMethod + ">");
                    return null;
                }

                // Inform receiver (Engine) that sender (Gateway) application is ready.
                code = Functions.ConvertStringToFixLengthByteArray(sender,
                            ClassParameters.MessageFormat_GRNF.Field("AppCode").Length, ' ', Functions.PaddingRule.Right);
                temp = msgGRNF.SetFieldActualValue("AppCode", ref code, PALS.Telegrams.Common.PaddingRule.Right);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("GRNF message \"AppCode\" field value assignment is failed! <" +
                                thisMethod + ">");
                    return null;
                }

                return msgGRNF;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Constructing GRNF message is failed! <" + thisMethod + ">", ex);

                return null;
            }
        }

        private void PerfCounterRefresh()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            try
            {
                // Refresh current class object status counters.
                if ((_perfMonitor != null) & (ObjectID != string.Empty))
                {
                    _perfMonitor.OpenObjectNode();
                    _perfMonitor.AddObjectStatus("class", _className);
                    _perfMonitor.AddObjectStatus("threadCounter", _threadCounter.ToString());
                    _perfMonitor.AddObjectStatus("incomingQueue", _syncdIncomingQueue.Count.ToString());
                    _perfMonitor.AddObjectStatus("msgs2PLC", _noOfMsg2PLC.ToString());
                    _perfMonitor.AddObjectStatus("msgs2SortEngn", _noOfMsg2SortEngine.ToString());
                    _perfMonitor.CloseObjectNode();
                }

            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Exception occurred! <" + thisMethod + ">", ex);
            }
        }
        #endregion
    }
}
