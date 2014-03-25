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

namespace BHS.Gateway.TCPClientTCPClientChains.Application
{
    /// <summary>
    /// Class for centralized application initializing.
    /// </summary>
    public class Initializer: IDisposable
    {
        #region Class Fields and Properties Declaration

        private const string XMLCONFIG_LOG4NET = "log4net";

        private const string OBJECT_ID_INITIALIZER = "1";

        private const string OBJECT_ID_GW2INTERNAL_SESSIONMANAGER = "2";
        private const string OBJECT_ID_GW2INTERNAL_TCPCLIENT = "2.1";
        private const string OBJECT_ID_GW2INTERNAL_FRAME = "2.2";
        private const string OBJECT_ID_GW2INTERNAL_APPCLIENT = "2.3";
        private const string OBJECT_ID_GW2INTERNAL_SOL = "2.4";
        private const string OBJECT_ID_GW2INTERNAL_INMID = "2.5";
        private const string OBJECT_ID_GW2INTERNAL_ACK = "2.6";
        private const string OBJECT_ID_GW2INTERNAL_OUTMID = "2.7";
        private const string OBJECT_ID_GW2INTERNAL_SESSIONHANDLER = "2.8";
        private const string OBJECT_ID_GW2INTERNAL_SESSIONFORWARDER = "2.9";

        private const string OBJECT_ID_GW2EXTERNAL_SESSIONMANAGER = "3";
        private const string OBJECT_ID_GW2EXTERNAL_TCPSERVERCLIENT = "3.1";
        private const string OBJECT_ID_GW2EXTERNAL_EIP = "3.2";
        private const string OBJECT_ID_GW2EXTERNAL_CIP = "3.3";
        private const string OBJECT_ID_GW2EXTERNAL_APPCLIENT = "3.4";
        private const string OBJECT_ID_GW2EXTERNAL_SOL = "3.5";
        private const string OBJECT_ID_GW2EXTERNAL_TSYN = "3.6";
        private const string OBJECT_ID_GW2EXTERNAL_INMID = "3.7";
        private const string OBJECT_ID_GW2EXTERNAL_ACK = "3.8";
        private const string OBJECT_ID_GW2EXTERNAL_OUTMID = "3.9";
        private const string OBJECT_ID_GW2EXTERNAL_SESSIONHANDLER = "3.10";
        private const string OBJECT_ID_GW2EXTERNAL_SESSIONFORWARDER = "3.11";

        private const string OBJECT_ID_MESSAGEHANDLER = "4";

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
        // private BHS.Gateway.TCPClientTCPClientChains.Configure.XmlSettingLoader2 _xmlLoader2;
        //
        // -----------------------------------------------------------------------------

        // -----------------------------------------------------------------------------
        // Object of class XmlSettingLoader derived from interface IConfigurationLoader for loading setting from XML file.
        private BHS.Gateway.TCPClientTCPClientChains.Configure.XmlSettingLoader _xmlLoader;
        //
        // Code Example: 
        // Object of class IniSettingLoader derived from interface IConfigurationLoader for loading setting from INI file.
        //
        // private BHS.Gateway.TCPClientTCPClientChains.Configure.IniSettingLoader _iniLoader;
        //
        // -----------------------------------------------------------------------------

        // -----------------------------------------------------------------------------
        // Declare Gateway Service (TCP Client) - Internal Engine Service application (TCP Server) chain classes
        // IREL.Net.Handlers classes object
        private PALS.Common.IChain _forwarderGW2Internal;
        // PALS.Net.Managers.SessionManager object
        private PALS.Net.Managers.SessionManager _managerGW2Internal;
        // PALS.Net.Filters chain classes
        private PALS.Common.IChain _outMIDGW2Internal;
        private PALS.Common.IChain _ackGW2Internal;
        private PALS.Common.IChain _inMIDGW2Internal;
        private PALS.Common.IChain _solGW2Internal;
        private PALS.Common.IChain _appClientGW2Internal;
        private PALS.Common.IChain _frameGW2Internal;
        // -----------------------------------------------------------------------------

        // -----------------------------------------------------------------------------
        // Declare Gateway Service (TCP Client) - External device (TCP Server) chain classes
        // IREL.Net.Handlers classes object
        private PALS.Common.IChain _forwarderGW2External;
        // PALS.Net.Managers.SessionManager object
        private PALS.Net.Managers.SessionManager _managerGW2External;
        // PALS.Net.Filters chain classes
        private PALS.Common.IChain _outMIDGW2External;
        private PALS.Common.IChain _ackGW2External;
        private PALS.Common.IChain _inMIDGW2External;
        private PALS.Common.IChain _solGW2External;
        private PALS.Common.IChain _tsynGW2External;
        private PALS.Common.IChain _appClientGW2External;
        private PALS.Common.IChain _cipGW2External;
        private PALS.Common.IChain _eipGW2External;

        // -----------------------------------------------------------------------------

        private BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.MessageHandler _msgHandler;

        
        // The ClassStatus object of current class
        private PALS.Diagnostics.ClassStatus _perfMonitor;
        // The Hashtable that contains the ClassStatus object of current class 
        // and all of its instance of sub classes.
        private ArrayList _perfMonitorList;

        /// <summary>
        /// Get or set the BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.Messagehandler class object.
        /// </summary>
        public BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.MessageHandler MsgHandler
        {
            get { return _msgHandler; }
            set { _msgHandler = value; }
        }
        
        /// <summary>
        /// null
        /// </summary>
        public string ObjectID { get; set; }

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

            if(_perfMonitorList !=null)
            {
                _perfMonitorList.Clear();
                _perfMonitorList = null;
            }

            if(_perfMonitor!=null)
            {
                _perfMonitor.Dispose();
                _perfMonitor = null;
            }

            // -----------------------------------------------------------------------------
            // Destory GW2Internal chain classes
            if (_outMIDGW2Internal != null)
            {
                PALS.Net.Filters.Application.OutgoingMessageIdentifier outMID =
                            (PALS.Net.Filters.Application.OutgoingMessageIdentifier)_outMIDGW2Internal;
                outMID.Dispose();
                _outMIDGW2Internal = null;
            }

            if (_ackGW2Internal != null)
            {
                PALS.Net.Filters.Acknowledge.ACK ack =
                            (PALS.Net.Filters.Acknowledge.ACK)_ackGW2Internal;
                ack.Dispose();
                _ackGW2Internal = null;
            }

            if (_inMIDGW2Internal != null)
            {
                PALS.Net.Filters.Application.IncomingMessageIdentifier inMID =
                            (PALS.Net.Filters.Application.IncomingMessageIdentifier)_inMIDGW2Internal;
                inMID.Dispose();
                _inMIDGW2Internal = null;
            }

            if (_solGW2Internal != null)
            {
                PALS.Net.Filters.SignOfLife.SOL sol =
                            (PALS.Net.Filters.SignOfLife.SOL)_solGW2Internal;
                sol.Dispose();
                _solGW2Internal = null;
            }

            if (_appClientGW2Internal != null)
            {
                PALS.Net.Filters.Application.AppClient appCln =
                            (PALS.Net.Filters.Application.AppClient)_appClientGW2Internal;
                appCln.Dispose();
                _appClientGW2Internal = null;
            }

            if (_frameGW2Internal != null)
            {
                PALS.Net.Filters.Frame.Frame frm =
                            (PALS.Net.Filters.Frame.Frame)_frameGW2Internal;
                frm.Dispose();
                _frameGW2Internal = null;
            }

            if (_forwarderGW2Internal != null)
            {
                BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.GW2InternalSessionForwarder fwdr =
                            (BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.GW2InternalSessionForwarder)_forwarderGW2Internal;
                fwdr.Dispose();
                _forwarderGW2Internal = null;
            }

            if (_managerGW2Internal != null)
            {
                PALS.Net.Managers.SessionManager mgr =
                            (PALS.Net.Managers.SessionManager)_managerGW2Internal;
                mgr.Dispose();
                _managerGW2Internal = null;

                System.Threading.Thread.Sleep(200);
            }
            // -----------------------------------------------------------------------------

            // -----------------------------------------------------------------------------
            // Destory GW2External chain classes
            if (_outMIDGW2External != null)
            {
                PALS.Net.Filters.Application.OutgoingMessageIdentifier outMID =
                            (PALS.Net.Filters.Application.OutgoingMessageIdentifier)_outMIDGW2External;
                outMID.Dispose();
                _outMIDGW2External = null;
            }

            if (_ackGW2External != null)
            {
                PALS.Net.Filters.Acknowledge.ACK ack =
                            (PALS.Net.Filters.Acknowledge.ACK)_ackGW2External;
                ack.Dispose();
                _ackGW2External = null;
            }

            if (_inMIDGW2External != null)
            {
                PALS.Net.Filters.Application.IncomingMessageIdentifier inMID =
                            (PALS.Net.Filters.Application.IncomingMessageIdentifier)_inMIDGW2External;
                inMID.Dispose();
                _inMIDGW2External = null;
            }

            if (_tsynGW2External != null)
            {
                PALS.Net.Filters.TimeSynchronizing.TimeSync tsyn =
                            (PALS.Net.Filters.TimeSynchronizing.TimeSync)_tsynGW2External;
                tsyn.Dispose();
                _tsynGW2External = null;
            }

            if (_solGW2External != null)
            {
                PALS.Net.Filters.SignOfLife.SOL sol =
                            (PALS.Net.Filters.SignOfLife.SOL)_solGW2External;
                sol.Dispose();
                _solGW2External = null;
            }

            if (_appClientGW2External != null)
            {
                PALS.Net.Filters.Application.AppClient appCln =
                            (PALS.Net.Filters.Application.AppClient)_appClientGW2External;
                appCln.Dispose();
                _appClientGW2External = null;
            }

            if (_cipGW2External != null)
            {
                PALS.Net.Filters.EIPCIP.CIP cip =
                            (PALS.Net.Filters.EIPCIP.CIP)_cipGW2External;
                cip.Dispose();
                _cipGW2External = null;
            }

            if (_eipGW2External != null)
            {
                PALS.Net.Filters.EIPCIP.EIP eip =
                            (PALS.Net.Filters.EIPCIP.EIP)_eipGW2External;
                eip.Dispose();
                _eipGW2External = null;
            }

            if (_forwarderGW2External != null)
            {
                BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.GW2ExternalSessionForwarder fwdr =
                            (BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.GW2ExternalSessionForwarder)_forwarderGW2External;
                fwdr.Dispose();
                _forwarderGW2External = null;
            }

            if (_managerGW2External != null)
            {
                PALS.Net.Managers.SessionManager mgr =
                            (PALS.Net.Managers.SessionManager)_managerGW2External;
                mgr.Dispose();
                _managerGW2External = null;

                System.Threading.Thread.Sleep(200);
            }
            // -----------------------------------------------------------------------------

            // Destory centralized message handler.
            if (_msgHandler != null)
            {
                _msgHandler.Dispose();
                _msgHandler = null;
            }

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

                _xmlLoader = new BHS.Gateway.TCPClientTCPClientChains.Configure.XmlSettingLoader();
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

                // Build GW2Engine TCPClient Communication Chain
                BuildGW2InternalTCPClientChain();
                // Build GW2PLC TCPClient Communication Chain
                BuildGW2ExternalTCPClientChain();

                // ------------------------------------------------------------------------------------
                // Create centralized message handler object.
                // ------------------------------------------------------------------------------------
                _msgHandler = new BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.MessageHandler(
                                _xmlLoader.Paramters_MsgHandler,
                                (BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.GW2InternalSessionForwarder)_forwarderGW2Internal,
                                (BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.GW2ExternalSessionForwarder)_forwarderGW2External);
                _msgHandler.ObjectID = OBJECT_ID_MESSAGEHANDLER;
                // Keep the reference of parameter object for GW2External channel TCPClient class objects, 
                // so that MessageHandler can use this reference to start the opening of connection to 
                // external device upon GW2Internal connection is opened.
                //**********************************_msgHandler.TCPClientParames = (PALS.Net.Transports.TCP.TCPClientParameters)_managerGW2External.ClassParameters;
                // Add event handler to MessageHandler class object.
                _msgHandler.OnReceived += new EventHandler<MessageEventArgs>(MsgHandler_OnReceived);
                _msgHandler.OnConnected += new EventHandler<MessageEventArgs>(MsgHandler_OnConnected);
                _msgHandler.OnDisconnected += new EventHandler<MessageEventArgs>(MsgHandler_OnDisconnected);

                
                //*********************************************************************************************
                //Class status for debuging only.
                _perfMonitor = new PALS.Diagnostics.ClassStatus();
                PALS.Diagnostics.ClassStatus mgrExPerfMonitor = _managerGW2External.PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref mgrExPerfMonitor);
                PALS.Diagnostics.ClassStatus outMidExPerfMonitor = ((PALS.Net.Filters.Application.OutgoingMessageIdentifier)_outMIDGW2External).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref outMidExPerfMonitor);
                PALS.Diagnostics.ClassStatus ackExPerfMonitor = ((PALS.Net.Filters.Acknowledge.ACK)_ackGW2External).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref ackExPerfMonitor);
                PALS.Diagnostics.ClassStatus inMidExPerfMonitor =((PALS.Net.Filters.Application.IncomingMessageIdentifier)_inMIDGW2External).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref inMidExPerfMonitor);
                PALS.Diagnostics.ClassStatus tsynExPerfMonitor =((PALS.Net.Filters.TimeSynchronizing.TimeSync)_tsynGW2External).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref tsynExPerfMonitor);
                PALS.Diagnostics.ClassStatus solExPerfMonitor = ((PALS.Net.Filters.SignOfLife.SOL)_solGW2External).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref solExPerfMonitor);
                PALS.Diagnostics.ClassStatus appClientExPerfMonitor = ((PALS.Net.Filters.Application.AppClient)_appClientGW2External).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref appClientExPerfMonitor);
                PALS.Diagnostics.ClassStatus cipExPerfMonitor = ((PALS.Net.Filters.EIPCIP.CIP)_cipGW2External).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref cipExPerfMonitor);
                PALS.Diagnostics.ClassStatus eipExPerfMonitor = ((PALS.Net.Filters.EIPCIP.EIP)_eipGW2External).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref eipExPerfMonitor);


                PALS.Diagnostics.ClassStatus mgrInPerfMonitor = _managerGW2Internal.PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref mgrInPerfMonitor);
                PALS.Diagnostics.ClassStatus outMidinPerfMonitor = ((PALS.Net.Filters.Application.OutgoingMessageIdentifier)_outMIDGW2Internal).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref outMidinPerfMonitor);
                PALS.Diagnostics.ClassStatus ackInPerfMonitor = ((PALS.Net.Filters.Acknowledge.ACK)_ackGW2Internal).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref ackInPerfMonitor);
                PALS.Diagnostics.ClassStatus inMidInPerfMonitor = ((PALS.Net.Filters.Application.IncomingMessageIdentifier)_inMIDGW2Internal).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref inMidInPerfMonitor);
                PALS.Diagnostics.ClassStatus solInPerfMonitor = ((PALS.Net.Filters.SignOfLife.SOL)_solGW2Internal).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref solInPerfMonitor);
                PALS.Diagnostics.ClassStatus appClientInPerfMonitor = ((PALS.Net.Filters.Application.AppClient)_appClientGW2Internal).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref appClientInPerfMonitor);
                PALS.Diagnostics.ClassStatus frameInPerfMonitor = ((PALS.Net.Filters.Frame.Frame)_frameGW2Internal).PerfMonitor;
                _perfMonitor.AddSubClassStatus(ref frameInPerfMonitor);         
         
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

                //                
                // ------------------------------------------------------------------------------------
                // MessageHandler object must be created before start session connections as below
                // ------------------------------------------------------------------------------------
                // Open underlying layer connection to open TCP connections
                if (_managerGW2Internal != null)
                    _managerGW2Internal.SessionStart();
                else
                    throw new Exception("SessionManager of GW2Internal chain is not created!");

                // ------------------------------------------------------------------------------------
                // GW2External chain connection will be started automatically upon GW2Internal chain connection
                // is opened. Hence, it is not started here.
                // ------------------------------------------------------------------------------------
                //if (_managerGW2External != null)
                //    _managerGW2External.SessionStart();
                //else
                //    throw new Exception("SessionManager of GW2External chain is not created!");
                // ------------------------------------------------------------------------------------

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
                    _logger.Error("Initializing application setting is failed! <" + thisMethod + ">", ex);

                return false;
            }
        }


        /// <summary>
        /// ------------------------------------------------------------------------------------
        /// Build "Handler-Filter-Transport" chain for GW2Internal Engine Service application 
        /// TCPClient chain by following below sequence:
        /// ------------------------------------------------------------------------------------
        /// BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.GW2InternalSessionForwarder
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
        private void BuildGW2InternalTCPClientChain()
        {
            // Instantiate Sessionmanager class to build basice TCPClient-SessioHandler chain
            PALS.Common.IParameters paramTCPCln = _xmlLoader.Paramters_TCPClient_GW2Internal;
            _managerGW2Internal = new PALS.Net.Managers.SessionManager(
                        PALS.Net.Common.TransportProtocol.TCPClient, ref paramTCPCln);
            _managerGW2Internal.ObjectID = OBJECT_ID_GW2INTERNAL_SESSIONMANAGER;
            _managerGW2Internal.TransportObjectID = OBJECT_ID_GW2INTERNAL_TCPCLIENT;
            _managerGW2Internal.HandlerObjectID = OBJECT_ID_GW2INTERNAL_SESSIONHANDLER;

            // Instantiate Frame class
            PALS.Common.IParameters paramFrame = _xmlLoader.Paramters_Frame;
            _frameGW2Internal = new PALS.Net.Filters.Frame.Frame(ref paramFrame);
            ((PALS.Net.Common.AbstractProtocolChain)_frameGW2Internal).ObjectID = OBJECT_ID_GW2INTERNAL_FRAME;

            // Instantiate AppClient class
            PALS.Common.IParameters paramApp = _xmlLoader.Paramters_AppClient_GW2Internal;
            _appClientGW2Internal = new PALS.Net.Filters.Application.AppClient(ref paramApp);
            ((PALS.Net.Common.AbstractProtocolChain)_appClientGW2Internal).ObjectID = OBJECT_ID_GW2INTERNAL_APPCLIENT;

            // Instantiate SOL class
            PALS.Common.IParameters paramSOL = _xmlLoader.Paramters_SOL_GW2Internal;
            _solGW2Internal = new PALS.Net.Filters.SignOfLife.SOL(ref paramSOL);
            ((PALS.Net.Common.AbstractProtocolChain)_solGW2Internal).ObjectID = OBJECT_ID_GW2INTERNAL_SOL;

            // Instantiate Message Identifier (MID) class
            PALS.Common.IParameters paramMID = _xmlLoader.Paramters_MID;
            _inMIDGW2Internal = new PALS.Net.Filters.Application.IncomingMessageIdentifier(ref paramMID);
            ((PALS.Net.Common.AbstractProtocolChain)_inMIDGW2Internal).ObjectID = OBJECT_ID_GW2INTERNAL_INMID;
            _outMIDGW2Internal = new PALS.Net.Filters.Application.OutgoingMessageIdentifier(ref paramMID);
            ((PALS.Net.Common.AbstractProtocolChain)_outMIDGW2Internal).ObjectID = OBJECT_ID_GW2INTERNAL_OUTMID;

            // Instantiate ACK class
            PALS.Common.IParameters paramACK = _xmlLoader.Paramters_ACK;
            _ackGW2Internal = new PALS.Net.Filters.Acknowledge.ACK(ref paramACK);
            ((PALS.Net.Common.AbstractProtocolChain)_ackGW2Internal).ObjectID = OBJECT_ID_GW2INTERNAL_ACK;

            // Instantiate GW2InternalSessionForwarder class
            _forwarderGW2Internal = new BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.GW2InternalSessionForwarder(null);
            ((PALS.Net.Common.AbstractProtocolChain)_forwarderGW2Internal).ObjectID = OBJECT_ID_GW2INTERNAL_SESSIONFORWARDER;

            // Build GW2Internal communication chain
            _managerGW2Internal.AddHandlerToLast(ref _forwarderGW2Internal);
            _managerGW2Internal.AddFilterToLast(ref _outMIDGW2Internal);
            _managerGW2Internal.AddFilterToLast(ref _ackGW2Internal);
            _managerGW2Internal.AddFilterToLast(ref _inMIDGW2Internal);
            _managerGW2Internal.AddFilterToLast(ref _solGW2Internal);
            _managerGW2Internal.AddFilterToLast(ref _appClientGW2Internal);
            _managerGW2Internal.AddFilterToLast(ref _frameGW2Internal);
        }

        /// <summary>
        /// ------------------------------------------------------------------------------------
        /// Build "Handler-Filter-Transport" chain for GW2External device TCPClient chain by following below sequence:
        /// ------------------------------------------------------------------------------------
        /// BHS.Gateway.Messages.Handlers.GW2ExternalSessionForwarder
        /// PALS.Net.handlers.SessionHandler
        /// PALS.Net.Filters.Application.OutgoingMessageIdentifier 
        /// PALS.Net.Filters.Acknowledge.ACK 
        /// PALS.Net.Filters.Application.IncomingMessageIdentifier 
        /// PALS.Net.Filters.SignOfLife.SOL 
        /// PALS.Net.Filters.Application.APPClient 
        /// PALS.Net.Filters.EIPCIP.CIP
        /// PALS.Net.Filters.EIPCIP.EIP
        /// PALS.Net.Transports.TCP.TCPClient
        /// ------------------------------------------------------------------------------------
        /// </summary>
        private void BuildGW2ExternalTCPClientChain()
        {
            // Instantiate Sessionmanager class to build basice TCPClient-SessioHandler chain
            PALS.Common.IParameters paramTCPSvrCln = _xmlLoader.Paramters_TCPServerClient_GW2External;
            _managerGW2External = new PALS.Net.Managers.SessionManager(
                        PALS.Net.Common.TransportProtocol.TCPServerClient, ref paramTCPSvrCln);
            _managerGW2External.ObjectID = OBJECT_ID_GW2EXTERNAL_SESSIONMANAGER;
            _managerGW2External.TransportObjectID = OBJECT_ID_GW2EXTERNAL_TCPSERVERCLIENT;
            _managerGW2External.HandlerObjectID = OBJECT_ID_GW2EXTERNAL_SESSIONHANDLER;
            // Opening GW2External device connection shall not be started automatically. 
            // It will be started only after the GW2Internal Engine service connection is opened.
            //((PALS.Net.Transports.TCP.TCPServerClientParameters)_managerGW2External.ClassParameters).IsAutoReconnected = false;

            // Instantiate EIP class
            PALS.Common.IParameters paramEIP = _xmlLoader.Paramters_EIP;
            _eipGW2External = new PALS.Net.Filters.EIPCIP.EIP(ref paramEIP);
            ((PALS.Net.Common.AbstractProtocolChain)_eipGW2External).ObjectID = OBJECT_ID_GW2EXTERNAL_EIP;

            // Instantiate CIP class
            PALS.Common.IParameters paramCIP = _xmlLoader.Paramters_CIP;
            _cipGW2External = new PALS.Net.Filters.EIPCIP.CIP(ref paramCIP);
            ((PALS.Net.Common.AbstractProtocolChain)_cipGW2External).ObjectID = OBJECT_ID_GW2EXTERNAL_CIP;

            // Instantiate AppClient class
            PALS.Common.IParameters paramApp = _xmlLoader.Paramters_AppClient_GW2External;
            _appClientGW2External = new PALS.Net.Filters.Application.AppClient(ref paramApp);
            ((PALS.Net.Common.AbstractProtocolChain)_appClientGW2External).ObjectID = OBJECT_ID_GW2EXTERNAL_APPCLIENT;

            // Instantiate SOL class
            PALS.Common.IParameters paramSOL = _xmlLoader.Paramters_SOL_GW2External;
            _solGW2External = new PALS.Net.Filters.SignOfLife.SOL(ref paramSOL);
            ((PALS.Net.Common.AbstractProtocolChain)_solGW2External).ObjectID = OBJECT_ID_GW2EXTERNAL_SOL;

            // Instantiate TSYN class
            PALS.Common.IParameters paramTSYN = _xmlLoader.Paramters_TSYN;
            _tsynGW2External = new PALS.Net.Filters.TimeSynchronizing.TimeSync(ref paramTSYN);
            ((PALS.Net.Common.AbstractProtocolChain)_tsynGW2External).ObjectID = OBJECT_ID_GW2EXTERNAL_TSYN;

            // Instantiate Message Identifier (MID) class
            PALS.Common.IParameters paramMID = _xmlLoader.Paramters_MID;
            _inMIDGW2External = new PALS.Net.Filters.Application.IncomingMessageIdentifier(ref paramMID);
            ((PALS.Net.Common.AbstractProtocolChain)_inMIDGW2External).ObjectID = OBJECT_ID_GW2EXTERNAL_INMID;
            _outMIDGW2External = new PALS.Net.Filters.Application.OutgoingMessageIdentifier(ref paramMID);
            ((PALS.Net.Common.AbstractProtocolChain)_outMIDGW2External).ObjectID = OBJECT_ID_GW2EXTERNAL_OUTMID;

            // Instantiate ACK class
            PALS.Common.IParameters paramACK = _xmlLoader.Paramters_ACK;
            _ackGW2External = new PALS.Net.Filters.Acknowledge.ACK(ref paramACK);
            ((PALS.Net.Common.AbstractProtocolChain)_ackGW2External).ObjectID = OBJECT_ID_GW2EXTERNAL_ACK;

            // Instantiate GW2ExternalSessionForwarder class
            _forwarderGW2External = new BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.GW2ExternalSessionForwarder(null);
            ((PALS.Net.Common.AbstractProtocolChain)_forwarderGW2External).ObjectID = OBJECT_ID_GW2EXTERNAL_SESSIONFORWARDER;

            // Build GW2External communication chain
            _managerGW2External.AddHandlerToLast(ref _forwarderGW2External);
            _managerGW2External.AddFilterToLast(ref _outMIDGW2External);
            _managerGW2External.AddFilterToLast(ref _ackGW2External);
            _managerGW2External.AddFilterToLast(ref _inMIDGW2External);
            _managerGW2External.AddFilterToLast(ref _tsynGW2External);
            _managerGW2External.AddFilterToLast(ref _solGW2External);
            _managerGW2External.AddFilterToLast(ref _appClientGW2External);
            _managerGW2External.AddFilterToLast(ref _cipGW2External);
            _managerGW2External.AddFilterToLast(ref _eipGW2External);
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
            ((PALS.Net.Managers.SessionManager)_managerGW2Internal).ClassParameters =
                    _xmlLoader.Paramters_TCPClient_GW2Internal;
            ((PALS.Net.Filters.Frame.Frame)_frameGW2Internal).ClassParameters =
                    (PALS.Net.Filters.Frame.FrameParameters)_xmlLoader.Paramters_Frame;
            ((PALS.Net.Filters.Application.AppClient)_appClientGW2Internal).ClassParameters =
                    (PALS.Net.Filters.Application.AppClientParameters)_xmlLoader.Paramters_AppClient_GW2Internal;
            ((PALS.Net.Filters.SignOfLife.SOL)_solGW2Internal).ClassParameters =
                    (PALS.Net.Filters.SignOfLife.SOLParameters)_xmlLoader.Paramters_SOL_GW2Internal;
            ((PALS.Net.Filters.Application.IncomingMessageIdentifier)_inMIDGW2Internal).ClassParameters =
                    (PALS.Net.Filters.Application.MessageIdentifierParameters)_xmlLoader.Paramters_MID;
            ((PALS.Net.Filters.Acknowledge.ACK)_ackGW2Internal).ClassParameters =
                    (PALS.Net.Filters.Acknowledge.ACKParameters)_xmlLoader.Paramters_ACK;
            ((PALS.Net.Filters.Application.OutgoingMessageIdentifier)_outMIDGW2Internal).ClassParameters =
                    (PALS.Net.Filters.Application.MessageIdentifierParameters)_xmlLoader.Paramters_MID;

            // Reassign the reference of new parameter class object to GW2CCTV chain classes
            ((PALS.Net.Managers.SessionManager)_managerGW2External).ClassParameters =
                    _xmlLoader.Paramters_TCPServerClient_GW2External;
            ((PALS.Net.Filters.EIPCIP.EIP)_eipGW2External).ClassParameters =
                    (PALS.Net.Filters.EIPCIP.EIPParameters)_xmlLoader.Paramters_EIP;
            ((PALS.Net.Filters.EIPCIP.CIP)_cipGW2External).ClassParameters =
            (PALS.Net.Filters.EIPCIP.CIPParameters)_xmlLoader.Paramters_CIP;
            ((PALS.Net.Filters.Application.AppClient)_appClientGW2External).ClassParameters =
                    (PALS.Net.Filters.Application.AppClientParameters)_xmlLoader.Paramters_AppClient_GW2External;
            ((PALS.Net.Filters.SignOfLife.SOL)_solGW2External).ClassParameters =
                    (PALS.Net.Filters.SignOfLife.SOLParameters)_xmlLoader.Paramters_SOL_GW2External;
            ((PALS.Net.Filters.TimeSynchronizing.TimeSync)_tsynGW2External).ClassParameters =
                    (PALS.Net.Filters.TimeSynchronizing.TimeSyncParameters)_xmlLoader.Paramters_TSYN;
            ((PALS.Net.Filters.Application.IncomingMessageIdentifier)_inMIDGW2External).ClassParameters =
                    (PALS.Net.Filters.Application.MessageIdentifierParameters)_xmlLoader.Paramters_MID;
            ((PALS.Net.Filters.Acknowledge.ACK)_ackGW2External).ClassParameters =
                    (PALS.Net.Filters.Acknowledge.ACKParameters)_xmlLoader.Paramters_ACK;
            ((PALS.Net.Filters.Application.OutgoingMessageIdentifier)_outMIDGW2External).ClassParameters =
                    (PALS.Net.Filters.Application.MessageIdentifierParameters)_xmlLoader.Paramters_MID;

            // Reassign the reference of new parameter class object to MessageHandler class
            ((Messages.Handlers.MessageHandler)_msgHandler).ClassParameters =
                    (Messages.Handlers.MessageHandlerParameters)_xmlLoader.Paramters_MsgHandler;
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
                    _perfMonitor.AddObjectStatus("class",_className);
                    _perfMonitor.CloseObjectNode();
                }

                // Refresh all other class object status counters.
                PALS.Diagnostics.ClassStatus temp;
                temp = _managerGW2External.PerfMonitor;
                temp = ((PALS.Net.Filters.EIPCIP.EIP)_eipGW2External).PerfMonitor;
                temp = ((PALS.Net.Filters.EIPCIP.CIP)_cipGW2External).PerfMonitor;
                temp = ((PALS.Net.Filters.Application.AppClient)_appClientGW2External).PerfMonitor;
                temp = ((PALS.Net.Filters.SignOfLife.SOL)_solGW2External).PerfMonitor;
                temp = ((PALS.Net.Filters.TimeSynchronizing.TimeSync)_tsynGW2External).PerfMonitor;
                temp = ((PALS.Net.Filters.Application.IncomingMessageIdentifier)_inMIDGW2External).PerfMonitor;
                temp = ((PALS.Net.Filters.Acknowledge.ACK)_ackGW2External).PerfMonitor;
                temp = ((PALS.Net.Filters.Application.OutgoingMessageIdentifier)_outMIDGW2External).PerfMonitor;

                temp = _managerGW2Internal.PerfMonitor;
                temp = ((PALS.Net.Filters.Frame.Frame)_frameGW2Internal).PerfMonitor;
                temp = ((PALS.Net.Filters.Application.AppClient)_appClientGW2Internal).PerfMonitor;
                temp = ((PALS.Net.Filters.SignOfLife.SOL)_solGW2Internal).PerfMonitor;
                temp = ((PALS.Net.Filters.Application.IncomingMessageIdentifier)_inMIDGW2Internal).PerfMonitor;
                temp = ((PALS.Net.Filters.Acknowledge.ACK)_ackGW2Internal).PerfMonitor;
                temp = ((PALS.Net.Filters.Application.OutgoingMessageIdentifier)_outMIDGW2Internal).PerfMonitor;

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
