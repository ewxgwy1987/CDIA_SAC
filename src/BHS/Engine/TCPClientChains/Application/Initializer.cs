#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       Initializer.cs
// Revision:      1.0 -   02 Apr 2009, By Xu Jian
// =====================================================================================
//
#endregion

using System;
using System.IO;
using System.Xml;
using PALS;
using PALS.Configure;
using PALS.Net;
using log4net;
using System.Collections;

namespace BHS.Engine.TCPClientChains.Application
{
    /// <summary>
    /// Class for centralized application initializing.
    /// </summary>
    public class Initializer: IDisposable
    {
        #region Class Fields and Properties Declaration

        private const string XMLCONFIG_LOG4NET = "log4net";

        private const string OBJECT_ID_INITIALIZER = "1";

        private const string OBJECT_ID_SESSIONMANAGER = "2";
        private const string OBJECT_ID_TCPCLIENT = "2.1";
        private const string OBJECT_ID_FRAME = "2.2";
        private const string OBJECT_ID_APPCLIENT = "2.3";
        private const string OBJECT_ID_SOL = "2.4";
        private const string OBJECT_ID_INMID = "2.5";
        private const string OBJECT_ID_ACK = "2.6";
        private const string OBJECT_ID_OUTMID = "2.7";
        private const string OBJECT_ID_SESSIONHANDLER = "2.8";
        private const string OBJECT_ID_SESSIONFORWARDER = "2.9";

        private const string OBJECT_ID_MESSAGEHANDLER = "4";

        //... Sortation Related Message ...
        private const string OBJECT_ID_GID = "4.1"; //Code: 0003, GID Generated Message
        private const string OBJECT_ID_ICR = "4.2"; //Code: 0004, Item Screened Message
        private const string OBJECT_ID_ISC = "4.3"; //Code: 0005, Item Scanned Message
        private const string OBJECT_ID_BMAM = "4.4"; //Code: 0006, Baggage Measurement Array Message
        private const string OBJECT_ID_IRD = "4.5"; //Code: 0007, Item Redirect Message
        private const string OBJECT_ID_ISE = "4.6"; //Code: 0008, Item Sort Event Message
        private const string OBJECT_ID_IPR = "4.7"; //Code: 0009, Item Proceeded Message
        private const string OBJECT_ID_ILT = "4.8"; //Code: 0010, Item Lost Message
        private const string OBJECT_ID_ITI = "4.9"; //Code: 0011, Item Tracking Information Message
       private const string OBJECT_ID_MER = "4.10"; //Code: 0012, Item Manual Encoding Request Message
        private const string OBJECT_ID_AFAI = "4.11"; //Code: 0013, Airport Code and Function Allocation Information Message
        private const string OBJECT_ID_CRAI = "4.12"; //Code: 0014, Carrier Allocation Information Message
        private const string OBJECT_ID_FBTI = "4.13"; //Code: 0015, Fallback Tag Information Message
        private const string OBJECT_ID_FPTI = "4.14"; //Code: 0016, Four Pier Tag Information Message
        private const string OBJECT_ID_1500P = "4.15"; // Code: 0017, 1500P Information Message
        //... Common Message ...
        private const string OBJECT_ID_GRNF = "4.16";   //Code: 0109, Gateway Ready Notification Message
        
        private const string OBJECT_ID_DBPERSISTOR = "5";

        // The name of current class 
        private static readonly string _className = 
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString() ;
        // Create a logger for use in this class
        private static readonly log4net.ILog _logger =
                    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Configuration files
        private FileInfo _xmlFileSetting;
        private FileInfo _xmlFileTelegram;

        // -----------------------------------------------------------------------------
        // Used to store the reference of ConfigureAndWatchHandler class object for proper release of file 
        // watchers (done by Dispose() method of Initializer class) when application is closed .
        private ConfigureAndWatchHandler _fileWatchHandler;
        //
        // Code Example: 
        // Instead of watch multiple configuration files in one ConfigureAndWatchHandler class object. Multiple 
        // ConfigureAndWatchHandler objects could be created. And each object is responsible for the watching
        // the changes of different single or multiple configuration files.
        // In this case, the multiple IConfigurationLoader objects need to be created too. Each loader object
        // is paired with one ConfigureAndWatchHandler object for loading settings and watching changes.
        //
        // private ConfigureAndWatchHandler _fileWatchHandler2;
        // private BHS.Engine.TCPClientChains.Configure.XmlSettingLoader2 _xmlLoader2;
        //
        // -----------------------------------------------------------------------------

        // -----------------------------------------------------------------------------
        // Object of class XmlSettingLoader derived from interface IConfigurationLoader for loading setting from XML file.
        private BHS.Engine.TCPClientChains.Configure.XmlSettingLoader _xmlLoader;
        //
        // Code Example: 
        // Object of class IniSettingLoader derived from interface IConfigurationLoader for loading setting from INI file.
        //
        // private BHS.Engine.TCPClientChains.Configure.IniSettingLoader _iniLoader;
        //
        // -----------------------------------------------------------------------------

        // -----------------------------------------------------------------------------
        // Declare Engine Service (TCP Client) - MessageRouter Service (TCP Server) chain classes
        // PALS.Net.Handlers classes object
        private PALS.Common.IChain _forwarder;
        // PALS.Net.Managers.SessionManager object
        private PALS.Net.Managers.SessionManager _manager;
        // PALS.Net.Filters chain classes
        private PALS.Common.IChain _outMID;
        private PALS.Common.IChain _ack;
        private PALS.Common.IChain _inMID;
        private PALS.Common.IChain _sol;
        private PALS.Common.IChain _appClient;
        private PALS.Common.IChain _frame;
        // -----------------------------------------------------------------------------


        //... Sortation Related Message ...
        private Messages.Handlers.GRNF _GRNF;
        private Messages.Handlers.GID _GID;
        private Messages.Handlers.ICR _ICR;
        private Messages.Handlers.ISC _ISC;
        private Messages.Handlers.IRD _IRD;
        private Messages.Handlers.ISE _ISE;
        private Messages.Handlers.IPR _IPR;
        private Messages.Handlers.ILT _ILT;
        private Messages.Handlers.ITI _ITI;
        private Messages.Handlers.MER _MER;
        private Messages.Handlers.AFAI _AFAI;
        private Messages.Handlers.BMAM _BMAM;
        private Messages.Handlers.CRAI _CRAI;
        private Messages.Handlers.FBTI _FBTI;
        private Messages.Handlers.FPTI _FPTI;
        private Messages.Handlers.P1500 _P1500;

        // The ClassStatus object of current class
        private PALS.Diagnostics.ClassStatus _perfMonitor;
        // The Hashtable that contains the ClassStatus object of current class 
        // and all of its instance of sub classes.
        private ArrayList _perfMonitorList;

        private Engine.TCPClientChains.DataPersistor.Database.Persistor _DBPersistor;

        private BHS.Engine.TCPClientChains.Messages.Handlers.MessageHandler _msgHandler;
        /// <summary>
        /// Get or set the BHS.Engine.TCPClientChains.Messages.Handlers.Messagehandler class object.
        /// </summary>
        public BHS.Engine.TCPClientChains.Messages.Handlers.MessageHandler MsgHandler
        {
            get { return _msgHandler; }
            set { _msgHandler = value; }
        }
        
        /// <summary>
        /// null
        /// </summary>
        public string ObjectID { get; set; }

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
        /// null
        /// </summary>
        public ArrayList PerfMonitorList
        {
            get
            {
                try
                {
                    PALS.Diagnostics.ClassStatus temp = PerfMonitor;

                    return _perfMonitorList;
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

        #endregion

        #region Class Constructor, Dispose, & Destructor

        /// <summary>
        /// Application global initizliaer for centralized performing of application initializing tasks.
        /// </summary>
        /// <param name="XMLFileSetting"></param>
        /// <param name="XMLFileTelegram"></param>
        public Initializer(string XMLFileSetting, string XMLFileTelegram)
        {
            XmlElement xmlRoot = PALS.Utilities.XMLConfig.GetConfigFileRootElement(XMLFileSetting);
            if (xmlRoot == null)
            {
                throw new Exception("Open application setting XML configuration file failure!");
            }

            ObjectID = OBJECT_ID_INITIALIZER;

            XmlElement log4netConfig = (XmlElement)PALS.Utilities.XMLConfig.GetConfigSetElement(ref xmlRoot, XMLCONFIG_LOG4NET);
            if (log4netConfig ==  null)
            {
                throw new System.Exception("There is no <" + XMLCONFIG_LOG4NET + 
                                "> settings in the XML configuration file!");
            }
            else
            {
                _xmlFileSetting = new System.IO.FileInfo(XMLFileSetting);
                _xmlFileTelegram = new System.IO.FileInfo(XMLFileTelegram);

                log4net.Config.XmlConfigurator.Configure(log4netConfig);
                _logger.Info(".");
                _logger.Info(".");
                _logger.Info(".");
                _logger.Info("[..................] <" + _className + ".Initializer()>");
                _logger.Info("[...App Starting...] <" + _className + ".Initializer()>");
                _logger.Info("[..................] <" + _className + ".Initializer()>");
            }
        }

        /// <summary>
        /// Destructer of Initializer class.
        /// </summary>
        ~Initializer()
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
                _logger.Info(".");
                _logger.Info("Class:[" + _className + "] object is being destroyed... <" + thisMethod + ">");
            }

            if (_perfMonitorList != null)
            {
                _perfMonitorList.Clear();
                _perfMonitorList = null;
            }

            if (_perfMonitor != null)
            {
                _perfMonitor.Dispose();
                _perfMonitor = null;
            }

            // -----------------------------------------------------------------------------
            // Destory TCPClient chain classes
            if (_outMID != null)
            {
                PALS.Net.Filters.Application.OutgoingMessageIdentifier outMID =
                            (PALS.Net.Filters.Application.OutgoingMessageIdentifier)_outMID;
                outMID.Dispose();
                _outMID = null;
            }

            if (_ack != null)
            {
                PALS.Net.Filters.Acknowledge.ACK ack =
                            (PALS.Net.Filters.Acknowledge.ACK)_ack;
                ack.Dispose();
                _ack = null;
            }

            if (_inMID != null)
            {
                PALS.Net.Filters.Application.IncomingMessageIdentifier inMID =
                            (PALS.Net.Filters.Application.IncomingMessageIdentifier)_inMID;
                inMID.Dispose();
                _inMID = null;
            }

            if (_sol != null)
            {
                PALS.Net.Filters.SignOfLife.SOL sol =
                            (PALS.Net.Filters.SignOfLife.SOL)_sol;
                sol.Dispose();
                _sol = null;
            }

            if (_appClient != null)
            {
                PALS.Net.Filters.Application.AppClient appCln =
                            (PALS.Net.Filters.Application.AppClient)_appClient;
                appCln.Dispose();
                _appClient = null;
            }

            if (_frame != null)
            {
                PALS.Net.Filters.Frame.Frame frm =
                            (PALS.Net.Filters.Frame.Frame)_frame;
                frm.Dispose();
                _frame = null;
            }

            if (_forwarder != null)
            {
                BHS.Engine.TCPClientChains.Messages.Handlers.SessionForwarder fwdr =
                            (BHS.Engine.TCPClientChains.Messages.Handlers.SessionForwarder)_forwarder;
                fwdr.Dispose();
                _forwarder = null;
            }

            if (_manager != null)
            {
                PALS.Net.Managers.SessionManager mgr =
                            (PALS.Net.Managers.SessionManager)_manager;
                mgr.Dispose();
                _manager = null;

                System.Threading.Thread.Sleep(200);
            }
            // -----------------------------------------------------------------------------

            // -----------------------------------------------------------------------------
            // Destory message handlers.
            if (_msgHandler != null)
            {
                _msgHandler.Dispose();
                _msgHandler = null;
            }

            if (_GRNF != null)
            {
                _GRNF.Dispose();
                _GRNF = null;
            }

            if (_GID != null)
            {
                _GID.Dispose();
                _GID = null;
            }

            if (_ICR != null)
            {
                _ICR.Dispose();
                _ICR = null;
            }

            if (_ISC != null)
            {
                _ISC.Dispose();
                _ISC = null;
            }

            //if (_IRD != null)
            //{
            //    _IRD.Dispose();
            //    _IRD = null;
            //}

            if (_ISE != null)
            {
                _ISE.Dispose();
                _ISE = null;
            }

            if (_IPR != null)
            {
                _IPR.Dispose();
                _IPR = null;
            }

            if (_ILT != null)
            {
                _ILT.Dispose();
                _ILT = null;
            }

            if (_ITI != null)
            {
                _ITI.Dispose();
                _ITI = null;
            }

            if (_MER != null)
            {
                _MER.Dispose();
                _MER = null;
            }

            if (_BMAM != null)
            {
                _BMAM.Dispose();
                _BMAM = null;
            }

            if (_P1500 != null)
            {
                _P1500.Dispose();
                _P1500 = null;
            }

            // -----------------------------------------------------------------------------

            // -----------------------------------------------------------------------------
            // Destory Database Persistor.
            if (_DBPersistor != null)
            {
                _DBPersistor.Dispose();
                _DBPersistor = null;
            }
            // -----------------------------------------------------------------------------

            // -----------------------------------------------------------------------------
            // Destory configuration file watcher.
            if (_fileWatchHandler != null) _fileWatchHandler.Dispose();
            if (_xmlLoader != null) _xmlLoader.Dispose();
            // -----------------------------------------------------------------------------

            if (disposing)
            {
                _logger.Info("Class:[" + _className + "] object has been destroyed. <" + thisMethod + ">");
                _logger.Info("[..................] <" + thisMethod + ">");
                _logger.Info("[...App Stopped....] <" + thisMethod + ">");
                _logger.Info("[..................] <" + thisMethod + ">");
            }
        }

        #endregion

        #region Class Method Declaration.

        /// <summary>
        /// Init() method of Initializer class is the place to perform the initialization
        /// tasks for current application. All initialization tasks needed to be done during
        /// the application startup time should be performed here.
        /// </summary>
        /// <returns></returns>
        public bool Init()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            try
            {
                if (_logger.IsInfoEnabled)
                    _logger.Info("Initializing application settings... <" + thisMethod + ">");

                _xmlLoader = new BHS.Engine.TCPClientChains.Configure.XmlSettingLoader();
                _xmlLoader.OnReloadSettingCompleted += new EventHandler(Handler_OnReloadSettingCompleted);

                //-----------------------------------------------------------------------------
                // Load system parameters from two configuration files (CFG_MDS2CCTVGW.xml, CFG_Telegrams.xml).
                // And also start watcher to detect the change of files.  and reload setting if change is detected.
                //
                _fileWatchHandler = PALS.Configure.AppConfigurator.ConfigureAndWatch(
                                        _xmlLoader, _xmlFileSetting, _xmlFileTelegram);
                //
                // Note: _fileWatchHandler need to be released in the Dispose() method of Initializer class.
                //-----------------------------------------------------------------------------

                #region Code Sample for loading application settings
                //-----------------------------------------------------------------------------
                // Code Example 1:
                // Load system parameters from single configuration file (CFG_MDS2CCTVGW.xml), and also start 
                // watcher to detect the change of files.  and reload setting if change is detected.
                //
                //_configFileHandler = PALS.Configure.AppConfigurator.ConfigureAndWatch(_xmlLoader, _xmlFileSetting);
                //
                // Note: _fileWatchHandler need to be released in the Dispose() method of Initializer class.
                //-----------------------------------------------------------------------------

                //-----------------------------------------------------------------------------
                // Code Example 2:
                // Load system parameters from two configuration files (CFG_MDS2CCTVGW.xml, CFG_Telegrams.xml),  
                // but no file change detection is required.
                //
                //PALS.Configure.AppConfigurator.Configure(_xmlLoader, _xmlFileSetting, _xmlFileTelegram);
                //
                //-----------------------------------------------------------------------------

                //-----------------------------------------------------------------------------
                // Code Example 3:
                // Load system parameters from single configuration file (CFG_MDS2CCTVGW.xml), but no file 
                // change detection is required.
                //
                //PALS.Configure.AppConfigurator.Configure(_xmlLoader, _xmlFileSetting);
                //
                //-----------------------------------------------------------------------------

                //-----------------------------------------------------------------------------
                // Code Example 4:
                // Load system parameters from multiple configuration file (CFG_MDS2CCTVGW.xml, CFG_Telegrams.xml). 
                // Only one file (CFG_MDS2CCTVGW.xml) need to be watched for the changes, but another one
                // does not need.
                //
                //_configFileHandler = PALS.Configure.AppConfigurator.ConfigureAndWatch(_xmlLoader, _xmlFileSetting);
                //PALS.Configure.AppConfigurator.Configure(_xmlLoader2, _xmlFileTelegram);
                //
                // Note: only _fileWatchHandler need to be released in the Dispose() method of Initializer class.
                //-----------------------------------------------------------------------------
                #endregion

                // Build TCPClient Communication Chain
                BuildTCPClientChain();

                // ------------------------------------------------------------------------------------
                // Create database Persistor object
                // ------------------------------------------------------------------------------------
                _DBPersistor = new BHS.Engine.TCPClientChains.DataPersistor.Database.Persistor(
                                _xmlLoader.Paramters_DBPersistor);
                _DBPersistor.ObjectID = OBJECT_ID_DBPERSISTOR;
                // ------------------------------------------------------------------------------------

                // ------------------------------------------------------------------------------------
                // Create Application Message Handler class ojects. 
                // Due to message handler classes object have the reference to Database.Persistor class 
                // object, hence CreateMessageHandlers() must be invoked after Database.Persistor class 
                // has been successfully instentiated.
                // ------------------------------------------------------------------------------------
                if (!CreateMessageHandlers())
                    throw new Exception("Instantiate message handlers failure!");
                // ------------------------------------------------------------------------------------
                
                // ------------------------------------------------------------------------------------
                // Create centralized message handler object. And set its references to individual message handler class objects.
                // ------------------------------------------------------------------------------------
                _msgHandler = new BHS.Engine.TCPClientChains.Messages.Handlers.MessageHandler(
                                _xmlLoader.Paramters_MsgHandler,
                                (BHS.Engine.TCPClientChains.Messages.Handlers.SessionForwarder)_forwarder);
                _msgHandler.ObjectID = OBJECT_ID_MESSAGEHANDLER;
                _msgHandler.DBPersistor = _DBPersistor;
                _msgHandler.GRNF = _GRNF;
                _msgHandler.GID = _GID;
                _msgHandler.ICR = _ICR;
                _msgHandler.ISC = _ISC;
                _msgHandler.ISE = _ISE;
                _msgHandler.IPR = _IPR;
                _msgHandler.ILT = _ILT;
                _msgHandler.ITI = _ITI;
                _msgHandler.MER = _MER;
                _msgHandler.AFAI = _AFAI;
                _msgHandler.BMAM = _BMAM;
                _msgHandler.CRAI = _CRAI;
                _msgHandler.FBTI = _FBTI;
                _msgHandler.FPTI = _FPTI;
                _msgHandler.P1500 = _P1500;
                

                // Init() method can only be invoked after Persistor and individual message handlers of MessageHandler class
                // are refered to the actual objects.
                _msgHandler.Init();
                //====================================================
                // Add in codes here for refering to other message handler objects required by other projects.
                // ...
                //====================================================

                _msgHandler.OnReceived += new EventHandler<MessageEventArgs>(MsgHandler_OnReceived);
                _msgHandler.OnConnected += new EventHandler<MessageEventArgs>(MsgHandler_OnConnected);
                _msgHandler.OnDisconnected += new EventHandler<MessageEventArgs>(MsgHandler_OnDisconnected);
                // ------------------------------------------------------------------------------------
                // MessageHandler object must be created before start session connections as below
                // ------------------------------------------------------------------------------------

                //*********************************************************************************************
                //Class status for debuging only.
                _perfMonitor = new PALS.Diagnostics.ClassStatus();
                PALS.Diagnostics.ClassStatus mgrPerfMonitor = _manager.PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref mgrPerfMonitor);
                PALS.Diagnostics.ClassStatus outMidPerfMonitor = ((PALS.Net.Filters.Application.OutgoingMessageIdentifier)_outMID).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref outMidPerfMonitor);
                PALS.Diagnostics.ClassStatus ackPerfMonitor = ((PALS.Net.Filters.Acknowledge.ACK)_ack).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref ackPerfMonitor);
                PALS.Diagnostics.ClassStatus inMidPerfMonitor = ((PALS.Net.Filters.Application.IncomingMessageIdentifier)_inMID).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref inMidPerfMonitor);
                PALS.Diagnostics.ClassStatus solPerfMonitor = ((PALS.Net.Filters.SignOfLife.SOL)_sol).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref solPerfMonitor);
                PALS.Diagnostics.ClassStatus appClientPerfMonitor = ((PALS.Net.Filters.Application.AppClient)_appClient).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref appClientPerfMonitor);
                PALS.Diagnostics.ClassStatus framePerfMonitor = ((PALS.Net.Filters.Frame.Frame)_frame).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref framePerfMonitor);

                //'#####################################################################
                //'#Important:                                                         #
                //'#Create the reference link between m_PerfMonitorList object items    #
                //'#to class itself and all individual sub classes.                    #
                //'#This reference link can only be done once, hence it is located in  #
                //'#this Init() method, instead of PerfMonitorHash() property.         #
                //'#In the PerfMonitorHash() property, only perfoemance monitoring     #
                //'counter refresh will be invoked.                                    #
                _perfMonitorList = _perfMonitor.GetAllClassStatusArray();
                _msgHandler.PerfMonitorList = _perfMonitorList;
                //'#####################################################################
                // Open underlying layer connection to open TCP connections
                if (_manager != null)
                {
                    // Open TCP Client connection right now...
                    _manager.SessionStart();
                    // Turn on auto re-connect indicator to auto re-open the connection when it is closed.
                    ((PALS.Net.Transports.TCP.TCPClientParameters)_manager.ClassParameters).IsAutoReconnected = true;
                }
                else
                    throw new Exception("SessionManager of TCPClient chain is not created!");

                //'#####################################################################
                //'# Collect Current Aiport Location Code , assign it to Common.AirportLocationCode (Global Variable) for Falback Tag verification
                _DBPersistor.CollectAiportLocationCode();
                //'#####################################################################

                if (_logger.IsInfoEnabled)
                {
                    _logger.Info("Initializing application setting is successed. <" + thisMethod + ">");
                    _logger.Info(".");
                }

                return true;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled )
                    _logger.Error("Initializing class setting is failed! <" + thisMethod + ">", ex);

                if (_manager != null) _manager.Dispose();
                if (_frame != null) ((PALS.Net.Filters.Frame.Frame)_frame).Dispose();
                if (_appClient != null) ((PALS.Net.Filters.Application.AppClient)_appClient).Dispose();
                if (_sol != null) ((PALS.Net.Filters.SignOfLife.SOL)_sol).Dispose();
                if (_inMID != null) ((PALS.Net.Filters.Application.IncomingMessageIdentifier)_inMID).Dispose();
                if (_ack != null) ((PALS.Net.Filters.Acknowledge.ACK)_ack).Dispose();
                if (_outMID != null) ((PALS.Net.Filters.Application.OutgoingMessageIdentifier)_outMID).Dispose();

                if (_GRNF != null) _GRNF.Dispose();
                if (_GID != null) _GID.Dispose();
                if (_ICR != null) _ICR.Dispose();
                if (_ISC != null) _ISC.Dispose();
                if (_ISE != null) _ISE.Dispose();
                if (_IPR != null) _IPR.Dispose();
                if (_ILT != null) _ILT.Dispose();
                if (_ITI != null) _ITI.Dispose();
                if (_MER != null) _MER.Dispose();
                if (_BMAM != null) _BMAM.Dispose();
                if (_P1500 != null) _P1500.Dispose();
                //if (_BSDI != null) _BSDI.Dispose();

                if (_msgHandler != null) _msgHandler.Dispose();
                if (_DBPersistor != null) _DBPersistor.Dispose();

                return false;
            }
        }


        /// <summary>
        /// ------------------------------------------------------------------------------------
        /// Build "Handler-Filter-Transport" chain for CCTV Engine Service application 
        /// TCPClient chain by following below sequence:
        /// ------------------------------------------------------------------------------------
        /// BHS.Engine.TCPClientChains.Messages.Handlers.SessionForwarder
        /// PALS.Net.handlers.SessionHandler
        /// PALS.Net.Filters.Application.OutgoingMessageIdentifier 
        /// PALS.Net.Filters.Acknowledge.ACK 
        /// PALS.Net.Filters.Application.IncomingMessageIdentifier 
        /// PALS.Net.Filters.SignOfLife.SOL 
        /// PALS.Net.Filters.Application.APPClient 
        /// PALS.Net.FiltersFrame.Frame
        /// PALS.Net.Transports.TCP.TCPClient
        /// ------------------------------------------------------------------------------------
        /// </summary>
        private void BuildTCPClientChain()
        {
            // Instantiate Sessionmanager class to build basice TCPClient-SessioHandler chain
            PALS.Common.IParameters paramTCPCln = _xmlLoader.Paramters_TCPClient;
            _manager = new PALS.Net.Managers.SessionManager(
                        PALS.Net.Common.TransportProtocol.TCPClient, ref paramTCPCln);
            _manager.ObjectID = OBJECT_ID_SESSIONMANAGER;
            _manager.TransportObjectID = OBJECT_ID_TCPCLIENT;
            _manager.HandlerObjectID = OBJECT_ID_SESSIONHANDLER;
            // Do not start socket auto connection process until TCPClient chain is built up.
            ((PALS.Net.Transports.TCP.TCPClientParameters)_manager.ClassParameters).IsAutoReconnected = false;

            // Instantiate Frame class
            PALS.Common.IParameters paramFrame = _xmlLoader.Paramters_Frame;
            _frame = new PALS.Net.Filters.Frame.Frame(ref paramFrame);
            ((PALS.Net.Common.AbstractProtocolChain)_frame).ObjectID = OBJECT_ID_FRAME;

            // Instantiate AppClient class
            PALS.Common.IParameters paramApp = _xmlLoader.Paramters_AppClient;
            _appClient = new PALS.Net.Filters.Application.AppClient(ref paramApp);
            ((PALS.Net.Common.AbstractProtocolChain)_appClient).ObjectID = OBJECT_ID_APPCLIENT;

            // Instantiate SOL class
            PALS.Common.IParameters paramSOL = _xmlLoader.Paramters_SOL;
            _sol = new PALS.Net.Filters.SignOfLife.SOL(ref paramSOL);
            ((PALS.Net.Common.AbstractProtocolChain)_sol).ObjectID = OBJECT_ID_SOL;

            // Instantiate Message Identifier (MID) class
            PALS.Common.IParameters paramMID = _xmlLoader.Paramters_MID;
            _inMID = new PALS.Net.Filters.Application.IncomingMessageIdentifier(ref paramMID);
            ((PALS.Net.Common.AbstractProtocolChain)_inMID).ObjectID = OBJECT_ID_INMID;
            _outMID = new PALS.Net.Filters.Application.OutgoingMessageIdentifier(ref paramMID);
            ((PALS.Net.Common.AbstractProtocolChain)_outMID).ObjectID = OBJECT_ID_OUTMID;

            // Instantiate ACK class
            PALS.Common.IParameters paramACK = _xmlLoader.Paramters_ACK;
            _ack = new PALS.Net.Filters.Acknowledge.ACK(ref paramACK);
            ((PALS.Net.Common.AbstractProtocolChain)_ack).ObjectID = OBJECT_ID_ACK;

            // Instantiate GW2InternalSessionForwarder class
            _forwarder = new BHS.Engine.TCPClientChains.Messages.Handlers.SessionForwarder(null);
            ((PALS.Net.Common.AbstractProtocolChain)_forwarder).ObjectID = OBJECT_ID_SESSIONFORWARDER;

            // Build TCPClient communication chain
            _manager.AddHandlerToLast(ref _forwarder);
            _manager.AddFilterToLast(ref _outMID);
            _manager.AddFilterToLast(ref _ack);
            _manager.AddFilterToLast(ref _inMID);
            _manager.AddFilterToLast(ref _sol);
            _manager.AddFilterToLast(ref _appClient);
            _manager.AddFilterToLast(ref _frame);
        }

        /// <summary>
        /// Event handler of ReloadSettingCompleted event fired by IConfigurationLoader interface 
        /// implemented class method LoadSettingFromConfigFile() upon the reloading setting from
        /// changed file is successfully completed. 
        /// 
        /// This event handler is to make sure the reloaded settings can be taken effective 
        /// immediately.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Handler_OnReloadSettingCompleted(object sender, EventArgs e)
        {
            // Reassign the reference of new parameter class object to GW2Engine chain classes
            ((PALS.Net.Managers.SessionManager)_manager).ClassParameters =
                    _xmlLoader.Paramters_TCPClient;
            ((PALS.Net.Filters.Frame.Frame)_frame).ClassParameters =
                    (PALS.Net.Filters.Frame.FrameParameters)_xmlLoader.Paramters_Frame;
            ((PALS.Net.Filters.Application.AppClient)_appClient).ClassParameters =
                    (PALS.Net.Filters.Application.AppClientParameters)_xmlLoader.Paramters_AppClient;
            ((PALS.Net.Filters.SignOfLife.SOL)_sol).ClassParameters =
                    (PALS.Net.Filters.SignOfLife.SOLParameters)_xmlLoader.Paramters_SOL;
            ((PALS.Net.Filters.Application.IncomingMessageIdentifier)_inMID).ClassParameters =
                    (PALS.Net.Filters.Application.MessageIdentifierParameters)_xmlLoader.Paramters_MID;
            ((PALS.Net.Filters.Acknowledge.ACK)_ack).ClassParameters =
                    (PALS.Net.Filters.Acknowledge.ACKParameters)_xmlLoader.Paramters_ACK;
            ((PALS.Net.Filters.Application.OutgoingMessageIdentifier)_outMID).ClassParameters =
                    (PALS.Net.Filters.Application.MessageIdentifierParameters)_xmlLoader.Paramters_MID;

            // Reassign the reference of new parameter class object to MessageHandler class
            ((BHS.Engine.TCPClientChains.Messages.Handlers.MessageHandler)_msgHandler).ClassParameters =
                    (BHS.Engine.TCPClientChains.Messages.Handlers.MessageHandlerParameters)_xmlLoader.Paramters_MsgHandler;
            // Reassign the reference of new parameter class object to Persistor class
            ((BHS.Engine.TCPClientChains.DataPersistor.Database.Persistor)_DBPersistor).ClassParameters =
                    (BHS.Engine.TCPClientChains.DataPersistor.Database.PersistorParameters)_xmlLoader.Paramters_DBPersistor;
        }

        private void MsgHandler_OnReceived(object sender, MessageEventArgs e)
        {
            // Copy to a temporary variable to be thread-safe.
            EventHandler<MessageEventArgs> temp = OnReceived;
            // Event could be null if there are no subscribers, so check it before raise event
            if (temp != null)
                // Raise OnReceived event upon message is received.
                temp(this, e);
        }

        private void MsgHandler_OnConnected(object sender, MessageEventArgs e)
        {
            // Copy to a temporary variable to be thread-safe.
            EventHandler<MessageEventArgs> temp = OnConnected;
            // Event could be null if there are no subscribers, so check it before raise event
            if (temp != null)
                // Raise OnConnected event upon channel connection is opened.
                temp(this, e);
        }

        private void MsgHandler_OnDisconnected(object sender, MessageEventArgs e)
        {
            // Copy to a temporary variable to be thread-safe.
            EventHandler<MessageEventArgs> temp = OnDisconnected;
            // Event could be null if there are no subscribers, so check it before raise event
            if (temp != null)
                // Raise OnDisconnected event upon channel connection is closed.
                temp(this, e);
        }

        private bool CreateMessageHandlers()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            try
            {
                // CSNF message handler
                _GRNF = new BHS.Engine.TCPClientChains.Messages.Handlers.GRNF();
                _GRNF.ObjectID = OBJECT_ID_GRNF;
                _GRNF.DBPersistor = _DBPersistor;
                _GRNF.MessageFormat = ((Messages.Handlers.MessageHandlerParameters)
                                        _xmlLoader.Paramters_MsgHandler).MessageFormat_GRNF;
                //_GRNF.MessageFormat_MAAV = ((Messages.Handlers.MessageHandlerParameters)
                //                        _xmlLoader.Paramters_MsgHandler).MessageFormat_MAAV;

                //====================================================
                // GID message handler
                _GID = new BHS.Engine.TCPClientChains.Messages.Handlers.GID();
                _GID.ObjectID = OBJECT_ID_GID;
                _GID.DBPersistor = _DBPersistor;
                _GID.MessageFormat = ((Messages.Handlers.MessageHandlerParameters)
                                        _xmlLoader.Paramters_MsgHandler).MessageFormat_GID;

                // ICR message handler
                _ICR = new BHS.Engine.TCPClientChains.Messages.Handlers.ICR();
                _ICR.ObjectID = OBJECT_ID_ICR;
                _ICR.DBPersistor = _DBPersistor;
                _ICR.MessageFormat = ((Messages.Handlers.MessageHandlerParameters)
                                        _xmlLoader.Paramters_MsgHandler).MessageFormat_ICR;
                
                // ISC message handler
                _ISC = new BHS.Engine.TCPClientChains.Messages.Handlers.ISC();
                _ISC.ObjectID = OBJECT_ID_ISC;
                _ISC.DBPersistor = _DBPersistor;
                _ISC.MessageFormat = ((Messages.Handlers.MessageHandlerParameters)
                                        _xmlLoader.Paramters_MsgHandler).MessageFormat_ISC;
                _ISC.MessageFormat_DLPS = ((Messages.Handlers.MessageHandlerParameters)
                                        _xmlLoader.Paramters_MsgHandler).MessageFormat_DLPS;

                // ISE message handler
                _ISE = new BHS.Engine.TCPClientChains.Messages.Handlers.ISE();
                _ISE.ObjectID = OBJECT_ID_ISE;
                _ISE.DBPersistor = _DBPersistor;
                _ISE.MessageFormat = ((Messages.Handlers.MessageHandlerParameters)
                                        _xmlLoader.Paramters_MsgHandler).MessageFormat_ISE;

                // IPR message handler
                _IPR = new BHS.Engine.TCPClientChains.Messages.Handlers.IPR();
                _IPR.ObjectID = OBJECT_ID_IPR;
                _IPR.DBPersistor = _DBPersistor;
                _IPR.MessageFormat = ((Messages.Handlers.MessageHandlerParameters)
                                        _xmlLoader.Paramters_MsgHandler).MessageFormat_IPR;

                // ILT message handler
                _ILT = new BHS.Engine.TCPClientChains.Messages.Handlers.ILT();
                _ILT.ObjectID = OBJECT_ID_ILT;
                _ILT.DBPersistor = _DBPersistor;
                _ILT.MessageFormat = ((Messages.Handlers.MessageHandlerParameters)
                                        _xmlLoader.Paramters_MsgHandler).MessageFormat_ILT;

                // ITI message handler
                _ITI = new BHS.Engine.TCPClientChains.Messages.Handlers.ITI();
                _ITI.ObjectID = OBJECT_ID_ITI;
                _ITI.DBPersistor = _DBPersistor;
                _ITI.MessageFormat = ((Messages.Handlers.MessageHandlerParameters)
                                        _xmlLoader.Paramters_MsgHandler).MessageFormat_ITI;

                // MER message handler
                _MER = new BHS.Engine.TCPClientChains.Messages.Handlers.MER();
                _MER.ObjectID = OBJECT_ID_MER;
                _MER.DBPersistor = _DBPersistor;
                _MER.MessageFormat = ((Messages.Handlers.MessageHandlerParameters)
                                        _xmlLoader.Paramters_MsgHandler).MessageFormat_MER;

                //// AFAI message handler
                //_AFAI = new BHS.Engine.TCPClientChains.Messages.Handlers.AFAI();
                //_AFAI.ObjectID = OBJECT_ID_AFAI;
                //_AFAI.DBPersistor = _DBPersistor;
                //_AFAI.MessageFormat = ((Messages.Handlers.MessageHandlerParameters)
                //                        _xmlLoader.Paramters_MsgHandler).MessageFormat_AFAI;

                // BMAM message handler
                _BMAM = new BHS.Engine.TCPClientChains.Messages.Handlers.BMAM();
                _BMAM.ObjectID = OBJECT_ID_BMAM;
                _BMAM.DBPersistor = _DBPersistor;
                _BMAM.MessageFormat = ((Messages.Handlers.MessageHandlerParameters)
                                        _xmlLoader.Paramters_MsgHandler).MessageFormat_BMAM;

                //// CRAI message handler
                //_CRAI = new BHS.Engine.TCPClientChains.Messages.Handlers.CRAI();
                //_CRAI.ObjectID = OBJECT_ID_CRAI;
                //_CRAI.DBPersistor = _DBPersistor;
                //_CRAI.MessageFormat = ((Messages.Handlers.MessageHandlerParameters)
                //                        _xmlLoader.Paramters_MsgHandler).MessageFormat_CRAI;

                //// FBTI message handler
                //_FBTI = new BHS.Engine.TCPClientChains.Messages.Handlers.FBTI();
                //_FBTI.ObjectID = OBJECT_ID_FBTI;
                //_FBTI.DBPersistor = _DBPersistor;
                //_FBTI.MessageFormat = ((Messages.Handlers.MessageHandlerParameters)
                //                        _xmlLoader.Paramters_MsgHandler).MessageFormat_FBTI;

                //// FPTI message handler
                //_FPTI = new BHS.Engine.TCPClientChains.Messages.Handlers.FPTI();
                //_FPTI.ObjectID = OBJECT_ID_FPTI;
                //_FPTI.DBPersistor = _DBPersistor;
                //_FPTI.MessageFormat = ((Messages.Handlers.MessageHandlerParameters)
                //                        _xmlLoader.Paramters_MsgHandler).MessageFormat_FPTI;

                //// TPTI message handler
                //_TPTI = new BHS.Engine.TCPClientChains.Messages.Handlers.TPTI();
                //_TPTI.ObjectID = OBJECT_ID_TPTI;
                //_TPTI.DBPersistor = _DBPersistor;
                //_TPTI.MessageFormat = ((Messages.Handlers.MessageHandlerParameters)
                //                        _xmlLoader.Paramters_MsgHandler).MessageFormat_TPTI;

                // PV1K message handler
                _P1500 = new BHS.Engine.TCPClientChains.Messages.Handlers.P1500();
                _P1500.ObjectID = OBJECT_ID_1500P;
                _P1500.DBPersistor = _DBPersistor;
                _P1500.MessageFormat = ((Messages.Handlers.MessageHandlerParameters)
                                        _xmlLoader.Paramters_MsgHandler).MessageFormat_P1500;

                // BSDI message handler
                //_BSDI = new BHS.Engine.TCPClientChains.Messages.Handlers.BSDI();
                //_BSDI.ObjectID = OBJECT_ID_BSDI;
                //_BSDI.DBPersistor = _DBPersistor;
                //_BSDI.MessageFormat = ((Messages.Handlers.MessageHandlerParameters)
                //                        _xmlLoader.Paramters_MsgHandler).MessageFormat_BSDI;

                // IRD message handler
                //_IRD = new BHS.Engine.TCPClientChains.Messages.Handlers.IRD();
                //_IRD.ObjectID = OBJECT_ID_IRD;
                //_IRD.DBPersistor = _DBPersistor;
                //_IRD.MessageFormat = ((Messages.Handlers.MessageHandlerParameters)
                //                                 _xmlLoader.Paramters_MsgHandler).MessageFormat_IRD;
                //====================================================

                return true;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("System Exception! <" + thisMethod + ">", ex);

                return false;
            }
        }

        //'<object id="1">
        //'	<class>BHS.SAC2PLCGW.Application.Initializer</class>
        //'	<service>SAC2PLC_Gateway1</service>
        //'	<serviceStartedTime>20/12/2005 15:30:50</serviceStartedTime>
        //'	<company>Inter-Roller</company>
        //'	<department>CSI</department>
        //'	<author>XuJian</author>
        //'</object>
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
                    _perfMonitor.CloseObjectNode();
                }

                // Refresh all other class object status counters.
                PALS.Diagnostics.ClassStatus temp;
                temp = _manager.PerfMonitor;
                temp = ((PALS.Net.Filters.Frame.Frame)_frame).PerfMonitor;
                temp = ((PALS.Net.Filters.Application.AppClient)_appClient).PerfMonitor;
                temp = ((PALS.Net.Filters.SignOfLife.SOL)_sol).PerfMonitor;
                temp = ((PALS.Net.Filters.Application.IncomingMessageIdentifier)_inMID).PerfMonitor;
                temp = ((PALS.Net.Filters.Acknowledge.ACK)_ack).PerfMonitor;
                temp = ((PALS.Net.Filters.Application.OutgoingMessageIdentifier)_outMID).PerfMonitor;

                temp = _msgHandler.PerfMonitor;
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
