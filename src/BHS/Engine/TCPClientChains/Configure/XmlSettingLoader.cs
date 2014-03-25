#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       XmlSettingLoader.cs
// Revision:      1.0 -   02 Apr 2009, By Xu Jian
// =====================================================================================
//
#endregion

using System;
using System.IO;
using System.Xml;
using PALS.Configure;
using PALS.Utilities;

namespace BHS.Engine.TCPClientChains.Configure
{
    /// <summary>
    /// Loading application settings from XML file.
    /// </summary>
    public class XmlSettingLoader : PALS.Configure.IConfigurationLoader  
    {
        #region Class Field and Property Declarations

        // there are total 2 XML configuration files required by SAC2PLC GW application: 
        // CFG_SortEngine.xml.xml - application settings 
        // CFG_Telegrams.xml  - application telegram format definations.
        private const int DESIRED_NUMBER_OF_CFG_FILES = 2;

        // XMLNode name of configuration sets.
        private const string XML_CONFIGSET = "configSet";
        private const string XML_CONFIGSET_GLOBALCONTEXT = "GlobalContext";
        private const string XML_CONFIGSET_TCPCLIENT = "PALS.Net.Transports.TCP.TCPClient";
        private const string XML_CONFIGSET_FRAME = "PALS.Net.Filters.Frame.Frame";
        private const string XML_CONFIGSET_APPCLIENT = "PALS.Net.Filters.Application.AppClient";
        private const string XML_CONFIGSET_SOL = "PALS.Net.Filters.SignOfLife.SOL";
        private const string XML_CONFIGSET_ACK = "PALS.Net.Filters.Acknowledge.ACK";
        private const string XML_CONFIGSET_MSGHANDLER = "BHS.Engine.TCPClientChains.Messages.Handlers.MessageHandler";
        private const string XML_CONFIGSET_DBPERSISTOR = "BHS.Engine.TCPClientChains.DataPersistor.Database.Persistor";
        private const string XML_CONFIGSET_TELEGRAM_FORMAT = "Telegram_Formats";

        // The name of current class 
        private static readonly string _className =
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
        // Create a logger for use in this class
        private static readonly log4net.ILog _logger =
                    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        // Global parameter classes variables for storing application settings loaded from configuration file.
        // In order to prevent the overwriting the existing system settings stored in the gloabl parameter variables  
        // due to the failure of reloading configuration file, the loaded parameters shall be stored into
        // the temporary variables and only assign to global parameter variables is the loading successed.

        /// <summary>
        /// Global object for storing GlobalContext settings.
        /// </summary>
        public GlobalContext Paramters_GlobalContext { get; set; }
        /// <summary>
        /// Global object for storing TCPClient protocol settings of Sort Engine Service 
        /// application communication chain.
        /// </summary>
        public PALS.Common.IParameters Paramters_TCPClient { get; set; }
        /// <summary>
        /// Global object for storing Frame protocol settings.
        /// </summary>
        public PALS.Common.IParameters Paramters_Frame { get; set; }
        /// <summary>
        /// Global object for storing AppClient protocol settings of Sort Engine Service.
        /// </summary>
        public PALS.Common.IParameters Paramters_AppClient { get; set; }
        /// <summary>
        /// Global object for storing SOL protocol settings of Sort Engine Service.
        /// </summary>
        public PALS.Common.IParameters Paramters_SOL { get; set; }
        /// <summary>
        /// Global object for storing ACK protocol settings.
        /// </summary>
        public PALS.Common.IParameters Paramters_ACK { get; set; }
        /// <summary>
        /// Global object for storing MID protocol settings.
        /// </summary>
        public PALS.Common.IParameters Paramters_MID { get; set; }
        /// <summary>
        /// Global object for storing MessageHandler settings.
        /// </summary>
        public PALS.Common.IParameters Paramters_MsgHandler { get; set; }
        /// <summary>
        /// Global object for storing data Persistor settings.
        /// </summary>
        public PALS.Common.IParameters Paramters_DBPersistor { get; set; }


        /// <summary>
        /// Event will be raised when reload setting from changed configuration 
        /// file is successfully completed.
        /// </summary>
        public event EventHandler OnReloadSettingCompleted;

        #endregion

        #region Class Constructor, Dispose, & Destructor

        /// <summary>
        /// Class constructor
        /// </summary>
        public XmlSettingLoader()
        {
        }

        /// <summary>
        /// Class destructor
        /// </summary>
        ~XmlSettingLoader()
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

            // Destory class level fields.
            if (Paramters_GlobalContext != null) Paramters_GlobalContext = null;
            if (Paramters_TCPClient != null) Paramters_TCPClient = null;
            if (Paramters_Frame != null) Paramters_Frame = null;
            if (Paramters_AppClient != null) Paramters_AppClient = null;
            if (Paramters_SOL != null) Paramters_SOL = null;
            if (Paramters_ACK != null) Paramters_ACK = null;
            if (Paramters_MID != null) Paramters_MID = null;
            if (Paramters_MsgHandler != null) Paramters_MsgHandler = null;
            if (Paramters_DBPersistor != null) Paramters_DBPersistor = null;

            if (disposing)
            {
                if (_logger.IsInfoEnabled)
                    _logger.Info("Class:[" + _className + "] object has been destroyed. <" + thisMethod + ">");
            }
        }

        #endregion


        #region Class Methods

        #endregion


        #region IConfigurationLoader Members

        /// <summary>
        /// This class method is the place to centralize the loading of application settings from 
        /// configuration file. 
        /// <para>
        /// The actual implementation of IConfigurationLoader interface method LoadSettingFromConfigFile(). 
        /// This method will be invoked by AppConfigurator class.
        /// </para>
        /// <para>
        /// If the parameter isReloading = true, the interface implemented LoadSettingFromConfigFile() 
        /// may raise a event after all settings have been reloaded successfully, to inform application 
        /// that the reloading setting has been done. So application can take the necessary actions
        /// to take effective of new settings.
        /// </para>
        /// <para>
        /// Decode XML configuration file and load application settings shall be done by this method.
        /// </para>
        /// </summary>
        /// <param name="isReloading">
        /// If the parameter isReloading = true, the interface implemented LoadSettingFromConfigFile() 
        /// may raise a event after all settings have been reloaded successfully, to inform application 
        /// that the reloading setting has been done. So application can take the necessary actions
        /// to take effective of new settings.
        /// </param>
        /// <param name="cfgFiles">
        /// params type method argument, represents one or more configuration files.
        /// </param>
        void IConfigurationLoader.LoadSettingFromConfigFile(bool isReloading, params FileInfo[] cfgFiles)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            // If the number of configuration files passed in is not same as the desired number, then throw exception.
            if (cfgFiles.Length != DESIRED_NUMBER_OF_CFG_FILES)
                throw new Exception("The number of files (" + cfgFiles.Length +
                        ") passed to configuration loader is not desired number (" + DESIRED_NUMBER_OF_CFG_FILES + ").");

            // -------------------------------------------------------------------------------
            if (_logger.IsInfoEnabled)
                _logger.Info("Loading application settings... <" + thisMethod + ">");
            // -------------------------------------------------------------------------------

            // Get the root elements of XML file: CFG_SortEngine.xml & CFG_Telegrams.xml.
            XmlElement rootSetting, rootTelegram;
            XmlNode node, nodeTele;
            
            rootSetting = XMLConfig.GetConfigFileRootElement(ref cfgFiles[0]);
            if (rootSetting == null)
                throw new Exception("Get root XmlElement failure! [Xml File: " + cfgFiles[0].FullName  + "].");

            rootTelegram = XMLConfig.GetConfigFileRootElement(cfgFiles[1].FullName );
            if (rootTelegram == null)
                throw new Exception("Get root XmlElement failure! [Xml File: " + cfgFiles[1].FullName + "].");
         
            nodeTele = XMLConfig.GetConfigSetElement(ref rootTelegram, XML_CONFIGSET, "name", XML_CONFIGSET_TELEGRAM_FORMAT);
            if (nodeTele == null)
                throw new Exception("ConfigSet <configSet name=\"" + XML_CONFIGSET_TELEGRAM_FORMAT +
                            "\"> is not found in the XML file.");

            if (_logger.IsInfoEnabled)
                _logger.Info(string.Format("Loading application settings from below configuration file(s): <" + 
                            thisMethod + "> \n   {0}\n   {1}", cfgFiles[0].FullName, cfgFiles[1].FullName));

            // -------------------------------------------------------------------------------
            // Load GlobalContext settings from <configSet name="globalContext"> XMLNode
            // -------------------------------------------------------------------------------
            // <configSet name="globalContext">
            //  <!--Generate Application Information-->
            //  <appName>MDS2CCTVEngine</appName>
            //  <company>PterisGlobal</company>
            //  <department>CSI</department>
            //  <author>XuJian</author>
            // <configSet name="globalContext">
            // -------------------------------------------------------------------------------
            // Description: In order to prevent the overwriting the existing system settings 
            // stored in the gloabl variables due to the failure of reloading configuration
            // file, the loaded parameters shall be stored into the temporary variables and 
            // only assign to global variables is the loading successed.
            //
            node = XMLConfig.GetConfigSetElement(ref rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_GLOBALCONTEXT);
            if (node != null)
            {
                // Declare a temporary parameter class object
                GlobalContext tempParam = new GlobalContext();

                tempParam.AppName = XMLConfig.GetSettingFromInnerText(node, "appName", "SORTENGN");
                tempParam.AppStartedTime = DateTime.Now;
                tempParam.Company = XMLConfig.GetSettingFromInnerText(node, "company", "PterisGlobal");
                tempParam.Department = XMLConfig.GetSettingFromInnerText(node, "department", "CSI");
                tempParam.Author = XMLConfig.GetSettingFromInnerText(node, "author", "HSC");

                // Assign temporary parameter object reference to global parameter object 
                if (tempParam != null)
                    Paramters_GlobalContext = tempParam;
                else
                    throw new Exception("Reading settings from ConfigSet <configSet name=\"" +
                            XML_CONFIGSET_GLOBALCONTEXT + "\"> is failed!");
            }
            else
            {
                throw new Exception("ConfigSet <" + XML_CONFIGSET_GLOBALCONTEXT + "> is not found in the XML file.");
            }

#if DEBUG
            // Start of debugging codes.
            if (_logger.IsDebugEnabled)
            {
                _logger.Debug(string.Format("[Param: Paramters_GlobalContext] AppName={0}, AppStartedTime={1}, " +
                        "Company={2}, Department={3}, Author={4}",
                        Paramters_GlobalContext.AppName, Paramters_GlobalContext.AppStartedTime,
                        Paramters_GlobalContext.Company, Paramters_GlobalContext.Department, 
                        Paramters_GlobalContext.Author));
            }
            // End of debugging codes.
#endif
            // -------------------------------------------------------------------------------

            // -------------------------------------------------------------------------------
            // Load TCPClient class parameters from <configSet name="PALS.Net.Transports.TCP.TCPClient"> XMLNode
            // -------------------------------------------------------------------------------
            //<configSet name="PALS.Net.Transports.TCP.TCPClient">
            //  <threadInterval>10</threadInterval>
            //  <isAutoReconnect>True</isAutoReconnect>
            //  <reconnectTimeout>20000</reconnectTimeout>
            //  <!--Maximum length of name is 1~8 characters-->
            //  <!--MDS2CCTVGW Svr IP: ?, CCTV Svr IP: ? -->
            //  <localNode name="SORTENGN" ip="127.0.0.1" port="0"/>
            //  <remoteNodes>
            //    <!--SocketConnector object is able to connect to multiple remote TCP servers-->
            //    <!--If there are more than one TCP server, just add following server element accordingly-->
            //    <!--Maximum length of name is 8 characters-->
            //    <server name="MSGROUTE" ip="127.0.0.1" port="26214"/>
            //  </remoteNodes>
            //</configSet>
            // -------------------------------------------------------------------------------
            node = null;
            node = XMLConfig.GetConfigSetElement(ref rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_TCPCLIENT);
            if (node != null)
            {
                // Declare a temporary parameter class object
                PALS.Common.IParameters tempParam;

                // Read settings from particular <configSet> by constructor of parameter class object.
                tempParam = new PALS.Net.Transports.TCP.TCPClientParameters(ref node);

                // Assign temporary parameter object reference to global parameter object 
                if (tempParam != null)
                {
                    Paramters_TCPClient = tempParam;
                }
                else
                    throw new Exception("Reading settings from ConfigSet <configSet name=\"" +
                            XML_CONFIGSET_TCPCLIENT + "\"> is failed!");
            }
            else
                throw new Exception("ConfigSet <configSet name=\"" + XML_CONFIGSET_TCPCLIENT + 
                            "\"> is not found in the XML file.");

#if DEBUG
            // Start of debugging codes.
            if (_logger.IsDebugEnabled)
            {
                PALS.Net.Transports.TCP.TCPClientParameters param = 
                        (PALS.Net.Transports.TCP.TCPClientParameters)Paramters_TCPClient;

                _logger.Debug(string.Format("[Param: Paramters_TCPClient] LocalNode={0}, " +
                        "#ofRemoteNode={1}, IsAutoReconnected={2}, ReconnectTimeout={3}",
                        param.LocalNode.ToString(), param.RemoteNodeHash.Count,
                        param.IsAutoReconnected, param.ReconnectTimeout));
            }
            // End of debugging codes.
#endif
            // -------------------------------------------------------------------------------

            // -------------------------------------------------------------------------------
            // Load Frame class parameters from <configSet name="PALS.Net.Filters.Frame.Frame"> XMLNode
            // -------------------------------------------------------------------------------
            //<configSet name="PALS.Net.Filters.Frame.Frame">
            //  <!--Only single character can be used as startMarker, endMarker, and specialMarker-->
            //  <startMarker>02</startMarker>
            //  <endMarker>03</endMarker>
            //  <!--If the character of startMarker or endMarker is included in the outgoing-->
            //  <!--data, the specialMarker is required to be prefixed in order to differentiate-->
            //  <!--the start or end marker and the actual data character.-->
            //  <specialMarker>27</specialMarker>
            //  <!--If accumulated incoming telegram length has been more than maxTelegramSize-->
            //  <!--(number of byte) but no EndMarker received, all accumulated data will be discarded.-->
            //  <maxTelegramSize>10240</maxTelegramSize>
            //</configSet>
            // -------------------------------------------------------------------------------
            node = null;
            node = XMLConfig.GetConfigSetElement(ref rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_FRAME);
            if (node != null)
            {
                // Declare a temporary parameter class object
                PALS.Common.IParameters tempParam;

                // Read settings from particular <configSet> by constructor of parameter class object.
                tempParam = new PALS.Net.Filters.Frame.FrameParameters(ref node, ref nodeTele);

                // Assign temporary parameter object reference to global parameter object 
                if (tempParam != null)
                    Paramters_Frame = tempParam;
                else
                    throw new Exception("Reading settings from ConfigSet <configSet name=\"" +
                            XML_CONFIGSET_FRAME + "\"> is failed!");
            }
            else
                throw new Exception("ConfigSet <configSet name=\"" + XML_CONFIGSET_FRAME +
                            "\"> is not found in the XML file.");

#if DEBUG
            // Start of debugging codes.
            if (_logger.IsDebugEnabled)
            {
                PALS.Net.Filters.Frame.FrameParameters param =
                        (PALS.Net.Filters.Frame.FrameParameters)Paramters_Frame;
                
                _logger.Debug(string.Format("[Param: Paramters_Frame] StartMarker=0x{0}, " +
                        "EndMarker=0x{1}, SpecialMarker=0x{2}, MaxTelegramSize={3}",
                        param.StartMarker.ToString("X2"), param.EndMarker.ToString("X2"), param.SpecialMarker.ToString("X2"),
                        param.MaxTelegramSize.ToString()));
            }
            // End of debugging codes.
#endif
            // -------------------------------------------------------------------------------

            // -------------------------------------------------------------------------------
            // Load AppClient class parameters from <configSet name="PALS.Net.Filters.Application.AppClient"> XMLNode
            // -------------------------------------------------------------------------------
            //<configSet name="PALS.Net.Filters.Application.AppClient">
            //  <threadInterval>100</threadInterval>
            //  <!--Maximum length of clientAppCode is 8 characters-->
            //  <clientAppCode>CCTVENGN</clientAppCode>
            //  <!--connectionConfirmTimeout value must bigger than the same parameter of bottom layer (RFC1006)-->
            //  <connectionConfirmTimeout>3000</connectionConfirmTimeout>
            //  <connectionRequestRetries>3</connectionRequestRetries>
            //  <minSequenceNo>1</minSequenceNo>
            //  <maxSequenceNo>9999</maxSequenceNo>
            //</configSet>
            // -------------------------------------------------------------------------------
            node = null;
            node = XMLConfig.GetConfigSetElement(ref rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_APPCLIENT);
            if (node != null)
            {
                // Declare a temporary parameter class object
                PALS.Common.IParameters tempParam;

                // Read settings from particular <configSet> by constructor of parameter class object.
                tempParam = new PALS.Net.Filters.Application.AppClientParameters(ref node, ref nodeTele);

                // Assign temporary parameter object reference to global parameter object 
                if (tempParam != null)
                    Paramters_AppClient = tempParam;
                else
                    throw new Exception("Reading settings from ConfigSet <configSet name=\"" +
                            XML_CONFIGSET_APPCLIENT + "\"> is failed!");
            }
            else
                throw new Exception("ConfigSet <configSet name=\"" + XML_CONFIGSET_APPCLIENT +
                            "\"> is not found in the XML file.");

#if DEBUG
            // Start of debugging codes.
            if (_logger.IsDebugEnabled)
            {
                PALS.Net.Filters.Application.AppClientParameters param =
                        (PALS.Net.Filters.Application.AppClientParameters)Paramters_AppClient;

                _logger.Debug(string.Format("[Param: Paramters_AppClient] ThreadInterval={0}, AppCode={1}" +
                    "CCFTimeout={2}, CRQRetries={3}, MinSequenceNo={4}, MaxSequenceNo={5}",
                    param.ThreadInterval.ToString(), param.AppCode, param.CCFTimeout.ToString(), param.CRQRetries.ToString(),
                    param.MinSequenceNo.ToString(), param.MaxSequenceNo.ToString()));
            }
            // End of debugging codes.
#endif
            // -------------------------------------------------------------------------------

            // -------------------------------------------------------------------------------
            // Load SOL class parameters from <configSet name="PALS.Net.Filters.SignOfLife.SOL"> XMLNode
            // -------------------------------------------------------------------------------
            //<configSet name="PALS.Net.Filters.SignOfLife.SOL">
            //  <threadInterval>100</threadInterval>
            //  <solSendTimeout>10000</solSendTimeout>
            //  <solReceiveTimeout>25000</solReceiveTimeout>
            //</configSet>
            // -------------------------------------------------------------------------------
            node = null;
            node = XMLConfig.GetConfigSetElement(ref rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_SOL);
            if (node != null)
            {
                // Declare a temporary parameter class object
                PALS.Common.IParameters tempParam;

                // Read settings from particular <configSet> by constructor of parameter class object.
                tempParam = new PALS.Net.Filters.SignOfLife.SOLParameters(ref node, ref nodeTele);

                // Assign temporary parameter object reference to global parameter object 
                if (tempParam != null)
                    Paramters_SOL = tempParam;
                else
                    throw new Exception("Reading settings from ConfigSet <configSet name=\"" +
                            XML_CONFIGSET_SOL + "\"> is failed!");
            }
            else
                throw new Exception("ConfigSet <configSet name=\"" + XML_CONFIGSET_SOL +
                            "\"> is not found in the XML file.");

#if DEBUG
            // Start of debugging codes.
            if (_logger.IsDebugEnabled)
            {
                PALS.Net.Filters.SignOfLife.SOLParameters param =
                        (PALS.Net.Filters.SignOfLife.SOLParameters)Paramters_SOL;

                _logger.Debug(string.Format("[Param: Paramters_SOL] ThreadInterval={0}, SOLReceiveTimeout={1}, SOLSendTimeout={2}",
                        param.ThreadInterval, param.SOLReceiveTimeout, param.SOLSendTimeout));
            }
            // End of debugging codes.
#endif
            // -------------------------------------------------------------------------------

            // -------------------------------------------------------------------------------
            // Load ACK class parameters from <configSet name="PALS.Net.Filters.Acknowledge.ACK"> XMLNode
            // -------------------------------------------------------------------------------
            //<configSet name="PALS.Net.Filters.Acknowledge.ACK">
            //  <threadInterval>100</threadInterval>
            //  <retransmitBufferSize>1</retransmitBufferSize>
            //  <retransmitTimeour>3000</retransmitTimeour>
            //  <retransmitRetries>3</retransmitRetries>
            //</configSet>
            // -------------------------------------------------------------------------------
            node = null;
            node = XMLConfig.GetConfigSetElement(ref rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_ACK);
            if (node != null)
            {
                // Declare a temporary parameter class object
                PALS.Common.IParameters tempParam;

                // Read settings from particular <configSet> by constructor of parameter class object.
                tempParam = new PALS.Net.Filters.Acknowledge.ACKParameters(ref node, ref nodeTele);

                // Assign temporary parameter object reference to global parameter object 
                if (tempParam != null)
                    Paramters_ACK = tempParam;
                else
                    throw new Exception("Reading settings from ConfigSet <configSet name=\"" +
                            XML_CONFIGSET_ACK + "\"> is failed!");
            }
            else
                throw new Exception("ConfigSet <configSet name=\"" + XML_CONFIGSET_ACK +
                            "\"> is not found in the XML file.");

#if DEBUG
            // Start of debugging codes.
            if (_logger.IsDebugEnabled)
            {
                PALS.Net.Filters.Acknowledge.ACKParameters param =
                        (PALS.Net.Filters.Acknowledge.ACKParameters)Paramters_ACK;

                _logger.Debug(string.Format("[Param: Paramters_ACK] ThreadInterval={0}, RetransmitBufferSize={1}," +
                    "RetransmitTimeout={2}, RetransmitRetries={3}",param.ThreadInterval, param.RetransmitBufferSize, 
                    param.RetransmitTimeout, param.RetransmitRetries));
            }
            // End of debugging codes.
#endif
            // -------------------------------------------------------------------------------

            // -------------------------------------------------------------------------------
            // Load MID class parameters from <telegramSet name="Application_Telegrams"> XMLNode
            // -------------------------------------------------------------------------------
            //<telegramSet name="Application_Telegrams">
            //  <header alias="Header" name="App_Header" sequence="False" acknowledge="False">
            //    <field name="Type" offset="0" length="4" default=""/>
            //    <field name="Length" offset="4" length="4" default=""/>
            //    <field name="Sequence" offset="8" length="4" default=""/>
            //  </header>
            //  <!-- "Type, Length" field of Application message is mandatory for APP class. -->
            //  <telegram alias="CRQ" name="App_Connection_Request_Message" sequence="True" acknowledge="False">
            //    <!-- value="48,48,48,49" - the ASCII value (decimal) string. -->
            //    <!-- "48,48,48,49" here represents the default field value are -->
            //    <!-- 4 bytes (H30 H30 H30 H31). The delimiter must be comma(,). -->
            //    <field name="Type" offset="0" length="4" default="48,48,48,49"/>
            //    <field name="Length" offset="4" length="4" default="48,48,50,48"/>
            //    <field name="Sequence" offset="8" length="4" default="?"/>
            //    <field name="ClientAppCode" offset="12" length="8" default="?"/>
            //  </telegram>
            //  ...
            // -------------------------------------------------------------------------------
            node = null;
            if (nodeTele != null)
            {
                // Declare a temporary parameter class object
                PALS.Common.IParameters tempParam;

                // Read settings from particular <configSet> by constructor of parameter class object.
                tempParam = new PALS.Net.Filters.Application.MessageIdentifierParameters(ref node, ref nodeTele);

                // Assign temporary parameter object reference to global parameter object 
                if (tempParam != null)
                    Paramters_MID = tempParam;
                else
                    throw new Exception("Reading settings from ConfigSet <telegramSet name=\"Application_Telegrams\"> is failed!");
            }
            else
                throw new Exception("Reading settings from ConfigSet <telegramSet name=\"Application_Telegrams\"> is failed!");

#if DEBUG
            // Start of debugging codes.
            if (_logger.IsDebugEnabled)
            {
                PALS.Net.Filters.Application.MessageIdentifierParameters param =
                        (PALS.Net.Filters.Application.MessageIdentifierParameters)Paramters_MID;

                System.Text.StringBuilder msg = new System.Text.StringBuilder();

                foreach (System.Collections.DictionaryEntry de in param.MessageFormatHash)
                {
                    PALS.Telegrams.TelegramFormat tf = (PALS.Telegrams.TelegramFormat)de.Value;

                    msg.Append(de.Key);
                    msg.Append("(");
                    msg.Append(tf.AliasName);
                    msg.Append("), ");
                }

                _logger.Debug(string.Format("[Param: Paramters_MID] Application Messages ={0}", msg));
            }
            // End of debugging codes.
#endif
            // -------------------------------------------------------------------------------

            // -------------------------------------------------------------------------------
            // Load MessageHandler class parameters from telegram format XML file
            // -------------------------------------------------------------------------------
            //<configSet name="BHS.Engine.TCPClientChains.Messages.Handlers.MessageHandler">
            //  <!-- INTM message (local) sender, max 8 characters-->
            //  <sender>SORTENGN</sender>
            //  <!-- INTM message (remote) receiver, max 8 characters-->
            //  <afai_Receiver>SAC2PLC1,SAC2PLC2,SAC2PLC3,SAC2PLC4,SAC2PLC5,SAC2PLC6</afai_Receiver>
            //  <crai_Receiver>SAC2PLC1,SAC2PLC2,SAC2PLC3,SAC2PLC4,SAC2PLC5,SAC2PLC6</crai_Receiver>
            //  <fbti_Receiver>SAC2PLC1,SAC2PLC2,SAC2PLC3,SAC2PLC4,SAC2PLC5,SAC2PLC6</fbti_Receiver>
            //  <fpti_Receiver>SAC2PLC1,SAC2PLC2,SAC2PLC3,SAC2PLC4,SAC2PLC5,SAC2PLC6</fpti_Receiver>
            //  <tpti_Receiver>SAC2PLC1,SAC2PLC2,SAC2PLC3,SAC2PLC4,SAC2PLC5,SAC2PLC6</tpti_Receiver>  
            //</configSet>
            // -------------------------------------------------------------------------------
            node = null;
            node = XMLConfig.GetConfigSetElement(ref rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_MSGHANDLER);
            if (node != null)
            {
                // Declare a temporary parameter class object
                PALS.Common.IParameters tempParam;

                // Read settings from particular <configSet> by constructor of parameter class object.
                tempParam = new Messages.Handlers.MessageHandlerParameters(node, nodeTele);

                // Assign temporary parameter object reference to global parameter object 
                if (tempParam != null)
                    Paramters_MsgHandler = tempParam;
                else
                    throw new Exception("Reading settings from ConfigSet <configSet name=\"" +
                            XML_CONFIGSET_MSGHANDLER + "\"> is failed!");
            }
            else
                throw new Exception("ConfigSet <configSet name=\"" + XML_CONFIGSET_MSGHANDLER +
                            "\"> is not found in the XML file.");

#if DEBUG
            // Start of debugging codes.
            if (_logger.IsDebugEnabled)
            {
                Messages.Handlers.MessageHandlerParameters param =
                        (Messages.Handlers.MessageHandlerParameters)Paramters_MsgHandler;

                string temp_AFAI=string.Empty;
                foreach (string receiver in param.AFAI_Receiver)
                {
                    if (temp_AFAI == string.Empty)
                    {
                        temp_AFAI = receiver + ',';
                    }
                    else
                    {
                        temp_AFAI = temp_AFAI + ',' + receiver;
                    }
                }

                string temp_CRAI = string.Empty;
                foreach (string receiver in param.CRAI_Receiver)
                {
                    if (temp_CRAI == string.Empty)
                    {
                        temp_CRAI = receiver + ',';
                    }
                    else
                    {
                        temp_CRAI = temp_CRAI + ',' + receiver;
                    }
                }

                string temp_FBTI = string.Empty;
                foreach (string receiver in param.FBTI_Receiver)
                {
                    if (temp_FBTI == string.Empty)
                    {
                        temp_FBTI = receiver + ',';
                    }
                    else
                    {
                        temp_FBTI = temp_FBTI + ',' + receiver;
                    }
                }

                string temp_FPTI = string.Empty;
                foreach (string receiver in param.FPTI_Receiver)
                {
                    if (temp_FPTI == string.Empty)
                    {
                        temp_FPTI = receiver + ',';
                    }
                    else
                    {
                        temp_FPTI = temp_AFAI + ',' + receiver;
                    }
                }

                string temp_TPTI = string.Empty;
                //foreach (string receiver in param.TPTI_Receiver)
                //{
                //    if (temp_TPTI == string.Empty)
                //    {
                //        temp_TPTI = receiver + ',';
                //    }
                //    else
                //    {
                //        temp_TPTI = temp_TPTI + ',' + receiver;
                //    }
                //}

                _logger.Debug(string.Format("[Param: Paramters_MsgHandler] Sender={0}, afai_Receiver={1}" +
                        "crai_Receiver={2}, fbti_Receiver={3}, fpti_Receiver={4}, tpti_Receiver={5}",
                        param.Sender, temp_AFAI, temp_CRAI, temp_FBTI, temp_FPTI, temp_TPTI));
            }
            // End of debugging codes.
#endif
            // -------------------------------------------------------------------------------
            
            // -------------------------------------------------------------------------------
            // Load Persistor class parameters from XML file
            // -------------------------------------------------------------------------------
            // <configSet name="BHS.Engine.TCPClientChains.DataPersistor.Database.Persistor">
            //   <connectionString>Persist Security Info=False;User ID=sacdbuser;Pwd=sac@interr0l1er;Initial Catalog=BHSDB;Data Source=DBSQL;Packet Size=4096</connectionString>
            //   ...
            // </configSet>
            // -------------------------------------------------------------------------------
            node = null;
            node = XMLConfig.GetConfigSetElement(ref rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_DBPERSISTOR);
            if (node != null)
            {
                // Declare a temporary parameter class object
                PALS.Common.IParameters tempParam;

                // Read settings from particular <configSet> by constructor of parameter class object.
                tempParam = new Engine.TCPClientChains.DataPersistor.Database.PersistorParameters(node, nodeTele);

                // Assign temporary parameter object reference to global parameter object 
                if (tempParam != null)
                    Paramters_DBPersistor = tempParam;
                else
                    throw new Exception("Reading settings from ConfigSet <configSet name=\"" +
                            XML_CONFIGSET_ACK + "\"> is failed!");
            }
            else
                throw new Exception("ConfigSet <configSet name=\"" + XML_CONFIGSET_ACK +
                            "\"> is not found in the XML file.");

#if DEBUG
            // Start of debugging codes.
            if (_logger.IsDebugEnabled)
            {
                Engine.TCPClientChains.DataPersistor.Database.PersistorParameters param =
                        (Engine.TCPClientChains.DataPersistor.Database.PersistorParameters)Paramters_DBPersistor;

                _logger.Debug(string.Format("[Param: Paramters_DBPersistor] DB ConnectionString={0}",
                        param.DBConnectionString));
            }
            // End of debugging codes.
#endif
            // -------------------------------------------------------------------------------

            // Raise event when reload setting from changed configuration file is successfully completed.
            if (isReloading)
            {
                if (OnReloadSettingCompleted != null)
                {
                    // Event will only be raised when there is any event handler has been subscribed to it.
                    OnReloadSettingCompleted(this, new EventArgs());
                }
            }

            // -------------------------------------------------------------------------------
            if (_logger.IsInfoEnabled)
                _logger.Info("Loading application settings is successed. <" + thisMethod + ">");
            // -------------------------------------------------------------------------------
        }

        #endregion


    }
}
