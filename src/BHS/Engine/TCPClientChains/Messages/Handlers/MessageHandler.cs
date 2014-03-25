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
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using PALS.Telegrams;
using PALS.Utilities;
using PALS.Common;

namespace BHS.Engine.TCPClientChains.Messages.Handlers
{
    /// <summary>
    /// Application layer message handler of Engine Service application. The incoming application 
    /// message from TCPClient communication chains will be forwarded to this class by the  
    /// OnReceived() event fired by most top class, SessionForwarder, in the chains. 
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
    /// unrequired message will be lost. All critical messages should be defined as the acknowledge 
    /// required message in the interface protocol design.
    /// </para>
    /// <para>
    /// Usually, the Engine Service shall be set as the Depending Nodes of Gateway service. It represents
    /// that 1) the Gateway service can not open connection to MessageRouter if the connection between
    /// Engine Service and MessageRouter is not opened; 2) MessageRouter will send Connection Status
    /// Notification (CSNF) message to Engine service once the Gateway service open or close the connection 
    /// to MessageRouter. Upon receives CSNF message, Engine service can start to perform the required
    /// initialization or finalization tasks.
    /// </para>
    /// <para>
    /// Upon channel conenction is opened, closed, or message is received, MessageHandler class will
    /// raise following events to wrapper class: 
    /// OnConnected(object sender, MessageEventArgs e), 
    /// OnDisconnected(object sender, MessageEventArgs e),
    /// and OnReceived(object sender, MessageEventArgs e) 
    /// 
    /// In the event MessageEventArgs type parameter e, the ChainName, ChannelName, OpenedChannelCount, 
    /// and Message will be forwarded to wrapper class.
    /// ChainName           - The name of communication Chain in which the event is fired. One Chain could have
    ///                       multiple channel connections.
    /// ChannelName         - The name of communication Channel where the connection is opened/closed, or 
    ///                       message is received from. One Chain could have multiple channel connections.
    /// OpenedChannelCount  - The number of current opened channel connections.
    /// Message             - The received message.
    /// </para>
    /// <para>
    /// Whenever Sort Engine service receives "Gateway Ready Notification" (GRNF) message sent by Gateway 
    /// application upon Gateway to its both internal and external conenction are opened, Engine will
    /// perform following 3 tasks:
    /// 1) clear all CCTV alarms stored in the database table [CCTV_STATUS].
    /// 2) clear all Outgoing MDS Alarm messages stored in the database table [CCTV_MDS_OUTGOING_ALARMS].
    /// 3) send all current MDS alarms stored in the database table [CCTV_MDS_ACTIVATED_ALARMS] to CCTV server.
    /// 4) Add ready Gateway application code into List object (_readyGageways) to inform HandlingThread 
    ///    start the monitoring of table [CCTV_MDS_OUTGOING_ALARMS] and sending its records to CCTV Gateway 
    ///    for forwarding to CCTV server.
    /// 
    /// Upon receives GRNF message, Gateway application code included in themessage will be added into 
    /// List object (_readyGageways). It will be removed from List upon receives CSNF message with 
    /// "Disconnected" status or Engine lost the connection to MessageRouter. No MDS alarm is allowed 
    /// to be sent to CCTV server if the number of items in the List object is less than 1 (no Gateway is 
    /// ready), no metter CCTV Engine is connected to MessageRouter or not. 
    /// 
    /// Whenever CCTV server is connected to CCTV Gateway Service, CCTV server shall clear all current MDS 
    /// alarms maintained by CCTV server itself.
    /// </para>
    /// 
    /// </remarks>
    public class MessageHandler
    {

        #region Class fields and Properties Declaration

        private const int THREAD_INTERVAL = 10; // 10 millisecond
        private const string CSNF_COMM_STATUS_OPENED = "01";
        private const string CSNF_COMM_STATUS_CLOSED = "00";
        //private const int MIN_SRQ_INTERVAL = 1000; // 1000 millisecond
        //private const string RUNNING_STATUS_ALL = "ALL";
        //private const int RR_BUFFER_PURGING_INTERVAL = 60000 '60000 millisecond

        // The name of current class 
        private static readonly string _className =
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
        // Create a logger for use in this class
        private static readonly log4net.ILog _logger =
                    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Upon "Gateway Ready Notification" (GRNF) message is received, Engine will classified that this
        // particular Gateway is ready for receiving message from Engine. Since one Engine could serve
        // multiple Gateways, all Gateway application codes reported by GRNF message will be stored in
        // the List upon GRNF is received. They will be removed from the list upon "Connection Status
        // Nortification" (CSNF) message with "Disconnected" status indication is received, or upon
        // Engine lost the connection to MessageRouter.
        private List<string> _readyGageways;
        private Object _thisLock = new Object();

        private Queue _incomingQueue;
        private Queue _syncdIncomingQueue;
        private Thread _handlingThread;

        /// <summary>
        /// Reference of the most top SessionForwarder class in the TCPClient chain.
        /// </summary>
        private SessionForwarder _forwarder;

        private long _threadCounter;
        private long _noOfMsgFromClnChannel;
        private long _noOfMsgToClnChannel;
        private long _noOfDiscardedMessage;
        private long _noOfGID;
        private long _noOfICR;
        private long _noOfISC;      
        private long _noOfISE;      
        private long _noOfIPR;
        private long _noOfILT;
        private long _noOfITI;
        private long _noOfMER;      
        private long _noOfBMAM;      
        private long _noOfP1500;
        private long _noOfGRNF;
        private long _noOfIRD;
        private PALS.Diagnostics.ClassStatus _perfMonitor;

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
        /// ID of class object
        /// </summary>
        public string ObjectID { get; set; }
        /// <summary>
        /// Property, object of MessageHandlerParameters class.
        /// </summary>
        public Messages.Handlers.MessageHandlerParameters ClassParameters { get; set; }

        /// <summary>
        /// Business logic handler of CSNF message
        /// </summary>
        public Messages.Handlers.GRNF GRNF { get; set; }

        /// <summary>
        /// Business logic handler of CANL message
        /// </summary>
        public Messages.Handlers.GID GID { get; set; }

        /// <summary>
        /// Business logic handler of ICR message
        /// </summary>
        public Messages.Handlers.ICR ICR { get; set; }

        /// <summary>
        /// Business logic handler of ISC message
        /// </summary>
        public Messages.Handlers.ISC ISC { get; set; }

        /// <summary>
        /// Business logic handler of ISE message
        /// </summary>
        public Messages.Handlers.ISE ISE { get; set; }

        /// <summary>
        /// Business logic handler of IPR message
        /// </summary>
        public Messages.Handlers.IPR IPR { get; set; }

        /// <summary>
        /// Business logic handler of ILT message
        /// </summary>
        public Messages.Handlers.ILT ILT { get; set; }

        /// <summary>
        /// Business logic handler of ITI message
        /// </summary>
        public Messages.Handlers.ITI ITI { get; set; }

        /// <summary>
        /// Business logic handler of MER message
        /// </summary>
        public Messages.Handlers.MER MER { get; set; }

        /// <summary>
        /// Business logic handler of AFAI message
        /// </summary>
        public Messages.Handlers.AFAI AFAI { get; set; }

        /// <summary>
        /// Business logic handler of BMAM message
        /// </summary>
        public Messages.Handlers.BMAM BMAM { get; set; }

        /// <summary>
        /// Business logic handler of CRAI message
        /// </summary>
        public Messages.Handlers.CRAI CRAI { get; set; }

        /// <summary>
        /// Business logic handler of FBTI message
        /// </summary>
        public Messages.Handlers.FBTI FBTI { get; set; }

        /// <summary>
        /// Business logic handler of FPTI message
        /// </summary>
        public Messages.Handlers.FPTI FPTI { get; set; }

        /// <summary>
        /// Business logic handler of P1500 message
        /// </summary>
        public Messages.Handlers.P1500 P1500 { get; set; }

        /// <summary>
        /// Business logic handler of IRD message
        /// </summary>
        public Messages.Handlers.IRD IRD { get; set; } 

        /// <summary>
        /// null
        /// </summary>
        public DataPersistor.Database.Persistor DBPersistor { get; set; }

        private DateTime _lastPollingTime;

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
        public MessageHandler(PALS.Common.IParameters param, SessionForwarder forwarder)
        {
            if (param == null)
                throw new Exception("Constractor parameter can not be null! Creating class " + _className +
                    " object failed! <BHS.Engine.TCPClientChains.Messages.Handlers.MessageHandler.Constructor()>");

            if (forwarder == null)
                throw new Exception("Constractor parameter can not be null! Creating class " + _className +
                    " object failed! <BHS.Engine.TCPClientChains.Messages.Handlers.MessageHandler.Constructor()>");

            ClassParameters = (Messages.Handlers.MessageHandlerParameters)param;
            _forwarder = forwarder;
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
            // Release managed & unmanaged resources...
            if (disposing)
            {
                if (_logger.IsInfoEnabled)
                    _logger.Info("Class:[" + _className + "] object is being destroyed... <" + _className + ".Dispose()>");
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
            if (_readyGageways != null)
            {
                _readyGageways.Clear();
                _readyGageways = null;
            }

            if (disposing)
            {
                if (_logger.IsInfoEnabled)
                    _logger.Info("Class:[" + _className + "] object has been destroyed. <" + _className + ".Dispose()>");
            }
        }

        #endregion
        
        #region Class Method Declaration.

        /// <summary>
        /// Perform MessageHandler class initialization tasks.
        /// <para>Before this method is invoked, those fields of individual message handler 
        /// need to be assigned with value class caller (Initializer class object).</para>
        /// </summary>
        public void Init()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            if (_logger.IsInfoEnabled)
                _logger.Info("Class:[" + _className + "] object is initializing... <" + thisMethod + ">");

            // Subscribe event handler to classes GW2INTERNALSessionForward & GW2EXTERNALSessionForward
            _forwarder.OnReceived += new EventHandler<MessageEventArgs>(Forwarder_OnReceived);
            _forwarder.OnConnected += new EventHandler<MessageEventArgs>(Forwarder_OnConnected);
            _forwarder.OnDisconnected += new EventHandler<MessageEventArgs>(Forwarder_OnDisconnected);

            //// Subscribe event handler to message handlers for them to send messages
            ISC.OnSendRequest += new EventHandler<MessageSendRequestEventArgs>(OnSendRequest);

            /// Subscribe event handler to message handlers for MER to send messages 
            MER.OnSendRequest += new EventHandler<MessageSendRequestEventArgs>(OnSendRequest);

            /// Subscribe event handler to message handler for ISE to send message
            ISE.OnSendRequest += new EventHandler<MessageSendRequestEventArgs>(OnSendRequest);

            // Create incoming message buffer
            _incomingQueue = new Queue();
            _syncdIncomingQueue = Queue.Synchronized(_incomingQueue);
            _syncdIncomingQueue.Clear();

            // Create message handling thread
            _handlingThread = new System.Threading.Thread(new ThreadStart(MessageHandlingThread));
            _handlingThread.Name = _className + ".MessageHandlingThread";

            _threadCounter = 0;
            _noOfMsgFromClnChannel = 0;
            _noOfMsgToClnChannel = 0;
            _noOfDiscardedMessage = 0;
            _noOfGID = 0;
            _noOfICR = 0;
            _noOfISC = 0;      
            _noOfISE = 0;      
            _noOfIPR = 0;
            _noOfILT = 0;
            _noOfITI = 0;
            _noOfMER = 0;
            _noOfBMAM = 0;
            _noOfP1500 = 0;
            _noOfGRNF = 0;
            _noOfIRD = 0;

            // Create ArrayList object for store opened channel connection name list
            _readyGageways = new List<string>();

            // Start message handling thread;
            _handlingThread.Start();
            Thread.Sleep(0);

            if (_logger.IsInfoEnabled)
                _logger.Info("Class:[" + _className + "] object has been initialized. <" + thisMethod + ">");
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
        private void Forwarder_OnConnected(object sender, MessageEventArgs e)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            if (_logger.IsInfoEnabled)
                _logger.Info("[Channel:" + e.ChannelName + 
                        "] Engine-MessageRouter connection has been successfully opened! <" + thisMethod + ">");

            // Copy to a temporary variable to be thread-safe.
            EventHandler<MessageEventArgs> temp = OnConnected;
            // Event could be null if there are no subscribers, so check it before raise event
            if (temp != null)
            {
                // Raise OnConnected event upon channel connection is opened.
                temp(this, e);
            }
        }

        private void Forwarder_OnDisconnected(object sender, MessageEventArgs e)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            if (_logger.IsErrorEnabled)
                _logger.Error("[Channel:" + e.ChannelName +
                        "] Engine-MessageRouter connection has been closed! <" + thisMethod + ">");

            lock (_thisLock)
            {
                if (_readyGageways != null)
                    _readyGageways.Clear();
            }

            // Copy to a temporary variable to be thread-safe.
            EventHandler<MessageEventArgs> temp = OnDisconnected;
            // Event could be null if there are no subscribers, so check it before raise event
            if (temp != null)
            {
                // Raise OnDisconnected event upon channel connection is closed.
                temp(this, e);
            }
        }

        private void Forwarder_OnReceived(object sender, MessageEventArgs e)
        {
            _noOfMsgFromClnChannel = Functions.CounterIncrease(_noOfMsgFromClnChannel);
            PALS.Telegrams.Common.MessageAndSource msgSource = new PALS.Telegrams.Common.MessageAndSource();

            msgSource.Source = e.ChannelName;
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
                // Raise OnReceived event upon message is received.
                temp(this, e);
            }
        }

        private void OnMessageSendRequest(string sender, string receiver, string channelName, Telegram message)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            byte[] originType;
            Telegram msgINTM;

            // Encapsulate outgoing message into INTM message before send out.
            if (message.Format == null)
                message.Format = ClassParameters.MessageFormat_Header;

            originType = message.GetFieldActualValue("Type");
            string type = Functions.ConvertByteArrayToString(originType, -1, HexToStrMode.ToAscPaddedHexString);
            msgINTM = ConstructINTMMessage(sender, receiver, originType, message, ClassParameters.MessageFormat_INTM);

            if (msgINTM != null)
            {
                // Assign channel name to INTM message.
                msgINTM.ChannelName = channelName;

                if (_logger.IsInfoEnabled)
                    _logger.Info("-> [Msg(INTM): Sender:" +
                        sender + ", Receiver: " + receiver +
                        ", OriginMsgType: " + type + ", OriginMsg:" +
                        message.ToString(HexToStrMode.ToAscPaddedHexString) +
                        "]. sending to Gateway... <" + thisMethod + ">");

                // Forward encapsulated INTM message to MessageRouter.
                Send(msgINTM);
            }
            else
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("INTM encapsulation is failed. Orginal message will be discarded! [Msg(APP):" +
                            message.ToString(HexToStrMode.ToAscPaddedHexString) +
                            ">. <" + thisMethod + ">");
                _noOfDiscardedMessage = Functions.CounterIncrease(_noOfDiscardedMessage);
            }
        }

        private void GRNF_OnSendRequest(object sender, MessageSendRequestEventArgs e)
        {
            if (e == null)
                return;

            if (e.Message == null)
                return;

            OnMessageSendRequest(e.Sender, e.Receiver, e.ChannelName, e.Message);

            // ==============================================================================
            // Notes for future projects:
            // MessageSendRequestEventArgs object e wil pass below 4 outgoing message information to 
            // MessageHandler class event via OnSendRequest event of GRNF handler class. 
            // Sender - Outgoing message sender. If empty string value is passed to MessageHandler class,
            //          then MessageHandler.ClassParameters.Sender property value will be used;
            // Receiver - Outgoing message receiver. If empty string value is passed to MessageHandler
            //          class, then MessageHandler will send outgoing message to all ready Gateways. The
            //          application code of ready Gateway are stored in value MessageHandler._readyGageways 
            //          field;
            // channelName - Name of channel from where the Outgoing message is sent. The same name as 
            //          incoming message will be used;
            // msg - Outgoing message.
            // ==============================================================================
        }
        
        /// <summary>
        /// For sending out MSG
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSendRequest(object sender, MessageSendRequestEventArgs e)
        {
            if (e == null)
                return;

            if (e.Message == null)
                return;

            OnMessageSendRequest(e.Sender, e.Receiver, e.ChannelName, e.Message);
        }
        
        /// <summary>
        /// Close the specified connection of TCPClient communication chain.
        /// If value null is passed to this method, then all connections of this chain will be closed.
        /// <para>
        /// Disconnect command will be passed to most top class SessionForwarder object
        /// in the chain, and then passed down to every chain classes to close each layer connections.
        /// </para>
        /// </summary>
        /// <param name="channelName">name of channel</param>
        public void Disconnect(string channelName)
        {
            _forwarder.Disconnect(channelName);
        }

        /// <summary>
        /// Sending message to MessageRouter via TCPClient chain classes.
        /// <para>
        /// The message will be sent to all current opened connections of TCPClient chain.
        /// </para>
        /// </summary>
        /// <param name="data"></param>
        public void Send(byte[] data)
        {
            if ((data == null) || (data.Length == 0))
                return;
            else
            {
                _noOfMsgToClnChannel = Functions.CounterIncrease(_noOfMsgToClnChannel);
                Telegram message = new Telegram(ref data);
                _forwarder.Send(message);
            }
        }

        /// <summary>
        /// Sending message to MessageRouter via TCPClient chain classes.
        /// <para>
        /// The message will be sent to all current opened connections of TCPClient chain.
        /// </para>
        /// </summary>
        /// <param name="message"></param>
        public void Send(Telegram message)
        {
            if (message == null)
                return;
            else
            {
                _noOfMsgToClnChannel = Functions.CounterIncrease(_noOfMsgToClnChannel);
                _forwarder.Send(message);
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
                    // 1. Handling incoming messages in the incoming queue...
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

                    // 2. Send out all current MDS alarm activated/deactived, and spot display commands
                    //    buffered in the database table [CCTV_MDS_OUTGOING_ALARMS] in 1 second time interval.
                    //    Upon receives GRNF message, Gateway application code included in themessage will be added into 
                    //    List object (_readyGageways). It will be removed from List upon receives CSNF message with 
                    //    "Disconnected" status or Engine lost the connection to MessageRouter. No MDS alarm is allowed 
                    //    to be sent to CCTV server if the number of items in the List object is less than 1 (no Gateway is 
                    //    ready), no metter CCTV Engine is connected to MessageRouter or not. 
                    if (_readyGageways.Count > 0)
                    {
                        // Start database table [CCTV_MDS_OUTGOING_ALARMS] monitoring and send its records to CCTV server.
                        TimeSpan timeDiff;
                        timeDiff = DateTime.Now.Subtract(_lastPollingTime);

                        if (Math.Abs(timeDiff.TotalMilliseconds) >= DBPersistor.ClassParameters.TablesPollingInterval && ClassParameters.IsEnable)
                        {
                            // In order to make sure unsubscriber will not run the checking, the counter use for decide need to check or not
                            int counter = 0;
                            for (int j = 0; j < _readyGageways.Count; j++)
                            {
                                // Check whether the Gateway has subcribe for this message
                                if (ClassParameters.CRAI_Receiver.Contains(_readyGageways[j]))
                                {
                                    counter = counter + 1;
                                }
                                else if (ClassParameters.AFAI_Receiver.Contains(_readyGageways[j]))
                                {
                                    counter = counter + 1;
                                }
                                else if (ClassParameters.FBTI_Receiver.Contains(_readyGageways[j]))
                                {
                                    counter = counter + 1;
                                }
                                else if (ClassParameters.FPTI_Receiver.Contains(_readyGageways[j]))
                                {
                                    counter = counter + 1;
                                }
                            }

                            if (counter > 0)
                            {
                                // Check for any changed of tables and send data to PLC
                                CheckChangedTables();
                            }
                            
                            _lastPollingTime = DateTime.Now;
                        }
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
        /// Whenever CCTV Engine service receives "Gateway Ready Notification" (GRNF) message sent by Gateway 
        /// application upon Gateway to its both internal and external conenction are opened, Engine will
        /// perform following 3 tasks:
        /// 1) clear all CCTV alarms stored in the database table [CCTV_STATUS].
        /// 2) clear all Outgoing MDS Alarm messages stored in the database table [CCTV_MDS_OUTGOING_ALARMS].
        /// 3) send all current MDS alarms stored in the database table [CCTV_MDS_ACTIVATED_ALARMS] to CCTV server.
        /// 4) Add ready Gateway application code into List object (_readyGageways) to inform HandlingThread 
        ///    start the monitoring of table [CCTV_MDS_OUTGOING_ALARMS] and sending its records to CCTV Gateway 
        ///    for forwarding to CCTV server.
        /// 
        /// Upon receives GRNF message, Gateway application code included in themessage will be added into 
        /// List object (_readyGageways). It will be removed from List upon receives CSNF message with 
        /// "Disconnected" status or Engine lost the connection to MessageRouter. No MDS alarm is allowed 
        /// to be sent to CCTV server if the number of items in the List object is less than 1 (no Gateway is 
        /// ready), no metter CCTV Engine is connected to MessageRouter or not. 
        /// 
        /// Whenever CCTV server is connected to CCTV Gateway Service, CCTV server shall clear all current MDS 
        /// alarms maintained by CCTV server itself.
        /// </summary>
        /// <param name="msgSource"></param>
        private void IncomingMessageHandling(PALS.Telegrams.Common.MessageAndSource msgSource)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            string channelName;
            PALS.Telegrams.Telegram message;

            try
            {
                if (msgSource == null)
                {
                    return;
                }
                else
                {
                    channelName = msgSource.Source;
                    message = msgSource.Message;
                }

                if (message.Format == null)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("[Channel:" + channelName +
                                "] Telegram format is not defined for this incoming message! Message is discarded! [Msg(APP):" +
                                message.ToString(PALS.Utilities.HexToStrMode.ToAscPaddedHexString) + "]. <"
                                + thisMethod + ">");
                    _noOfDiscardedMessage = Functions.CounterIncrease(_noOfDiscardedMessage);
                }
                else
                {
                    byte[] originType = message.GetFieldActualValue("Type");
                    string type = PALS.Utilities.Functions.ConvertByteArrayToString(
                            originType, -1, PALS.Utilities.HexToStrMode.ToAscString);

                    if (string.Compare(type, ClassParameters.MessageType_INTM) == 0)
                    {
                        // GRNF message is sent from Gateway application to Engine application only. It is 
                        // required to be routed by  MessageRouter,  therefore,  CRNF  message  needs  to  
                        // be encapsulated into INTM message by Gateway before sending out to MessageRouter. 
                        //
                        // 1) clear all CCTV alarms stored in the database table [CCTV_STATUS].
                        // 2) clear all Outgoing MDS Alarm messages stored in the database table [CCTV_MDS_OUTGOING_ALARMS].
                        // 3) send all current MDS alarms stored in the database table [CCTV_MDS_ACTIVATED_ALARMS] to CCTV server.
                        // 4) Add ready Gateway application code into List object (_readyGageways) to inform HandlingThread 
                        //    start the monitoring of table [CCTV_MDS_OUTGOING_ALARMS] and sending its records to CCTV Gateway 
                        //    for forwarding to CCTV server.
                        // ...
                        message.Format = ClassParameters.MessageFormat_INTM;
                        IncomingINTMMessageHandling(channelName, message);
                    }
                    else if (string.Compare(type, ClassParameters.MessageType_CSNF) == 0)
                    {
                        // CSNF message is sent from MessageRouter to those applications connected to it. 
                        // Since it is sent by MessageRouter directly, instead of Routed by it, CSNF message 
                        // will not be encapsulated into INTM message format.
                        //
                        // Perform Initializing or Finalizing tasks upon associated remote interface party is 
                        // connected or disconnected to MessageRouter, by means of receive CSNF message.
                        // 1. If CSNF message indicates that Gateway has disconnected to MessageRouter, then
                        //    remove the application code included in the CSNF message from List object (_readyGageways).
                        message.Format = ClassParameters.MessageFormat_CSNF;
                        IncomingCSNFMessageHandling(channelName, message);
                    }
                    else
                    {
                        // Undesired message will be discarded.
                        if (_logger.IsErrorEnabled)
                            _logger.Error("[Channel:" + channelName +
                                    "] Undesired message is received! it will be discarded... [Msg(APP):" +
                                    message.ToString(PALS.Utilities.HexToStrMode.ToAscPaddedHexString) + "]. <"
                                    + thisMethod + ">");
                        _noOfDiscardedMessage = Functions.CounterIncrease(_noOfDiscardedMessage);

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
        /// All application data were encapsulated into INTM message. This handling process will decode
        /// it into original application data first, and then following by business logic handling.
        /// </summary>
        /// <param name="channelName">ChannelName.</param>
        /// <param name="message">INTM message.</param>
        private void IncomingINTMMessageHandling(string channelName, PALS.Telegrams.Telegram message)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            byte[] originMsgData;
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
            originMsgData = message.GetFieldActualValue("OriginMsg");
            message.Format.Field("OriginMsg").Length = temp;

            if (_logger.IsDebugEnabled)
                _logger.Debug("[Channel:" + channelName + "] <- [Msg(INTM):" + 
                        message.ToString(HexToStrMode.ToAscPaddedHexString) + 
                        "]. <Sndr:" + sender + ", Rcvr: " + receiver + ", OriginMsgType: " + 
                        originMsgType + ">. <" + thisMethod + ">");

            Telegram originMsg = new Telegram(channelName, ref originMsgData);

            // ############################################################
            // To-Do-List (for future)
            // Verify the receiver name in the incoming INTM message. If it is not
            // the same as the local sender name of current application, then the
            // INTM Original message will not be sent to external.
            // ...
            // ############################################################
            
            if (string.Compare(originMsgType, ClassParameters.MessageType_GID) == 0)
            {
                // Encapsulated message is GID.
                _noOfGID = Functions.CounterIncrease(_noOfGID);
                originMsg.Format = ClassParameters.MessageFormat_GID;

                IncomingMessageInfo InMsgInfo = new IncomingMessageInfo(sender, receiver, channelName, originMsg);
                GID.MessageReceived(InMsgInfo);
            }
            else if (string.Compare(originMsgType, ClassParameters.MessageType_ICR) == 0)
            {
                // Encapsulated message is ICR message.
                _noOfICR = Functions.CounterIncrease(_noOfICR);
                originMsg.Format = ClassParameters.MessageFormat_ICR;

                IncomingMessageInfo InMsgInfo = new IncomingMessageInfo(sender, receiver, channelName, originMsg);
                ICR.MessageReceived(InMsgInfo);
            }
            else if (string.Compare(originMsgType, ClassParameters.MessageType_ISC) == 0)
            {
                // Encapsulated message is ISC message.
                _noOfISC = Functions.CounterIncrease(_noOfISC);
                originMsg.Format = ClassParameters.MessageFormat_ISC;
                ISC.MessageFormat_IRD = ClassParameters.MessageFormat_IRD;
                ISC.DLPSMaxRecords = ClassParameters.DLPSMaxRecords;
                ISC.DLPSStatusSingle = ClassParameters.DLPSStatusSingle;
                ISC.DLPSStatusDuplicate = ClassParameters.DLPSStatusDuplicate;
                ISC.DLPSSubscriberATR = ClassParameters.DLPSSubscriberATR;
                ISC.EnableSendingDLPS = ClassParameters.EnableSendingDLPS;
                ISC.IRDSubscriber = ClassParameters.IRDSubscriberATR;
                ISC.EnableSendingIRD = ClassParameters.EnabledSendingIRD;

                IncomingMessageInfo InMsgInfo = new IncomingMessageInfo(sender, receiver, channelName, originMsg);
                ISC.MessageReceived(InMsgInfo);
            }
            else if (string.Compare(originMsgType, ClassParameters.MessageType_ISE) == 0)
            {
                // Encapsulated message is ISE message.
                _noOfISE = Functions.CounterIncrease(_noOfISE);
                originMsg.Format = ClassParameters.MessageFormat_ISE;
                ISE.MessageFormat_IRD = ClassParameters.MessageFormat_IRD;

                IncomingMessageInfo InMsgInfo = new IncomingMessageInfo(sender, receiver, channelName, originMsg);
                ISE.MessageReceived(InMsgInfo);
            }
            else if (string.Compare(originMsgType, ClassParameters.MessageType_IPR) == 0)
            {
                // Encapsulated message is IPR message.
                _noOfIPR = Functions.CounterIncrease(_noOfIPR);
                originMsg.Format = ClassParameters.MessageFormat_IPR;

                IncomingMessageInfo InMsgInfo = new IncomingMessageInfo(sender, receiver, channelName, originMsg);
                IPR.MessageReceived(InMsgInfo);
            }    
            else if (string.Compare(originMsgType, ClassParameters.MessageType_ILT) == 0)
            {
                // Encapsulated message is ILT message.
                _noOfILT = Functions.CounterIncrease(_noOfILT);
                originMsg.Format = ClassParameters.MessageFormat_ILT;

                IncomingMessageInfo InMsgInfo = new IncomingMessageInfo(sender, receiver, channelName, originMsg);
                ILT.MessageReceived(InMsgInfo);
            }
            else if (string.Compare(originMsgType, ClassParameters.MessageType_ITI) == 0)
            {
                // Encapsulated message is ITI message.
                _noOfITI = Functions.CounterIncrease(_noOfITI);
                originMsg.Format = ClassParameters.MessageFormat_ITI;

                IncomingMessageInfo InMsgInfo = new IncomingMessageInfo(sender, receiver, channelName, originMsg);
                ITI.MessageReceived(InMsgInfo);
            }
            else if (string.Compare(originMsgType, ClassParameters.MessageType_MER) == 0)
            {
                // Encapsulated message is MER message.
                _noOfMER = Functions.CounterIncrease(_noOfMER);
                originMsg.Format = ClassParameters.MessageFormat_MER;
                MER.MessageFormat_IRD = ClassParameters.MessageFormat_IRD;

                IncomingMessageInfo InMsgInfo = new IncomingMessageInfo(sender, receiver, channelName, originMsg);
                MER.MessageReceived(InMsgInfo);
            }
            else if (string.Compare(originMsgType, ClassParameters.MessageType_BMAM) == 0)
            {
                // Encapsulated message is BMAM message.
                _noOfBMAM = Functions.CounterIncrease(_noOfBMAM);
                originMsg.Format = ClassParameters.MessageFormat_BMAM;

                IncomingMessageInfo InMsgInfo = new IncomingMessageInfo(sender, receiver, channelName, originMsg);
                BMAM.MessageReceived(InMsgInfo);
            }
            else if (string.Compare(originMsgType, ClassParameters.MessageType_P1500) == 0)
            {
                // Encapsulated message is P1500 message.
                _noOfP1500 = Functions.CounterIncrease(_noOfP1500);
                originMsg.Format = ClassParameters.MessageFormat_P1500;

                IncomingMessageInfo InMsgInfo = new IncomingMessageInfo(sender, receiver, channelName, originMsg);
                P1500.MessageReceived(InMsgInfo);
            }
            else if (string.Compare(originMsgType, ClassParameters.MessageType_GRNF) == 0)
            {
                // Encapsulated message is Gateway Ready Notification message.
                _noOfGRNF = Functions.CounterIncrease(_noOfGRNF);

                originMsg.Format = ClassParameters.MessageFormat_GRNF;

                IncomingMessageInfo InMsgInfo = new IncomingMessageInfo(sender, receiver, channelName, originMsg);
                GRNF.MessageReceived(InMsgInfo);

                if (ClassParameters.IsEnable)
                {
                    if (ClassParameters.AFAI_Receiver.Contains(sender))
                    {
                        ProduceAFAIMessage(InMsgInfo, true);
                    }

                    if (ClassParameters.CRAI_Receiver.Contains(sender))
                    {
                        ProduceCRAIMessage(InMsgInfo, true, DBPersistor.ClassParameters.STP_SAC_COLLECTENTIRECRAI);
                    }

                    if (ClassParameters.FBTI_Receiver.Contains(sender))
                    {
                        ProduceFBTIMessage(InMsgInfo, true, DBPersistor.ClassParameters.STP_SAC_COLLECTENTIREFBTI);
                    }

                    if (ClassParameters.FPTI_Receiver.Contains(sender))
                    {
                        ProduceFPTIMessage(InMsgInfo, true, DBPersistor.ClassParameters.STP_SAC_COLLECTENTIREFPTI);
                    }

                }
                // ==============================================================
                // Following 4 tasks will be performed by message handler: GRNF:
                // 1. Decode GRNF message to extract the Gateway application code.
                // 4. send all current AFAI, CRAI, FBTI, FPTI, TPTI stored in the database table
                //     to all ready Gateways.
                // ==============================================================

                // The last task done by this class:
                // 5. Add Gateway application into List object (_readyGageways).
                if (InMsgInfo.GRNF_AppCode != string.Empty)
                {
                    lock (_thisLock)
                    {
                        if (_readyGageways.Contains(InMsgInfo.GRNF_AppCode) == false)
                            _readyGageways.Add(InMsgInfo.GRNF_AppCode);
                    }
                }            
            }
            else
            {
                // Undesired message will be discarded
                if (_logger.IsErrorEnabled)
                    _logger.Error("[Channel:" + channelName +
                            "] Undesired message is received! it will be discarded... [Msg(APP):" +
                            message.ToString(PALS.Utilities.HexToStrMode.ToAscPaddedHexString) + "]. <"
                            + thisMethod + ">");
                _noOfDiscardedMessage = Functions.CounterIncrease(_noOfDiscardedMessage);
            }
        }

        /// <summary>
        /// Perform Initializing or Finalizing tasks upon associated remote interface party is 
        /// conencted or disconnected to MessageRouter, by means of receive CSNF message.
        /// 1. If CSNF message indicates that Gateway has disconnected to MessageRouter, then
        ///    remove the application code included in the CSNF message from List 
        ///    object (_readyGageways).
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
                                "]. Engine associated application (" + gwAppCode + 
                                ") has connected to MessageRouter" + ". <" + thisMethod + ">");

                    break;
                case CSNF_COMM_STATUS_CLOSED:
                    //    Add Gateway application code from List (_readyGageways) will not be performed by 
                    //    receiving of CSNF message. It is because that Gateway has two connections, one to 
                    //    MessageRouter, another one to external devices (PLC, CCTV server, etc.). The CSNF 
                    //    message with "Opened" status will be sent upon Gateway is connected to 
                    //    MessageRouter. But at that time, Gateway may not be connected to external device. 
                    //    Hence, Engine will treat the particular Gateway is ready and add its name into 
                    //    List (_readyGageways) only when receives "Gateway Ready Notification" (GRNF) 
                    //    message, instead of CSNF message.
                    lock (_thisLock)
                    {
                        if (_readyGageways.Contains(gwAppCode) == true)
                            _readyGageways.Remove(gwAppCode);
                    }

                    if (_logger.IsErrorEnabled)
                        _logger.Error("[Channel:" + channelName + "] <- [Msg(CSNF):" +
                                message.ToString(PALS.Utilities.HexToStrMode.ToAscPaddedHexString) +
                                "]. Engine associated application (" + gwAppCode + 
                                ") has disconnected to MessageRouter" + "! <" + thisMethod + ">");

                    break;
            }
        }

        /// <summary>
        /// Constructing BagEvent message by encapsulating ItemTracking message received from
        /// external device (e.g. PLC).
        /// </summary>
        /// <param name="sender">Outgoing message sender. It shall be the code of current Engine application.</param>
        /// <param name="receiver">Outgoing message final receiver. it shall be the code of associated Gateway application.</param>
        /// <param name="originType">The type of original message that need to be encapsulated into INTM message.</param>
        /// <param name="originMsg">original message.</param>
        /// <param name="formatINTM">INTM message format object.</param>
        /// <returns></returns>
        private Telegram ConstructINTMMessage(string sender, string receiver, 
                        byte[] originType, Telegram originMsg, TelegramFormat formatINTM)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            Telegram msgINTM = null;

            if (sender == string.Empty)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("sender name is empty, no INTM message is constructed! <" +
                            thisMethod + ">");
                return null;
            }

            if (receiver == string.Empty)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("receiver name is empty, no INTM message is constructed! <" +
                            thisMethod + ">");
                return null;
            }

            if (originType == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Original message type is null, no INTM message is constructed! <" +
                            thisMethod + ">");
                return null;
            }

            if (originMsg == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Original message is null, no INTM message is constructed! <" +
                            thisMethod + ">");
                return null;
            }

            if (formatINTM == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("INTM message format object is null, no INTM message is constructed! <" +
                            thisMethod + ">");
                return null;
            }

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
                            newSeq.ToString(), fieldLen, '0', Functions.PaddingRule.Left);
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
                sndr = Functions.ConvertStringToFixLengthByteArray(sender,
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
                rcvr = Functions.ConvertStringToFixLengthByteArray(receiver,
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

                fieldLen = originMsg.RawData.Length;
                msgLen = msgLen + fieldLen;
                msgINTM.Format.Field("OriginMsg").Length = fieldLen;
                origin = originMsg.RawData;
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
                    _logger.Error("Constructing INTM message is failed! <" + thisMethod + ">", ex);

                return null;
            }
        }


        //private void SendMDSAlarmAndSpotCommand()
        //{
        //    string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
        //    SqlConnection sqlConn = null;
        //    SqlDataAdapter adapter = null;
        //    SqlCommand sqlCmd = null;
        //    DataSet dataSet = null;

        //    try
        //    {
        //        sqlConn = new SqlConnection(DBPersistor.ClassParameters.DBConnectionString);
        //        adapter = new SqlDataAdapter();
        //        sqlCmd = new SqlCommand(DBPersistor.ClassParameters.STP_CCTV_GetOutgoingMDSAlarms, sqlConn);
        //        sqlCmd.CommandType = CommandType.StoredProcedure;
        //        adapter.SelectCommand = sqlCmd;

        //        sqlConn.Open();
        //        dataSet = new DataSet();
        //        adapter.Fill(dataSet);

        //        if (dataSet.Tables.Count > 0)
        //        {
        //            foreach (DataRow row in dataSet.Tables[0].Rows)
        //            {
        //                // ==================================================
        //                // SQL script in the StoredProcedurem [stp_CCTV_GETOUTGOINGMDSALARMS]:
        //                // SELECT [ID],[ACTION],[TIME_STAMP],[SUBSYSTEM],[EQUIPMENT_ID],[ALARM_TYPE],
        //                //        [ALARM_DESCRIPTION],[CCTV_DEVICE_TYPE],[CCTV_DEVICE_ID],[CCTV_CAMERA_POSITION_ID]
        //                //    FROM [dbo].[CCTV_MDS_OUTGOING_ALARMS]
        //                // ==================================================
        //                long id = 0;
        //                string action = string.Empty;

        //                if (!row.IsNull("ID")) id = (long)row["ID"];
        //                if (!row.IsNull("ACTION")) action = (string)row["ACTION"];

        //                if (string.Compare(action, DBPersistor.ClassParameters.Action_MDS_Activated) == 0)
        //                {
        //                    // Send MDS outgoing Activated alarm to all ready Gateways.
        //                    ProduceMAAVMessage(sqlConn, row, id, action);
        //                }
        //                else if (string.Compare(action, DBPersistor.ClassParameters.Action_MDS_Deactivated) == 0)
        //                {
        //                    // Send MDS outgoing Deactivated alarm to all ready Gateways.
        //                    ProduceMANLMessage(sqlConn, row, id, action);
        //                }
        //                else if (string.Compare(action, DBPersistor.ClassParameters.Action_MDS_Spot) == 0)
        //                {
        //                    // Send MDS Spot Display Command to all ready Gateways.
        //                    ProduceMSDCMessage(sqlConn, row, id, action);
        //                }
        //                else
        //                {
        //                    if (_logger.IsErrorEnabled)
        //                        _logger.Error("Invalid ACTION field value (" + action +
        //                            ") is fount, no message will be sent to CCTV server! <" + thisMethod + ">");
        //                }
        //            }
        //        }
        //        return;
        //    }
        //    catch (SqlException ex)
        //    {
        //        if (_logger.IsErrorEnabled)
        //            _logger.Error("Sending MDS current alarm failure! <" + thisMethod + ">", ex);

        //        return;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (_logger.IsErrorEnabled)
        //            _logger.Error("Sending MDS current alarm failure! <" + thisMethod + ">", ex);

        //        return;
        //    }
        //    finally
        //    {
        //        if (dataSet != null)
        //        {
        //            dataSet.Clear();
        //            dataSet = null;
        //        }

        //        if (sqlConn != null) 
        //            sqlConn.Close();
        //    }
        //}

        /// <summary>
        /// Create Airline Code and Function Allocation Message
        /// </summary>
        /// <param name="msgInfo"></param>
        /// <param name="isGWReady"></param>
        private void ProduceAFAIMessage(IncomingMessageInfo msgInfo, bool isGWReady)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            string strNoCarrierDest = string.Empty;
            string strNoAllocationDest = string.Empty;
            string strDumpDischargeDest = string.Empty;
            string strNoReadDest = string.Empty;

            SqlConnection sqlConn = null;
            sqlConn = new SqlConnection(DBPersistor.ClassParameters.DBConnectionString);
            SqlCommand sqlCmd = new SqlCommand(DBPersistor.ClassParameters.STP_SAC_COLLECTENTIREAFAI, sqlConn);
            sqlCmd.CommandType = CommandType.StoredProcedure;
            try
            {
                sqlCmd.Parameters.Add("@AirportDesc", SqlDbType.VarChar, 30);
                sqlCmd.Parameters["@AirportDesc"].Value = DBPersistor.ClassParameters.AIRPORT_CODE_DESC;

                sqlCmd.Parameters.Add("@NoCarrierDesc", SqlDbType.VarChar, 4);
                sqlCmd.Parameters["@NoCarrierDesc"].Value = DBPersistor.ClassParameters.FUNC_ALLOCATION_NOCR;

                sqlCmd.Parameters.Add("@NoAllocDesc", SqlDbType.VarChar, 4);
                sqlCmd.Parameters["@NoAllocDesc"].Value = DBPersistor.ClassParameters.FUNC_ALLOCATION_NOAL;

                sqlCmd.Parameters.Add("@DumpDischargeDesc", SqlDbType.VarChar, 4);
                sqlCmd.Parameters["@DumpDischargeDesc"].Value = DBPersistor.ClassParameters.FUNC_ALLOCATION_DUMP;

                sqlCmd.Parameters.Add("@NoReadDesc", SqlDbType.VarChar, 4);
                sqlCmd.Parameters["@NoReadDesc"].Value = DBPersistor.ClassParameters.FUNC_ALLOCATION_NORD;
                
                sqlCmd.Parameters.Add("@Airport", SqlDbType.VarChar, 4);
                sqlCmd.Parameters["@Airport"].Direction = ParameterDirection.Output;

                sqlCmd.Parameters.Add("@NoCarrier", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@NoCarrier"].Direction = ParameterDirection.Output;

                sqlCmd.Parameters.Add("@NoAlloc", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@NoAlloc"].Direction = ParameterDirection.Output;

                sqlCmd.Parameters.Add("@DumpDischarge", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@DumpDischarge"].Direction = ParameterDirection.Output;

                sqlCmd.Parameters.Add("@NoRead", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@NoRead"].Direction = ParameterDirection.Output;

                sqlCmd.Parameters.Add("@DumpDischargeDest", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@DumpDischargeDest"].Direction = ParameterDirection.Output;

                sqlCmd.Parameters.Add("@NoAllocDest", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@NoAllocDest"].Direction = ParameterDirection.Output;

                sqlCmd.Parameters.Add("@NoCarrierDest", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@NoCarrierDest"].Direction = ParameterDirection.Output;

                sqlCmd.Parameters.Add("@NoReadDest", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@NoReadDest"].Direction = ParameterDirection.Output;

                AFAI hdlrAFAI = new AFAI(ClassParameters.MessageFormat_AFAI);
                             
                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();

                if (!Convert.IsDBNull(sqlCmd.Parameters["@Airport"].Value))
                {
                    hdlrAFAI.AirportLocationCode = sqlCmd.Parameters["@Airport"].Value.ToString();
                     
                    //Common.AirportLocationCode = sqlCmd.Parameters["@Airport"].Value.ToString();
                }
                else
                {
                    hdlrAFAI.AirportLocationCode = string.Empty;
                }

                if (!Convert.IsDBNull(sqlCmd.Parameters["@NoCarrier"].Value))
                {
                    hdlrAFAI.NoCarrierDestination = int.Parse(sqlCmd.Parameters["@NoCarrier"].Value.ToString());
                }
                else
                {
                    hdlrAFAI.NoCarrierDestination = 0;
                }

                if (!Convert.IsDBNull(sqlCmd.Parameters["@NoAlloc"].Value))
                {
                    hdlrAFAI.NoAllocationDestination = int.Parse(sqlCmd.Parameters["@NoAlloc"].Value.ToString());
                }
                else
                {
                    hdlrAFAI.NoAllocationDestination = 0;
                }

                if (!Convert.IsDBNull(sqlCmd.Parameters["@DumpDischarge"].Value))
                {
                    hdlrAFAI.DumpDischargeDestination = int.Parse(sqlCmd.Parameters["@DumpDischarge"].Value.ToString());
                }
                else
                {
                    hdlrAFAI.DumpDischargeDestination = 0;
                }

                if (!Convert.IsDBNull(sqlCmd.Parameters["@NoRead"].Value))
                {
                    hdlrAFAI.NoReadDestination = int.Parse(sqlCmd.Parameters["@NoRead"].Value.ToString());
                }
                else
                {
                    hdlrAFAI.NoReadDestination = 0;
                }

                if (!Convert.IsDBNull(sqlCmd.Parameters["@DumpDischargeDest"].Value))
                {
                    strDumpDischargeDest = sqlCmd.Parameters["@DumpDischargeDest"].Value.ToString();
                }
                else
                {
                    strDumpDischargeDest = "-";
                }

                if (!Convert.IsDBNull(sqlCmd.Parameters["@NoAllocDest"].Value))
                {
                    strNoAllocationDest = sqlCmd.Parameters["@NoAllocDest"].Value.ToString();
                }
                else
                {
                    strNoAllocationDest = "-";
                }

                if (!Convert.IsDBNull(sqlCmd.Parameters["@NoCarrierDest"].Value))
                {
                   strNoCarrierDest = sqlCmd.Parameters["@NoCarrierDest"].Value.ToString();
                }
                else
                {
                    strNoCarrierDest = "-";
                }

                if (!Convert.IsDBNull(sqlCmd.Parameters["@NoReadDest"].Value))
                {
                    strNoReadDest = sqlCmd.Parameters["@NoReadDest"].Value.ToString();
                }
                else
                {
                    strNoReadDest = "-";
                }

                if (!Convert.IsDBNull(sqlCmd.Parameters["@NoReadDest"].Value))
                {
                    strNoReadDest = sqlCmd.Parameters["@NoReadDest"].Value.ToString();
                }
                else
                {
                    strNoReadDest = "-";
                }

                Telegram msgAFAI = hdlrAFAI.ConstructAFAIMessage();

                if (msgAFAI != null)
                {
                    if (_logger.IsDebugEnabled)
                        _logger.Debug("-> [Msg(AFAI):" +
                            " Airport Location Code=" + hdlrAFAI.AirportLocationCode +
                            ", No Carrier Destination=" + hdlrAFAI.NoCarrierDestination + " (" + strNoCarrierDest + ")" +
                            ", No Allocation Destination=" + hdlrAFAI.NoAllocationDestination + " (" + strNoAllocationDest + ")" +
                            ", Dump Discharge Destination=" + hdlrAFAI.DumpDischargeDestination + " (" + strDumpDischargeDest + ")" + 
                            ", No Read Destination=" + hdlrAFAI.NoReadDestination + " (" + strNoReadDest + ")" +
                            "]. <" + thisMethod + ">");

                    lock (_thisLock)
                    {
                        if (isGWReady)
                        {
                            // Check whether the Gateway has subcribe for this message
                            if (ClassParameters.AFAI_Receiver.Contains(msgInfo.Sender))
                            {
                                // Send to all ready Gateways.
                                OnMessageSendRequest(msgInfo.Receiver, msgInfo.Sender, msgInfo.ChannelName, msgAFAI);

                                // Log sent data into historical DB table.
                                DBPersistor.SentAFAILogging(msgInfo.Receiver, msgInfo.Sender, hdlrAFAI.AirportLocationCode,
                                                hdlrAFAI.NoCarrierDestination.ToString(), hdlrAFAI.NoAllocationDestination.ToString(),
                                                hdlrAFAI.DumpDischargeDestination.ToString(), hdlrAFAI.NoReadDestination.ToString());
                            }
                        }
                        else
                        {
                            for (int i = 0; i < _readyGageways.Count; i++)
                            {
                                // Check whether the Gateway has subcribe for this message
                                if (ClassParameters.AFAI_Receiver.Contains(_readyGageways[i]))
                                {
                                    // Send to all ready Gateways.
                                    OnMessageSendRequest(ClassParameters.Sender, _readyGageways[i], string.Empty, msgAFAI);

                                    // Log sent data into historical DB table.
                                    DBPersistor.SentAFAILogging(ClassParameters.Sender, _readyGageways[i], hdlrAFAI.AirportLocationCode,
                                                    hdlrAFAI.NoCarrierDestination.ToString(), hdlrAFAI.NoAllocationDestination.ToString(),
                                                    hdlrAFAI.DumpDischargeDestination.ToString(), hdlrAFAI.NoReadDestination.ToString());
                                }
                            }
                        }

                    }
                }
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Sending AFAI for sql failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Sending AFAI failure! <" + thisMethod + ">", ex);

                return;
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
            }
        }

        /// <summary>
        /// Create Carrier Allocation Information Message
        /// </summary>
        /// <param name="msgInfo"></param>
        /// <param name="isGWReady"></param>
        /// <param name="dbSTPString"></param>
        private void ProduceCRAIMessage(IncomingMessageInfo msgInfo, bool isGWReady, string dbSTPString)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            sqlConn = new SqlConnection(DBPersistor.ClassParameters.DBConnectionString);
            SqlCommand sqlCmd = new SqlCommand(dbSTPString, sqlConn);
            sqlCmd.CommandType = CommandType.StoredProcedure;
            SqlDataReader sqlReader;
            int carrierCodeFieldLen, sortDeviceDestFieldLen,  counter;

            List<CRAIList> singleCRAI = new List<CRAIList>();
            List<CRAIList> newSigleCRAI = new List<CRAIList>();
            CRAIList temp = new CRAIList();
            try
            {
                CRAI hdlrCRAI = new CRAI(ClassParameters.MessageFormat_CRAI);

                carrierCodeFieldLen = ClassParameters.MessageFormat_CRAI.Field("CARRIER").Length;
                sortDeviceDestFieldLen = ClassParameters.MessageFormat_CRAI.Field("DEST").Length;

                sqlConn.Open();
                sqlReader = sqlCmd.ExecuteReader();
                counter = 0;
                //readerLoopCounter = 0;

                while (sqlReader.Read())
                {
                    if (!sqlReader.IsDBNull(0))
                    {
                        temp.CarrierCode = int.Parse(sqlReader.GetSqlValue(0).ToString());
                    }
                    else
                    {
                        temp.CarrierCode = 0;
                    }

                    if (!sqlReader.IsDBNull(1)) 
                    { 
                        temp.SortDeviceDestination = int.Parse(sqlReader.GetSqlValue(1).ToString()); 
                    }
                    else
                    {
                        temp.SortDeviceDestination = 0;
                    }

                    if (!sqlReader.IsDBNull(2))
                    {
                        temp.SortDeviceDescription = sqlReader.GetValue(2).ToString();
                    }
                    else
                    {
                        temp.SortDeviceDescription = string.Empty;
                    }
                    
                    singleCRAI.Add(temp);
                }



                CRAIList[] tempList = new CRAIList[singleCRAI.Count];
                tempList = singleCRAI.ToArray();

                for (int i = 0; i < singleCRAI.Count; i++)
                {
                    newSigleCRAI.Add(tempList[i]);
                    counter = counter + 1;

                    if (((i + 1) % 10 == 0) || ((i + 1) == singleCRAI.Count))
                    {
                        hdlrCRAI.NoOfCarrier = newSigleCRAI.Count;
                        hdlrCRAI.AllocationData = newSigleCRAI.ToArray();
                        Telegram msgCRAI = hdlrCRAI.ConstructCRAIMessage();
                        //groupData.Add(newSigleCRAI);
                        string display = string.Empty;

                        for (int j = 0; j < newSigleCRAI.Count; j++)
                        {
                            display = display + "Data " + (j + 1) + "-(" + newSigleCRAI[j].CarrierCode.ToString().PadLeft(3,'0') + ", " + newSigleCRAI[j].SortDeviceDestination + "-[" + newSigleCRAI[j].SortDeviceDescription + "])";
                        }

                        if (msgCRAI != null)
                        {
                            if (_logger.IsDebugEnabled)
                                _logger.Debug("-> [Msg(CRAI):" +
                                    " No Of Carrier=" + hdlrCRAI.NoOfCarrier +
                                    ", Data (Carrier Code, Sort Device Dest) =" + display + "," +
                                    "]. <" + thisMethod + ">");

                            lock (_thisLock)
                            {
                                if (isGWReady)
                                {
                                    // Check whether the Gateway has subcribe for this message
                                    if (ClassParameters.CRAI_Receiver.Contains(msgInfo.Sender))
                                    {
                                        // Send to all ready Gateways.
                                        OnMessageSendRequest(msgInfo.Receiver, msgInfo.Sender, msgInfo.ChannelName, msgCRAI);

                                        // Log sent data into historical DB table.
                                        DBPersistor.SentCRAILogging(msgInfo.Receiver, msgInfo.Sender, hdlrCRAI.NoOfCarrier, newSigleCRAI.ToArray());
                                    }
                                }
                                else
                                {
                                    for (int j = 0; j < _readyGageways.Count; j++)
                                    {
                                        // Check whether the Gateway has subscribe for this message
                                        if (ClassParameters.CRAI_Receiver.Contains(_readyGageways[j]))
                                        {
                                            // Send to all ready Gateways.
                                            OnMessageSendRequest(ClassParameters.Sender, _readyGageways[j], string.Empty, msgCRAI);

                                            // Log sent data into historical DB table.
                                            DBPersistor.SentCRAILogging(ClassParameters.Sender, _readyGageways[j], hdlrCRAI.NoOfCarrier, newSigleCRAI.ToArray());
                                        }
                                    }
                                }
                            }
                        }
                        counter = 0;
                        newSigleCRAI = new List<CRAIList>();
                    }
                }
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Sending CRAI for sql failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Sending CRAI failure! <" + thisMethod + ">", ex);

                return;
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
            }
        }

        /// <summary>
        /// Create Fallback Tag Information Message 
        /// </summary>
        /// <param name="msgInfo"></param>
        /// <param name="isGWReady"></param>
        /// <param name="dbSTPString"></param>
        private void ProduceFBTIMessage(IncomingMessageInfo msgInfo, bool isGWReady, string dbSTPString)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            sqlConn = new SqlConnection(DBPersistor.ClassParameters.DBConnectionString);
            SqlCommand sqlCmd = new SqlCommand(dbSTPString, sqlConn);
            sqlCmd.CommandType = CommandType.StoredProcedure;
            SqlDataReader sqlReader;
            int codeFieldLen, destFieldLen, counter;

            List<Tag[]> groupData = new List<Tag[]>();
            List<Tag> singleFBTI = new List<Tag>();
            List<Tag> newSingleFBTI = new List<Tag>();
            Tag temp = new Tag();

            try
            {
                FBTI hdlr = new FBTI(ClassParameters.MessageFormat_FBTI);

                codeFieldLen = ClassParameters.MessageFormat_FBTI.Field("FALLBACK_NO").Length;
                destFieldLen = ClassParameters.MessageFormat_FBTI.Field("DEST").Length;

                sqlConn.Open();
                sqlReader = sqlCmd.ExecuteReader();
                counter = 0;

                while (sqlReader.Read())
                {
                    if (!sqlReader.IsDBNull(0))
                    {
                        temp.Code = int.Parse(sqlReader.GetSqlValue(0).ToString().Trim());
                    }
                    else
                    {
                        temp.Code = 0;
                    }

                    if (!sqlReader.IsDBNull(1))
                    {
                        temp.Destination = int.Parse(sqlReader.GetSqlValue(1).ToString().Trim());
                    }
                    else
                    {
                        temp.Destination= 0;
                    }

                    if (!sqlReader.IsDBNull(2))
                    {
                        temp.DestinationDescription = sqlReader.GetSqlValue(2).ToString().Trim();
                    }
                    else
                    {
                        temp.DestinationDescription = string.Empty;
                    }

                    singleFBTI.Add(temp);
                }

                Tag[] tempTag = new Tag[singleFBTI.Count];
                tempTag = singleFBTI.ToArray();

                for (int i = 0; i < singleFBTI.Count; i++)
                {
                    newSingleFBTI.Add(tempTag[i]);
                    counter = counter + 1;

                    if (((i + 1) % 10 == 0) || ((i + 1) == singleFBTI.Count))
                    {
                        hdlr.NoOfFallback = newSingleFBTI.Count;
                        hdlr.AllocationData = newSingleFBTI.ToArray();
                        Telegram msg = hdlr.ConstructFBTIMessage();

                        string display = string.Empty;

                        for (int j = 0; j < newSingleFBTI.Count; j++)
                        {
                            display = display + "Data " + (j + 1) + "-(" + newSingleFBTI[j].Code.ToString().PadLeft(2,'0') + ", " + newSingleFBTI[j].Destination+ " [" + newSingleFBTI[j].DestinationDescription 
                                        + "])";
                        }

                        if (msg != null)
                        {
                            if (_logger.IsDebugEnabled)
                                _logger.Debug("-> [Msg(FBTI):" +
                                    " No Of Fallback Tag=" + hdlr.NoOfFallback.ToString().PadLeft(2,'0') +
                                    ", Data (Fallback Code, Destination) = " + display + "," +
                                    "]. <" + thisMethod + ">");

                            lock (_thisLock)
                            {
                                if (isGWReady)
                                {
                                    // Check whether the Gateway has subcribe for this message
                                    if (ClassParameters.FBTI_Receiver.Contains(msgInfo.Sender))
                                    {
                                        // Send to all ready Gateways.
                                        OnMessageSendRequest(msgInfo.Receiver, msgInfo.Sender, msgInfo.ChannelName, msg);

                                        // Log sent data into historical DB table.
                                        DBPersistor.SentFBTILogging(msgInfo.Receiver, msgInfo.Sender, hdlr.NoOfFallback, newSingleFBTI.ToArray());
                                    }
                                }
                                else
                                {
                                    for (int j = 0; j < _readyGageways.Count; j++)
                                    {
                                        // Check whether the Gateway has subcribe for this message
                                        if (ClassParameters.FBTI_Receiver.Contains(_readyGageways[j]))
                                        {
                                            // Send to all ready Gateways.
                                            OnMessageSendRequest(ClassParameters.Sender, _readyGageways[j], string.Empty, msg);

                                            // Log sent data into historical DB table.
                                            DBPersistor.SentFBTILogging(ClassParameters.Sender, _readyGageways[j], hdlr.NoOfFallback, newSingleFBTI.ToArray());
                                        }
                                    }
                                }
                            }

                        }
                        counter = 0;
                        newSingleFBTI = new List<Tag>();
                    }
                }
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Sending FBTI for sql failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Sending FBTI failure! <" + thisMethod + ">", ex);

                return;
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
            }
        }

        /// <summary>
        /// Create Four Pier Tag Information 
        /// </summary>
        /// <param name="msgInfo"></param>
        /// <param name="isGWReady"></param>
        /// <param name="dbSTPString"></param>
        private void ProduceFPTIMessage(IncomingMessageInfo msgInfo, bool isGWReady, string dbSTPString)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            sqlConn = new SqlConnection(DBPersistor.ClassParameters.DBConnectionString);
            SqlCommand sqlCmd = new SqlCommand(dbSTPString, sqlConn);
            sqlCmd.CommandType = CommandType.StoredProcedure;
            SqlDataReader sqlReader;
            int codeFieldLen, destFieldLen, counter;

            List<Tag> singleFPTI = new List<Tag>();
            List<Tag> newSingleFPTI = new List<Tag>();
            Tag temp = new Tag();
            
            try
            {
                FPTI hdlr = new FPTI(ClassParameters.MessageFormat_FPTI);

                codeFieldLen = ClassParameters.MessageFormat_FPTI.Field("4PIER_NO").Length;
                destFieldLen = ClassParameters.MessageFormat_FPTI.Field("DEST").Length;

                sqlConn.Open();
                sqlReader = sqlCmd.ExecuteReader();
                counter = 0;

                while (sqlReader.Read())
                {
                    if (!sqlReader.IsDBNull(0))
                    {
                        temp.Code = int.Parse(sqlReader.GetSqlValue(0).ToString().Trim());
                    }
                    else
                    {
                        temp.Code = 0;
                    }

                    if (!sqlReader.IsDBNull(1))
                    {
                        temp.Destination = int.Parse(sqlReader.GetSqlValue(1).ToString().Trim());
                    }
                    else
                    {
                        temp.Destination = 0;
                    }

                    if (!sqlReader.IsDBNull(1))
                    {
                        temp.DestinationDescription = sqlReader.GetSqlValue(2).ToString().Trim();
                    }
                    else
                    {
                        temp.DestinationDescription = string.Empty;
                    }

                    singleFPTI.Add(temp);
                }

                Tag[] tempTag = new Tag[singleFPTI.Count];
                tempTag = singleFPTI.ToArray();

                for (int i = 0; i < tempTag.Length; i++)
                {
                    newSingleFPTI.Add(tempTag[i]);
                    counter = counter + 1;

                    if (((i + 1) % 10 == 0) || ((i + 1) == singleFPTI.Count))
                    {
                        hdlr.NoOfFourPier = newSingleFPTI.Count;
                        hdlr.AllocationData = newSingleFPTI.ToArray();
                        Telegram msg = hdlr.ConstructFPTIMessage();

                        string display = string.Empty;

                        for (int j = 0; j < newSingleFPTI.Count; j++)
                        {
                            display = display + "Data " + (j + 1) + "-(" + newSingleFPTI[j].Code.ToString().PadLeft(4,'0') + ", " + newSingleFPTI[j].Destination + " [" + newSingleFPTI[j].DestinationDescription  
                                        + "])";
                        }

                        if (msg != null)
                        {
                            if (_logger.IsDebugEnabled)
                                _logger.Debug("-> [Msg(FPTI):" +
                                    " No Of Four Digits Tag=" + hdlr.NoOfFourPier +
                                    ", Data (Four Digits Tag Code, Destination) = " + display + "," +
                                    "]. <" + thisMethod + ">");

                            lock (_thisLock)
                            {
                                if (isGWReady)
                                {
                                    // Check whether the Gateway has subcribe for this message
                                    if (ClassParameters.FPTI_Receiver.Contains(msgInfo.Sender))
                                    {
                                        // Send to all ready Gateways.
                                        OnMessageSendRequest(msgInfo.Receiver, msgInfo.Sender, msgInfo.ChannelName, msg);

                                        // Log sent data into historical DB table.
                                        DBPersistor.SentFPTILogging(msgInfo.Receiver, msgInfo.Sender, hdlr.NoOfFourPier, newSingleFPTI.ToArray());
                                    }
                                }
                                else
                                {
                                    for (int j = 0; j < _readyGageways.Count; j++)
                                    {
                                        // Check whether the Gateway has subcribe for this message
                                        if (ClassParameters.FPTI_Receiver.Contains(_readyGageways[j]))
                                        {
                                            // Send to all ready Gateways.
                                            OnMessageSendRequest(ClassParameters.Sender, _readyGageways[j], string.Empty, msg);

                                            // Log sent data into historical DB table.
                                            DBPersistor.SentFPTILogging(ClassParameters.Sender, _readyGageways[j], hdlr.NoOfFourPier, newSingleFPTI.ToArray());
                                        }
                                    }
                                }
                            }
                        }
                        counter = 0;
                        newSingleFPTI = new List<Tag>();
                    }
                }
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Sending FPTI for sql failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Sending FPTI failure! <" + thisMethod + ">", ex);

                return;
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
            }
        }
        
        //private void ProduceTPTIMessage(IncomingMessageInfo msgInfo, bool isGWReady, string dbSTPString)
        //{
        //    string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
        //    SqlConnection sqlConn = null;
        //    sqlConn = new SqlConnection(DBPersistor.ClassParameters.DBConnectionString);
        //    SqlCommand sqlCmd = new SqlCommand(dbSTPString, sqlConn);
        //    sqlCmd.CommandType = CommandType.StoredProcedure;
        //    SqlDataReader sqlReader;
        //    int codeFieldLen, destFieldLen, counter;

        //    List<Tag> singleTPTI = new List<Tag>();
        //    List<Tag> newSingleTPTI = new List<Tag>();
        //    Tag temp = new Tag();

        //    try
        //    {
        //        TPTI hdlr = new TPTI(ClassParameters.MessageFormat_TPTI);

        //        codeFieldLen = ClassParameters.MessageFormat_TPTI.Field("TwoPierCode").Length;
        //        destFieldLen = ClassParameters.MessageFormat_TPTI.Field("Destination").Length;

        //        sqlConn.Open();
        //        sqlReader = sqlCmd.ExecuteReader();
        //        counter = 0;

        //        while (sqlReader.Read())
        //        {
        //            if (!sqlReader.IsDBNull(0))
        //            {
        //                temp.Code = sqlReader.GetSqlValue(0).ToString().Trim();
        //            }
        //            else
        //            {
        //                temp.Code = string.Empty;
        //            }

        //            if (!sqlReader.IsDBNull(1))
        //            {
        //                temp.Destination = sqlReader.GetSqlValue(1).ToString().Trim();
        //            }
        //            else
        //            {
        //                temp.Destination = string.Empty;
        //            }

        //            singleTPTI.Add(temp);
        //        }

        //        Tag[] tempTag = new Tag[singleTPTI.Count];
        //        tempTag = singleTPTI.ToArray();

        //        for (int i = 0; i < singleTPTI.Count; i++)
        //        {
        //            newSingleTPTI.Add(tempTag[i]);
        //            counter = counter + 1;

        //            if (((i + 1) % 10 == 0) || ((i + 1) == singleTPTI.Count))
        //            {
        //                hdlr.NoOfTwoPier = newSingleTPTI.Count;
        //                hdlr.AllocationData = newSingleTPTI.ToArray();

        //                Telegram msg = hdlr.ConstructTPTIMessage();
        //                string display = string.Empty;

        //                for (int j = 0; j < newSingleTPTI.Count; j++)
        //                {
        //                    display = display + "Data " + (j + 1) + "-(" + newSingleTPTI[j].Code + ", " + newSingleTPTI[j].Destination
        //                                + ")";
        //                }

        //                if (msg != null)
        //                {
        //                    if (_logger.IsDebugEnabled)
        //                        _logger.Debug("-> [Msg(TPTI):" +
        //                            " No Of Two Pier Tag=" + hdlr.NoOfTwoPier +
        //                            ", Data (Two Pier Tag Code, Destination) = " + display +
        //                            "]. <" + thisMethod + ">");

        //                    lock (_thisLock)
        //                    {
        //                        if (isGWReady)
        //                        {
        //                            // Check whether the Gateway has subcribe for this message
        //                            if (ClassParameters.TPTI_Receiver.Contains(msgInfo.Sender))
        //                            {
        //                                // Send to all ready Gateways.
        //                                OnMessageSendRequest(msgInfo.Receiver, msgInfo.Sender, msgInfo.ChannelName, msg);

        //                                // Log sent data into historical DB table.
        //                                DBPersistor.SentTPTILogging(msgInfo.Receiver, msgInfo.Sender, hdlr.NoOfTwoPier, newSingleTPTI.ToArray());
        //                            }
        //                        }
        //                        else
        //                        {
        //                            for (int j = 0; j < _readyGageways.Count; j++)
        //                            {
        //                                // Check whether the Gateway has subcribe for this message
        //                                if (ClassParameters.TPTI_Receiver.Contains(_readyGageways[j]))
        //                                {
        //                                    // Send to all ready Gateways.
        //                                    OnMessageSendRequest(ClassParameters.Sender, _readyGageways[j], string.Empty, msg);

        //                                    // Log sent data into historical DB table.
        //                                    DBPersistor.SentTPTILogging(ClassParameters.Sender, _readyGageways[j], hdlr.NoOfTwoPier, newSingleTPTI.ToArray());
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //                counter = 0;
        //                newSingleTPTI = new List<Tag>();
        //            }
        //        }
        //    }
        //    catch (SqlException ex)
        //    {
        //        if (_logger.IsErrorEnabled)
        //            _logger.Error("Sending TPTI for sql failure! <" + thisMethod + ">", ex);

        //        return;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (_logger.IsErrorEnabled)
        //            _logger.Error("Sending TPTI failure! <" + thisMethod + ">", ex);

        //        return;
        //    }
        //    finally
        //    {
        //        if (sqlConn != null) sqlConn.Close();
        //    }
        //}

        private void CheckChangedTables()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            sqlConn = new SqlConnection(DBPersistor.ClassParameters.DBConnectionString);
            SqlCommand sqlCmd = new SqlCommand(DBPersistor.ClassParameters.STP_SAC_COLLECTCHANGEDTABLES, sqlConn);
            sqlCmd.CommandType = CommandType.StoredProcedure;
            SqlDataReader sqlReader;
            List<string> tables = new List<string>();

            try
            {
                sqlConn.Open();
                sqlReader = sqlCmd.ExecuteReader();

                while (sqlReader.Read())
                {
                    if (!sqlReader.IsDBNull(0))
                    {
                        if ((sqlReader.GetSqlValue(0).ToString().Trim() == (DBPersistor.ClassParameters.TBL_FUNCTION_ALLOC_LIST)) &&
                                 (tables.Contains(DBPersistor.ClassParameters.TBL_SYS_CONFIG)))
                        {      // Do nothing               
                        }
                        else if ((sqlReader.GetSqlValue(0).ToString().Trim() == (DBPersistor.ClassParameters.TBL_SYS_CONFIG)) &&
                                 (tables.Contains(DBPersistor.ClassParameters.TBL_FUNCTION_ALLOC_LIST)))
                        {   // Do nothing 
                        }
                        else
                        {
                            tables.Add(sqlReader.GetSqlValue(0).ToString().Trim());
                        }

                    }
                }

                string[] tablesArray = new string[tables.Count];
                string temp;

                tablesArray = tables.ToArray();

                for (int i = 0; i < tables.Count; i++)
                {
                    temp = tablesArray[i];

                    if (temp == DBPersistor.ClassParameters.TBL_CARRIER_LOG)
                    {
                        ProduceCRAIMessage(null, false, DBPersistor.ClassParameters.STP_SAC_COLLECTCHANGEDCRAI);
                    }
                    else if (temp == (DBPersistor.ClassParameters.TBL_FALLBACK_TAG_MAPPING))
                    {
                        ProduceFBTIMessage(null, false, DBPersistor.ClassParameters.STP_SAC_COLLECTCHANGEDFBTI);
                    }
                    else if (temp == (DBPersistor.ClassParameters.TBL_FOUR_PIER_TAG_MAPPING))
                    {
                        ProduceFPTIMessage(null, false, DBPersistor.ClassParameters.STP_SAC_COLLECTCHANGEDFPTI);
                    }
                    else if ((temp == (DBPersistor.ClassParameters.TBL_FUNCTION_ALLOC_LIST)) || (temp == (DBPersistor.ClassParameters.TBL_SYS_CONFIG)))
                    {
                        ProduceAFAIMessage(null, false);
                    }
                    //else if (temp == (DBPersistor.ClassParameters.TBL_TWO_PIER_TAG_INFO))
                    //{
                    //    ProduceTPTIMessage(null, false, DBPersistor.ClassParameters.STP_SAC_COLLECTCHANGEDTPTI);
                    //}
                }
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Check Changed Tables for sql failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Check Changed Tables failure! <" + thisMethod + ">", ex);

                return;
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
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
                    _perfMonitor.AddObjectStatus("msgsFromPLCGW", _noOfMsgFromClnChannel.ToString());
                    _perfMonitor.AddObjectStatus("msgsToPLCGW", _noOfMsgToClnChannel.ToString());
                    _perfMonitor.AddObjectStatus("msgsDiscarded", _noOfDiscardedMessage.ToString());
                    _perfMonitor.AddObjectStatus("msgsGID", _noOfGID.ToString());
                    _perfMonitor.AddObjectStatus("msgsICR", _noOfICR.ToString());
                    _perfMonitor.AddObjectStatus("msgsISC", _noOfISC.ToString());
                    _perfMonitor.AddObjectStatus("msgsISE", _noOfISE.ToString());
                    _perfMonitor.AddObjectStatus("msgsIPR", _noOfIPR.ToString());
                    _perfMonitor.AddObjectStatus("msgsILT", _noOfILT.ToString());
                    _perfMonitor.AddObjectStatus("msgsITI", _noOfITI.ToString());
                    _perfMonitor.AddObjectStatus("msgsMER", _noOfMER.ToString());
                    _perfMonitor.AddObjectStatus("msgsBMAM", _noOfBMAM.ToString());
                    _perfMonitor.AddObjectStatus("msgsP1500", _noOfP1500.ToString());
                    _perfMonitor.AddObjectStatus("msgsGRNF",_noOfGRNF.ToString());
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
