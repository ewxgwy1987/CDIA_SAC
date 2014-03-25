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

namespace BHS.Gateway.TCPClientTCPClientChains.Configure
{
    /// <summary>
    /// Loading application settings from XML file.
    /// </summary>
    public class XmlSettingLoader : PALS.Configure.IConfigurationLoader  
    {
        #region Class Field and Property Declarations

        // there are total 2 XML configuration files required by MDS2CCTV GW application: 
        // CFG_MDS2CCTVGW.xml - application settings 
        // CFG_Telegrams.xml  - application telegram format definations.
        private const int DESIRED_NUMBER_OF_CFG_FILES = 2;

        // XMLNode name of configuration sets.
        private const string XML_CONFIGSET = "configSet";
        private const string XML_CONFIGSET_GLOBALCONTEXT = "GlobalContext";
        private const string XML_CONFIGSET_GW2INTERNAL_TCPCLIENT = "[Channel:GW2Internal]PALS.Net.Transports.TCP.TCPClient";
        private const string XML_CONFIGSET_GW2EXTERNAL_TCPSERVERCLIENT = "[Channel:GW2External]PALS.Net.Transports.TCP.TCPServerClient";
        private const string XML_CONFIGSET_FRAME = "PALS.Net.Filters.Frame.Frame";
        private const string XML_CONFIGSET_EIP = "PALS.Net.Filters.EIPCIP.EIP";
        private const string XML_CONFIGSET_CIP = "PALS.Net.Filters.EIPCIP.CIP";
        private const string XML_CONFIGSET_GW2INTERNAL_APPCLIENT = "[Channel:GW2Internal]PALS.Net.Filters.Application.AppClient";
        private const string XML_CONFIGSET_GW2EXTERNAL_APPCLIENT = "[Channel:GW2External]PALS.Net.Filters.Application.AppClient";
        private const string XML_CONFIGSET_TSYN = "PALS.Net.Filters.TimeSynchronizing.TimeSync";
        private const string XML_CONFIGSET_GW2INTERNAL_SOL = "[Channel:GW2Internal]PALS.Net.Filters.SignOfLife.SOL";
        private const string XML_CONFIGSET_GW2EXTERNAL_SOL = "[Channel:GW2External]PALS.Net.Filters.SignOfLife.SOL";
        private const string XML_CONFIGSET_ACK = "PALS.Net.Filters.Acknowledge.ACK";
        private const string XML_CONFIGSET_MSGHANDLER = "BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.MessageHandler";
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
        /// Global object for storing TCPClient protocol settings of GW2Internal Engine Service 
        /// application communication chain.
        /// </summary>
        public PALS.Common.IParameters Paramters_TCPClient_GW2Internal { get; set; }
        /// <summary>
        /// Global object for storing TCPClient protocol settings of GW2External device communication chain.
        /// </summary>
        public PALS.Common.IParameters Paramters_TCPServerClient_GW2External { get; set; }
        /// <summary>
        /// Global object for storing Frame protocol settings.
        /// </summary>
        public PALS.Common.IParameters Paramters_Frame { get; set; }
        /// <summary>
        /// Global object for storing EIP protocol settings.
        /// </summary>
        public PALS.Common.IParameters Paramters_EIP { get; set; }
        /// <summary>
        /// Global object for storing CIP protocol settings.
        /// </summary>
        public PALS.Common.IParameters Paramters_CIP { get; set; }
        /// <summary>
        /// Global object for storing AppClient protocol settings of GW2Internal Engine Service.
        /// </summary>
        public PALS.Common.IParameters Paramters_AppClient_GW2Internal { get; set; }
        /// <summary>
        /// Global object for storing AppClient protocol settings of GW2External device communication chain.
        /// </summary>
        public PALS.Common.IParameters Paramters_AppClient_GW2External { get; set; }
        /// <summary>
        /// Global object for storing TSYN protocol settings.
        /// </summary>
        public PALS.Common.IParameters Paramters_TSYN { get; set; }
        /// <summary>
        /// Global object for storing SOL protocol settings of GW2Internal Engine Service.
        /// </summary>
        public PALS.Common.IParameters Paramters_SOL_GW2Internal { get; set; }
        /// <summary>
        /// Global object for storing SOL protocol settings of GW2External device communication chain.
        /// </summary>
        public PALS.Common.IParameters Paramters_SOL_GW2External { get; set; }
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
            if (Paramters_TCPClient_GW2Internal != null) Paramters_TCPClient_GW2Internal = null;
            if (Paramters_TCPServerClient_GW2External != null) Paramters_TCPServerClient_GW2External = null;
            if (Paramters_Frame != null) Paramters_Frame = null;
            if (Paramters_AppClient_GW2Internal != null) Paramters_AppClient_GW2Internal = null;
            if (Paramters_AppClient_GW2External != null) Paramters_AppClient_GW2External = null;
            if (Paramters_TSYN != null) Paramters_TSYN = null;
            if (Paramters_SOL_GW2Internal != null) Paramters_SOL_GW2Internal = null;
            if (Paramters_SOL_GW2External != null) Paramters_SOL_GW2External = null;
            if (Paramters_ACK != null) Paramters_ACK = null;
            if (Paramters_MID != null) Paramters_MID = null;
            if (Paramters_MsgHandler != null) Paramters_MsgHandler = null;

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

            // Get the root elements of XML file: CFG_MDS2CCTVGW.xml & CFG_Telegrams.xml.
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
            //  <appName>MDS2CCTVGW</appName>
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

                tempParam.AppName = XMLConfig.GetSettingFromInnerText(node, "appName", "SAC2PLC1GW");
                tempParam.AppStartedTime = DateTime.Now;
                tempParam.Company = XMLConfig.GetSettingFromInnerText(node, "company", "PterisGlobal");
                tempParam.Department = XMLConfig.GetSettingFromInnerText(node, "department", "CSI");
                tempParam.Author = XMLConfig.GetSettingFromInnerText(node, "author", "HSChia");

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
            // Load MDS2Engine channel TCPClient class parameters from <configSet name="[Channel:GW2Internal]PALS.Net.Transports.TCP.TCPClient"> XMLNode
            // -------------------------------------------------------------------------------
            //<configSet name="[Channel:GW2Internal]PALS.Net.Transports.TCP.TCPClient">
            //  <threadInterval>10</threadInterval>
            //  <isAutoReconnect>True</isAutoReconnect>
            //  <reconnectTimeout>20000</reconnectTimeout>
            //  <!--Maximum length of name is 1~8 characters-->
            //  <!--MDS2CCTVGW Svr IP: ?, CCTV Svr IP: ? -->
            //  <localNode name="MDSGW" ip="127.0.0.1" port="0"/>
            //  <remoteNodes>
            //    <!--SocketConnector object is able to connect to multiple remote TCP servers-->
            //    <!--If there are more than one TCP server, just add following server element accordingly-->
            //    <!--Maximum length of name is 8 characters-->
            //    <server name="CCTVEngn" ip="127.0.0.1" port="24000"/>
            //  </remoteNodes>
            //</configSet>
            // -------------------------------------------------------------------------------
            node = null;
            node = XMLConfig.GetConfigSetElement(ref rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_GW2INTERNAL_TCPCLIENT);
            if (node != null)
            {
                // Declare a temporary parameter class object
                PALS.Common.IParameters tempParam;

                // Read settings from particular <configSet> by constructor of parameter class object.
                tempParam = new PALS.Net.Transports.TCP.TCPClientParameters(ref node);

                // Assign temporary parameter object reference to global parameter object 
                if (tempParam != null)
                {
                    Paramters_TCPClient_GW2Internal = tempParam;
                }
                else
                    throw new Exception("Reading settings from ConfigSet <configSet name=\"" +
                            XML_CONFIGSET_GW2INTERNAL_TCPCLIENT + "\"> is failed!");
            }
            else
                throw new Exception("ConfigSet <configSet name=\"" + XML_CONFIGSET_GW2INTERNAL_TCPCLIENT + 
                            "\"> is not found in the XML file.");

#if DEBUG
            // Start of debugging codes.
            if (_logger.IsDebugEnabled)
            {
                PALS.Net.Transports.TCP.TCPClientParameters param = 
                        (PALS.Net.Transports.TCP.TCPClientParameters)Paramters_TCPClient_GW2Internal;

                _logger.Debug(string.Format("[Param: Paramters_TCPClient_GW2Internal] LocalNode={0}, " +
                        "#ofRemoteNode={1}, IsAutoReconnected={2}, ReconnectTimeout={3}",
                        param.LocalNode.ToString(), param.RemoteNodeHash.Count,
                        param.IsAutoReconnected, param.ReconnectTimeout));
            }
            // End of debugging codes.
#endif
            // -------------------------------------------------------------------------------

            // -------------------------------------------------------------------------------
            // Load GW2External channel TCPClient class parameters from <configSet name="[Channel:GW2External]PALS.Net.Transports.TCP.TCPServerClient"> XMLNode
            // -------------------------------------------------------------------------------
            //<configSet name="[Channel:GW2External]PALS.Net.Transports.TCP.TCPServerClient">
            //  <!-- ChannelPath defines the direction of message transmission.  -->
            //  <!-- 0: via TCP Server channel, 1: via TCP Client channgel, 2: both Server & Client channels can have own in & out messages-->
            //  <incomingChannelPath>2</incomingChannelPath>
            //  <outgoingChannelPath>2</outgoingChannelPath>
            //  <configSet name="TCPServer">
            //    <threadInterval>10</threadInterval>
            //    <!-- SAC Server 1 IP: 192.168.100.33, SAC Server 2 IP: 192.168.100.35, PLC01 IP: 192.168.100.91, PLC02 IP: 192.168.100.93, 
            //    PLC03 IP: 192.168.100.95, PLC04 IP: 192.168.100.97, PLC06 IP: 192.168.100.101 -->
            //    <localNode name="SAC2PLC1" ip="192.168.100.33" port="44810"/>
            //    <!--The minimum allowed client connections must be 1, only one remote TCP client is allowed to connect to current SAC2PLCGW service.-->
            //    <maxConnections>1</maxConnections>
            //  </configSet>
            //  <configSet name="TCPClient">
            //    <threadInterval>10</threadInterval>
            //    <isAutoReconnect>True</isAutoReconnect>
            //    <reconnectTimeout>20000</reconnectTimeout>
            //    <!--Maximum length of name is 1~8 characters-->
            //    <localNode name="SAC2PLC1" ip="192.168.100.33" port="0"/>
            //    <remoteNodes>
            //      <!--Maximum length of name is 8 characters-->
            //      <server name="PLC01" ip="192.168.100.91" port="44818"/>
            //    </remoteNodes>
            //  </configSet>
            //</configSet>
            // -------------------------------------------------------------------------------
            node = null;
            node = XMLConfig.GetConfigSetElement(ref rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_GW2EXTERNAL_TCPSERVERCLIENT);
            if (node != null)
            {
                // Declare a temporary parameter class object
                PALS.Common.IParameters tempParam;

                // Read settings from particular <configSet> by constructor of parameter class object.
                tempParam = new PALS.Net.Transports.TCP.TCPServerClientParameters(ref node);

                // Assign temporary parameter object reference to global parameter object 
                if (tempParam != null)
                    Paramters_TCPServerClient_GW2External = tempParam;
                else
                    throw new Exception("Reading settings from ConfigSet <configSet name=\"" +
                            XML_CONFIGSET_GW2EXTERNAL_TCPSERVERCLIENT + "\"> is failed!");
            }
            else
                throw new Exception("ConfigSet <configSet name=\"" + XML_CONFIGSET_GW2EXTERNAL_TCPSERVERCLIENT + 
                            "\"> is not found in the XML file.");

#if DEBUG
            // Start of debugging codes.
            if (_logger.IsDebugEnabled)
            {
                PALS.Net.Transports.TCP.TCPServerClientParameters param =
                        (PALS.Net.Transports.TCP.TCPServerClientParameters)Paramters_TCPServerClient_GW2External;

                System.Text.StringBuilder msg = new System.Text.StringBuilder();
              

                foreach (System.Collections.DictionaryEntry de in param.TCPClientParam.RemoteNodeHash)
                {
                    msg.Append(de.Key);
                    msg.Append(", ");
                }

                _logger.Debug(string.Format("[Param: Paramters_TCPServerClient_GW2External] incomingChannelPath={0}, " +
                        "outgoingChannelPath={1}, TCPServer.threadInterval={2}, TCPServer.localNode.name={3}, " +
                        "TCPServer.localNode.ip={4}, TCPServer.localNode.port={5}, TCPServer.maxConnections={6}, " +
                        "TCPClient.threadInterval={7}, TCPClient.isAutoReconnect={8}, " +
                        "TCPClient.reconnectTimeout={9}, TCPClient.localNode.name={10}, TCPClient.localNode.ip={11}, " +
                        "TCPClient.localNode.port={12}, TCPClient.remoteNodes={13}" ,
                        param.IncomingChannelPath.ToString(), param.OutgoingChannelPath.ToString(), param.TCPServerParam.ThreadInterval.ToString(),
                        param.TCPServerParam.LocalNode.Name, param.TCPServerParam.LocalNode.IP.ToString(), param.TCPServerParam.LocalNode.Port.ToString(),
                        param.TCPServerParam.MaxConnections.ToString(),param.TCPClientParam.ThreadInterval.ToString(),
                        param.TCPClientParam.IsAutoReconnected.ToString(),param.TCPClientParam.ReconnectTimeout.ToString(),
                        param.TCPClientParam.LocalNode.Name, param.TCPClientParam.LocalNode.IP.ToString(), 
                        param.TCPClientParam.LocalNode.Port.ToString(), msg.ToString()));
            }
            // End of debugging codes.
#endif
            // -------------------------------------------------------------------------------

            // -------------------------------------------------------------------------------
            // Load EIP class parameters from <configSet name="PALS.Net.Filters.EIPCIP.EIP"> XMLNode
            // -------------------------------------------------------------------------------
            //<configSet name="PALS.Net.Filters.EIPCIP.EIP">
            //  <!-- Socket connection will be closed if -
            //       1) no RegisterSession Request received TCP connection has been opened for 5sec;
            //       2) no RegisterSession Reply returned from remote after Request has been sent for 5sec.-->
            //  <registerSessionTimeout>5000</registerSessionTimeout>
            //  <!-- SenderContext = SAC2PLC1, Array of 8 USINT -->
            //  <senderContext>83,65,67,50,80,76,67,49</senderContext>
            //  <!-- Success status value = 0x0000, 4-byte integer -->
            //  <successStatus>0,0,0,0</successStatus>
            //  <!-- Unsupported EIP command status value = 0x0001 -->
            //  <unsupportedCmdStatus>0,0,0,1</unsupportedCmdStatus>
            //  <maximumDataFieldLength>1024</maximumDataFieldLength>
            //</configSet>
            // -------------------------------------------------------------------------------
            node = null;
            node = XMLConfig.GetConfigSetElement(ref rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_EIP);
            if (node != null)
            {
                // Declare a temporary parameter class object
                PALS.Common.IParameters tempParam;

                // Read settings from particular <configSet> by constructor of parameter class object.
                tempParam = new PALS.Net.Filters.EIPCIP.EIPParameters(ref node, ref nodeTele);

                // Assign temporary parameter object reference to global parameter object 
                if (tempParam != null)
                    Paramters_EIP = tempParam;
                else
                    throw new Exception("Reading settings from ConfigSet <configSet name=\"" +
                            XML_CONFIGSET_EIP + "\"> is failed!");
            }
            else
                throw new Exception("ConfigSet <configSet name=\"" + XML_CONFIGSET_EIP +
                            "\"> is not found in the XML file.");

#if DEBUG
            // Start of debugging codes.
            if (_logger.IsDebugEnabled)
            {
                PALS.Net.Filters.EIPCIP.EIPParameters param =
                        (PALS.Net.Filters.EIPCIP.EIPParameters)Paramters_EIP;

                _logger.Debug(string.Format("[Param: Paramters_EIP] registerSessionTimeout={0}, " +
                        "senderContext={1}, successStatus={2}, unsupportedCmdStatus={3}, maximumDataFieldLength={4}",
                        param.RegisterSessionTimeout.ToString(), Functions.ConvertByteArrayToString(param.SenderContext,-1,HexToStrMode.ToAscString),
                        param.EIPStatus_Success_Int32.ToString(), param.EIPStatus_UnsupportedCmd_Int32.ToString(), 
                        param.MaxEIPDataFieldLength.ToString()));
            }
            // End of debugging codes.
#endif
            // -------------------------------------------------------------------------------

            // -------------------------------------------------------------------------------
            // Load CIP class parameters from <configSet name="PALS.Net.Filters.EIPCIP.CIP"> XMLNode
            // -------------------------------------------------------------------------------
            //<configSet name="PALS.Net.Filters.EIPCIP.CIP">
            //  <!-- outgoingQueueCapacity: the capacity of CIP class internal outgoing queue to buffer Table Write 
            //       Request messages. The next TWRQ message needs to be buffered in the queue to wait for TWRS (Response)
            //       of last TWRQ message before it can be sent out. Default: 100  -->
            //  <outgoingQueueCapacity>100</outgoingQueueCapacity>
            //  <!-- EIP and Socket connection will be closed if -
            //       1) no Fwd_Open Request received after EIP connection has been opened for 5sec;
            //       2) no Fwd_Open Response returned from remote after Request has been sent for 5sec.-->
            //  <cipConnectionTimeout>5000</cipConnectionTimeout>
            //  <!-- retriesForResponse: Resend times of CIP Connected message before close the CIP connection
            //       because no response is returned from Target. Default: 1 -->
            //  <retriesForResponse>3</retriesForResponse>
            //  <!-- Success value of CIP General Status = 0x00, 1 byte integer -->
            //  <successStatus>0</successStatus>
            //  <!-- tableWriteSymbolPath: The default Tag name defined in the remote node to receive the 
            //       data table write messages from SAC2PLCGW Service. SAC-COM1: TAG_PLC1, SAC-COM2: TAG_PLC2 -->
            //  <tableWriteSymbolPath>TAG_PLC1</tableWriteSymbolPath>
            //  <!-- If SAC send message to PLC too fast, PLC could miss receiving some of them. Hence, the 
            //       message sending delay is implemented in SAC and it can be enabled by setting below. -->
            //  <enableSendingDelay>True</enableSendingDelay>
            //  <!-- the CIP Data Table Write Request sending delay time in milisecond. its value should be within 10~100ms. Default is 10ms. -->
            //  <sendingDelayTime>10</sendingDelayTime>
            //  <!-- the CIP Data Table Write Response sending delay time in milisecond. its value should be within 0~100ms. Default is 50ms. -->
            //  <sendingCIPResponseDelayTime>50</sendingCIPResponseDelayTime>
            //</configSet>
            // -------------------------------------------------------------------------------

            node = null;
            node = XMLConfig.GetConfigSetElement(ref rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_CIP);
            if (node != null)
            {
                // Declare a temporary parameter class object
                PALS.Common.IParameters tempParam;

                // Read settings from particular <configSet> by constructor of parameter class object.
                tempParam = new PALS.Net.Filters.EIPCIP.CIPParameters(ref node, ref nodeTele);

                // Assign temporary parameter object reference to global parameter object 
                if (tempParam != null)
                    Paramters_CIP = tempParam;
                else
                    throw new Exception("Reading settings from ConfigSet <configSet name=\"" +
                            XML_CONFIGSET_CIP + "\"> is failed!");
            }
            else
                throw new Exception("ConfigSet <configSet name=\"" + XML_CONFIGSET_CIP +
                            "\"> is not found in the XML file.");

#if DEBUG
            // Start of debugging codes.
            if (_logger.IsDebugEnabled)
            {
                PALS.Net.Filters.EIPCIP.CIPParameters param =
                        (PALS.Net.Filters.EIPCIP.CIPParameters)Paramters_CIP;

                _logger.Debug(string.Format("[Param: Paramters_CIP] outgoingQueueCapacity={0}, " +
                        "cipConnectionTimeout={1}, retriesForResponse={2}, successStatus={3}, tableWriteSymbolPath={4}," +
                        "enableSendingDelay={5}, sendingDelayTime={6}, sendingCIPResponseDelayTime={7}",
                        param.OutgoingQueueCapacity.ToString(), param.CIPConnectionTimeout.ToString(),
                        param.RetriesForResponse.ToString(), param.CIPStatus_Success_Int.ToString(),
                        param.CIPTableWriteSymbolPath, param.EnableSendingDelay.ToString(), param.SendingDelayTime.ToString(),
                        param.SendingCIPResponseDelayTime.ToString()));
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
            // Load AppClient class parameters from <configSet name="[Channel:GW2Internal]PALS.Net.Filters.Application.AppClient"> XMLNode
            // -------------------------------------------------------------------------------
            //<configSet name="[Channel:GW2Internal]PALS.Net.Filters.Application.AppClient">
            //  <threadInterval>100</threadInterval>
            //  <!--Maximum length of clientAppCode is 8 characters-->
            //  <clientAppCode>CCTVGW</clientAppCode>
            //  <!--connectionConfirmTimeout value must bigger than the same parameter of bottom layer (RFC1006)-->
            //  <connectionConfirmTimeout>3000</connectionConfirmTimeout>
            //  <connectionRequestRetries>3</connectionRequestRetries>
            //  <minSequenceNo>1</minSequenceNo>
            //  <maxSequenceNo>9999</maxSequenceNo>
            //</configSet>
            // -------------------------------------------------------------------------------
            node = null;
            node = XMLConfig.GetConfigSetElement(ref rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_GW2INTERNAL_APPCLIENT);
            if (node != null)
            {
                // Declare a temporary parameter class object
                PALS.Common.IParameters tempParam;

                // Read settings from particular <configSet> by constructor of parameter class object.
                tempParam = new PALS.Net.Filters.Application.AppClientParameters(ref node, ref nodeTele);

                // Assign temporary parameter object reference to global parameter object 
                if (tempParam != null)
                    Paramters_AppClient_GW2Internal = tempParam;
                else
                    throw new Exception("Reading settings from ConfigSet <configSet name=\"" +
                            XML_CONFIGSET_GW2INTERNAL_APPCLIENT + "\"> is failed!");
            }
            else
                throw new Exception("ConfigSet <configSet name=\"" + XML_CONFIGSET_GW2INTERNAL_APPCLIENT +
                            "\"> is not found in the XML file.");

#if DEBUG
            // Start of debugging codes.
            if (_logger.IsDebugEnabled)
            {
                PALS.Net.Filters.Application.AppClientParameters param =
                        (PALS.Net.Filters.Application.AppClientParameters)Paramters_AppClient_GW2Internal;

                _logger.Debug(string.Format("[Param: Paramters_AppClient_GW2Internal] ThreadInterval={0}, AppCode={1}" +
                    "CCFTimeout={2}, CRQRetries={3}, MinSequenceNo={4}, MaxSequenceNo={5}",
                    param.ThreadInterval.ToString(), param.AppCode, param.CCFTimeout.ToString(),param.CRQRetries.ToString(),
                    param.MinSequenceNo.ToString(), param.MaxSequenceNo.ToString()));
            }
            // End of debugging codes.
#endif
            // -------------------------------------------------------------------------------

            // -------------------------------------------------------------------------------
            // Load AppClient class parameters from <configSet name="[Channel:GW2External]PALS.Net.Filters.Application.AppClient"> XMLNode
            // -------------------------------------------------------------------------------
            //<configSet name="[Channel:GW2External]PALS.Net.Filters.Application.AppClient">
            //  <threadInterval>100</threadInterval>
            //  <!--Maximum length of clientAppCode is 8 characters-->
            //  <clientAppCode>MDS</clientAppCode>
            //  <!--connectionConfirmTimeout value must bigger than the same parameter of bottom layer (RFC1006)-->
            //  <connectionConfirmTimeout>3000</connectionConfirmTimeout>
            //  <connectionRequestRetries>3</connectionRequestRetries>
            //  <minSequenceNo>1</minSequenceNo>
            //  <maxSequenceNo>9999</maxSequenceNo>
            //</configSet>
            // -------------------------------------------------------------------------------
            node = null;
            node = XMLConfig.GetConfigSetElement(ref rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_GW2EXTERNAL_APPCLIENT);
            if (node != null)
            {
                // Declare a temporary parameter class object
                PALS.Common.IParameters tempParam;

                // Read settings from particular <configSet> by constructor of parameter class object.
                tempParam = new PALS.Net.Filters.Application.AppClientParameters(ref node, ref nodeTele);

                // Assign temporary parameter object reference to global parameter object 
                if (tempParam != null)
                    Paramters_AppClient_GW2External = tempParam;
                else
                    throw new Exception("Reading settings from ConfigSet <configSet name=\"" +
                            XML_CONFIGSET_GW2EXTERNAL_APPCLIENT + "\"> is failed!");
            }
            else
                throw new Exception("ConfigSet <configSet name=\"" + XML_CONFIGSET_GW2EXTERNAL_APPCLIENT +
                            "\"> is not found in the XML file.");

#if DEBUG
            // Start of debugging codes.
            if (_logger.IsDebugEnabled)
            {
                PALS.Net.Filters.Application.AppClientParameters param =
                        (PALS.Net.Filters.Application.AppClientParameters)Paramters_AppClient_GW2External;

                _logger.Debug(string.Format("[Param: Paramters_AppClient_GW2External] ThreadInterval={0}, AppCode={1}" +
                    "CCFTimeout={2}, CRQRetries={3}, MinSequenceNo={4}, MaxSequenceNo={5}",
                    param.ThreadInterval.ToString(), param.AppCode, param.CCFTimeout.ToString(), param.CRQRetries.ToString(),
                    param.MinSequenceNo.ToString(), param.MaxSequenceNo.ToString()));
            }
            // End of debugging codes.
#endif
            // -------------------------------------------------------------------------------

            // -------------------------------------------------------------------------------
            // Load TimeSync class parameters from <configSet name="PALS.Net.Filters.TimeSynchronizing.TimeSync"> XMLNode
            // -------------------------------------------------------------------------------
            //<configSet name="PALS.Net.Filters.TimeSynchronizing.TimeSync">
            //  <threadInterval>100</threadInterval>
            //  <timeSyncInterval>3600000</timeSyncInterval>
            //  <!-- y->year, M->Month, d->Day, h->Hour, m->Min, s->Sec, f->milisec-->
            //  <dateTimeFormat>yyyyMMdd-hhmmssfff</dateTimeFormat>
            //</configSet>
            node = null;
            node = XMLConfig.GetConfigSetElement(ref rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_TSYN);
            if (node != null)
            {
                // Declare a temporary parameter class object
                PALS.Common.IParameters tempParam;

                // Read settings from particular <configSet> by constructor of parameter class object.
                tempParam = new PALS.Net.Filters.TimeSynchronizing.TimeSyncParameters(ref node, ref nodeTele);

                // Assign temporary parameter object reference to global parameter object 
                if (tempParam != null)
                    Paramters_TSYN = tempParam;
                else
                    throw new Exception("Reading settings from ConfigSet <configSet name=\"" +
                            XML_CONFIGSET_TSYN + "\"> is failed!");
            }
            else
                throw new Exception("ConfigSet <configSet name=\"" + XML_CONFIGSET_TSYN +
                            "\"> is not found in the XML file.");

#if DEBUG
            // Start of debugging codes.
            if (_logger.IsDebugEnabled)
            {
                PALS.Net.Filters.TimeSynchronizing.TimeSyncParameters param =
                        (PALS.Net.Filters.TimeSynchronizing.TimeSyncParameters)Paramters_TSYN;

                _logger.Debug(string.Format("[Param: Paramters_TSYN] threadInterval={0}, timeSyncInterval={1}, dateTimeFormat={2}",
                        param.ThreadInterval.ToString(), param.TimeSyncInterval.ToString(),param.DateTimeFormat));
            }
            // End of debugging codes.
#endif
            // -------------------------------------------------------------------------------


            // -------------------------------------------------------------------------------
            // Load SOL class parameters from <configSet name="[Channel:GW2Internal]PALS.Net.Filters.SignOfLife.SOL"> XMLNode
            // -------------------------------------------------------------------------------
            //<configSet name="[Channel:GW2Internal]PALS.Net.Filters.SignOfLife.SOL">
            //  <threadInterval>100</threadInterval>
            //  <solSendTimeout>10000</solSendTimeout>
            //  <solReceiveTimeout>25000</solReceiveTimeout>
            //</configSet>
            // -------------------------------------------------------------------------------
            node = null;
            node = XMLConfig.GetConfigSetElement(ref rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_GW2INTERNAL_SOL);
            if (node != null)
            {
                // Declare a temporary parameter class object
                PALS.Common.IParameters tempParam;

                // Read settings from particular <configSet> by constructor of parameter class object.
                tempParam = new PALS.Net.Filters.SignOfLife.SOLParameters(ref node, ref nodeTele);

                // Assign temporary parameter object reference to global parameter object 
                if (tempParam != null)
                    Paramters_SOL_GW2Internal = tempParam;
                else
                    throw new Exception("Reading settings from ConfigSet <configSet name=\"" +
                            XML_CONFIGSET_GW2INTERNAL_SOL + "\"> is failed!");
            }
            else
                throw new Exception("ConfigSet <configSet name=\"" + XML_CONFIGSET_GW2INTERNAL_SOL +
                            "\"> is not found in the XML file.");

#if DEBUG
            // Start of debugging codes.
            if (_logger.IsDebugEnabled)
            {
                PALS.Net.Filters.SignOfLife.SOLParameters param =
                        (PALS.Net.Filters.SignOfLife.SOLParameters)Paramters_SOL_GW2Internal;

                _logger.Debug(string.Format("[Param: Paramters_SOL_GW2Internal] ThreadInterval={0}, SOLReceiveTimeout={1}, SOLSendTimeout={2}",
                        param.ThreadInterval, param.SOLReceiveTimeout, param.SOLSendTimeout));
            }
            // End of debugging codes.
#endif
            // -------------------------------------------------------------------------------

            // -------------------------------------------------------------------------------
            // Load SOL class parameters from <configSet name="[Channel:GW2External]PALS.Net.Filters.SignOfLife.SOL"> XMLNode
            // -------------------------------------------------------------------------------
            //<configSet name="[Channel:GW2External]PALS.Net.Filters.SignOfLife.SOL">
            //  <threadInterval>100</threadInterval>
            //  <solSendTimeout>10000</solSendTimeout>
            //  <solReceiveTimeout>25000</solReceiveTimeout>
            //</configSet>
            // -------------------------------------------------------------------------------
            node = null;
            node = XMLConfig.GetConfigSetElement(ref rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_GW2EXTERNAL_SOL);
            if (node != null)
            {
                // Declare a temporary parameter class object
                PALS.Common.IParameters tempParam;

                // Read settings from particular <configSet> by constructor of parameter class object.
                tempParam = new PALS.Net.Filters.SignOfLife.SOLParameters(ref node, ref nodeTele);

                // Assign temporary parameter object reference to global parameter object 
                if (tempParam != null)
                    Paramters_SOL_GW2External = tempParam;
                else
                    throw new Exception("Reading settings from ConfigSet <configSet name=\"" +
                            XML_CONFIGSET_GW2EXTERNAL_SOL + "\"> is failed!");
            }
            else
                throw new Exception("ConfigSet <configSet name=\"" + XML_CONFIGSET_GW2EXTERNAL_SOL +
                            "\"> is not found in the XML file.");

#if DEBUG
            // Start of debugging codes.
            if (_logger.IsDebugEnabled)
            {
                PALS.Net.Filters.SignOfLife.SOLParameters param =
                        (PALS.Net.Filters.SignOfLife.SOLParameters)Paramters_SOL_GW2External;

                _logger.Debug(string.Format("[Param: Paramters_SOL_GW2External] ThreadInterval={0}, SOLReceiveTimeout={1}, SOLSendTimeout={2}",
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

                _logger.Debug(string.Format("[Param: Paramters_ACK] ThreadInterval={0}, RetransmitBufferSize={1}, RetransmitTimeout={2}, RetransmitRetries={3}",
                        param.ThreadInterval, param.RetransmitBufferSize, param.RetransmitTimeout, param.RetransmitRetries));
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

                foreach (System.Collections.DictionaryEntry de in param.MessageFormatHash )
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
            // <configSet name="BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.MessageHandler">
            //   <!-- INTM message (local) sender, max 8 characters-->
            //   <sender>MDSGW</sender>
            //   <!-- INTM message (remote) receiver, max 8 characters-->
            //   <receiver>CCTVENGN</receiver>
            // </configSet>
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
                            XML_CONFIGSET_ACK + "\"> is failed!");
            }
            else
                throw new Exception("ConfigSet <configSet name=\"" + XML_CONFIGSET_ACK +
                            "\"> is not found in the XML file.");

#if DEBUG
            // Start of debugging codes.
            if (_logger.IsDebugEnabled)
            {
                Messages.Handlers.MessageHandlerParameters param =
                        (Messages.Handlers.MessageHandlerParameters)Paramters_MsgHandler;

                _logger.Debug(string.Format("[Param: Paramters_MsgHandler] Type_INTM={0}, " + 
                        "Type_SRQ={1}, Type_SRP={2}, Sender={3}, Receiver={4}",
                        param.MessageType_INTM, param.MessageType_SRQ, param.MessageType_SRP,
                        param.Sender, param.Receiver));
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
