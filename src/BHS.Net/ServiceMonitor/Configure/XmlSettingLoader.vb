'=====================================================================================
' Copyright 2009, Xu Jian, All Rights Reserved.
'=====================================================================================
'FileName       XmlSettingLoader.vb
'Revision:      1.0 -   13 Mar 2009, By hs chia, For Winnipeg International Airport BHS project.
'=====================================================================================

Option Explicit On
Option Strict On

Imports System
Imports System.IO
Imports System.Xml
Imports PALS.Configure
Imports PALS.Utilities


Namespace ServiceMonitor.Configure

    Public Class XmlSettingLoader
        Implements PALS.Configure.IConfigurationLoader
        Implements IDisposable

#Region "1. Class Fields Declaration."
        ''' there are total 3 XML configuration files required by SortEng application: 
        ''' CFG_SortEngine.xml - application settings 
        ''' CFG_Telegrams.xml - application telegram format definations.
        ''' CFG_BHSConfig.xml - application Public Parameter Settings.
        Private Const DESIRED_NUMBER_OF_CFG_FILES As Integer = 2

        ''' XMLNode name of configuration sets.
        Private Const XML_CONFIGSET As String = "configSet"
        Private Const XML_CONFIGSET_APPINITIALIZER As String = "BHS.ServiceMonitor.Application.Initializer"
        'Private Const CONFIGSET_MESSAGEHANDLER As String = "BHS.ServiceMonitor.Application.MessageHandler"
        Private Const XML_CONFIGSET_MONITORING As String = "BHS.ServiceMonitor.Monitoring.Monitor"

        Private Const XML_CONFIGSET_TCPSERVER As String = "PALS.Net.Transports.TCP.TCPServer"
        Private Const XML_CONFIGSET_FRAME As String = "PALS.Net.Filters.Frame.Frame"
        Private Const XML_CONFIGSET_APPSERVER As String = "PALS.Net.Filters.Application.AppServer"
        Private Const XML_CONFIGSET_SOL As String = "PALS.Net.Filters.SignOfLife.SOL"
        Private Const XML_CONFIGSET_TELEGRAM_FORMAT As String = "Telegram_Formats"



        ''' The name of current class 
        Private Shared ReadOnly _className As String = _
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString
        ''' Create a logger for use in this class
        Private Shared ReadOnly _logger As log4net.ILog = _
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType)

        Private _globalContext As GlobalContext
        Private _tCPServer As PALS.Common.IParameters
        Private _frame As PALS.Common.IParameters
        Private _appServer As PALS.Common.IParameters
        Private _sOL As PALS.Common.IParameters
        Private _aCK As PALS.Common.IParameters
        Private _serverMsgForwarder As PALS.Common.IParameters
        Private _mID As PALS.Common.IParameters
        Private _msgHdlr As PALS.Common.IParameters
        Private _monitor As PALS.Common.IParameters

#End Region

#Region "2. Class Constructor and Destructor Declaration."
        Public Sub Dispose() Implements IDisposable.Dispose
            Dim thisMethod As String = _className & "." & _
                System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Try
                If _logger.IsInfoEnabled Then
                    _logger.Info("Class:[" & _className & "] object is going to be destroyed... <" & _
                            _className & ".Dispose()>")
                End If

                ' Destory class level fields.
                If (Not Parameters_GlobalContext Is Nothing) Then
                    Parameters_GlobalContext = Nothing
                End If

                If (Not Parameters_TCPServer Is Nothing) Then
                    Parameters_TCPServer = Nothing
                End If

                If (Not Parameters_Frame Is Nothing) Then
                    Parameters_Frame = Nothing
                End If

                If (Not Parameters_AppServer Is Nothing) Then
                    Parameters_AppServer = Nothing
                End If

                If (Not Parameters_SOL Is Nothing) Then
                    Parameters_SOL = Nothing
                End If

                If (Not Parameters_ACK Is Nothing) Then
                    Parameters_ACK = Nothing
                End If

                If (Not Parameters_Monitor Is Nothing) Then
                    Parameters_Monitor = Nothing
                End If

                If (Not Parameters_ServerMsgForwarder Is Nothing) Then
                    Parameters_ServerMsgForwarder = Nothing
                End If

                If (Not Parameters_MID Is Nothing) Then
                    Parameters_MID = Nothing
                End If

                If (Not Parameters_MsgHdlr Is Nothing) Then
                    Parameters_MsgHdlr = Nothing
                End If


                If _logger.IsInfoEnabled Then
                    _logger.Info("Class:[" & _className & "] object has been destroyed. <" & _
                            _className & ".Dispose()>")
                End If
            Catch ex As Exception
                If _logger.IsErrorEnabled Then
                    _logger.Error("Class:[" & _className & "] System Error! <" & _
                            _className & ".Dispose()> Exception: Source = " & ex.Source & _
                            " | Type : " & ex.GetType.ToString & " | Message : " & ex.Message)
                End If
            End Try


        End Sub 'Dispose
#End Region


#Region "3. Class Properties Defination."
        ''' AppInitializer parameter classes variables for storing application settings loaded from configuration file.
        ''' In order to prevent the overwriting the existing system settings stored in the gloabl parameter variables  
        ''' due to the failure of reloading configuration file, the loaded parameters shall be stored into
        ''' the temporary variables and only assign to global parameter variables is the loading successed.

        Public Property Parameters_GlobalContext() As GlobalContext
            Get
                Return _globalContext
            End Get
            Set(ByVal Value As GlobalContext)
                _globalContext = Value
            End Set
        End Property

        Public Property Parameters_TCPServer() As PALS.Common.IParameters
            Get
                Return _tCPServer
            End Get
            Set(ByVal Value As PALS.Common.IParameters)
                _tCPServer = Value
            End Set
        End Property

        Public Property Parameters_Frame() As PALS.Common.IParameters
            Get
                Return _frame
            End Get
            Set(ByVal Value As PALS.Common.IParameters)
                _frame = Value
            End Set
        End Property

        Public Property Parameters_AppServer() As PALS.Common.IParameters
            Get
                Return _appServer
            End Get
            Set(ByVal Value As PALS.Common.IParameters)
                _appServer = Value
            End Set
        End Property

        Public Property Parameters_SOL() As PALS.Common.IParameters
            Get
                Return _sOL
            End Get
            Set(ByVal Value As PALS.Common.IParameters)
                _sOL = Value
            End Set
        End Property

        Public Property Parameters_ACK() As PALS.Common.IParameters
            Get
                Return _aCK
            End Get
            Set(ByVal Value As PALS.Common.IParameters)
                _aCK = Value
            End Set
        End Property

        Public Property Parameters_Monitor() As PALS.Common.IParameters
            Get
                Return _monitor
            End Get
            Set(ByVal Value As PALS.Common.IParameters)
                _monitor = Value
            End Set
        End Property

        Public Property Parameters_ServerMsgForwarder() As PALS.Common.IParameters
            Get
                Return _serverMsgForwarder
            End Get
            Set(ByVal Value As PALS.Common.IParameters)
                _serverMsgForwarder = Value
            End Set
        End Property

        Public Property Parameters_MID() As PALS.Common.IParameters
            Get
                Return _mID
            End Get
            Set(ByVal Value As PALS.Common.IParameters)
                _mID = Value
            End Set
        End Property

        Public Property Parameters_MsgHdlr() As PALS.Common.IParameters
            Get
                Return _msgHdlr
            End Get
            Set(ByVal Value As PALS.Common.IParameters)
                _msgHdlr = Value
            End Set
        End Property
#End Region

#Region "5. Class Method Declaration."

#End Region

#Region "6. IConfigurationLoader Members"
        ''' <summary>
        ''' This class method is the place to centralize the loading of application settings from 
        ''' configuration file. 
        ''' <para>
        ''' The actual implementation of IConfigurationLoader interface method LoadSettingFromConfigFile(). 
        ''' This method will be invoked by AppConfigurator class.
        ''' </para>
        ''' <para>
        ''' If the parameter isReloading = true, the interface implemented LoadSettingFromConfigFile() 
        ''' may raise a event after all settings have been reloaded successfully, to inform application 
        ''' that the reloading setting has been done. So application can take the necessary actions
        ''' to take effective of new settings.
        ''' </para>
        ''' <para>
        ''' Decode XML configuration file and load application settings shall be done by this method.
        ''' </para>
        ''' </summary>
        ''' <param name="isReloading">
        ''' If the parameter isReloading = true, the interface implemented LoadSettingFromConfigFile() 
        ''' may raise a event after all settings have been reloaded successfully, to inform application 
        ''' that the reloading setting has been done. So application can take the necessary actions
        ''' to take effective of new settings.
        ''' </param>
        ''' <param name="cfgFiles">
        ''' params type method argument, represents one or more configuration files.
        ''' </param>

        Public Sub LoadSettingFromConfigFile(ByVal isReloading As Boolean, ByVal ParamArray cfgFiles() As System.IO.FileInfo) Implements PALS.Configure.IConfigurationLoader.LoadSettingFromConfigFile
            Dim thisMethod As String = _className & "." & _
                System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            If (cfgFiles.Length <> DESIRED_NUMBER_OF_CFG_FILES) Then
                Throw New Exception("The number of files (" & cfgFiles.Length & _
                    ") passed to configuration loader is not desired number (" & DESIRED_NUMBER_OF_CFG_FILES & ").")
            End If

            If _logger.IsInfoEnabled Then
                _logger.Info("Class:[" & _className & "] Loading application settings... <" & _
                            thisMethod & ">")
            End If

            '  Get the root elements of XML file: CFG_SortEngine.xml, CFG_Telegrams.xml & CFG_BHSConfig.xml.
            Dim rootSetting As XmlElement
            Dim rootTelegram As XmlElement
            Dim node As XmlNode
            Dim nodeTele As XmlNode

            rootSetting = XMLConfig.GetConfigFileRootElement(cfgFiles(0))
            If (rootSetting Is Nothing) Then
                Throw New Exception("Get root XmlElement failure! [Xml File: " & cfgFiles(0).FullName & "].")
            End If

            rootTelegram = XMLConfig.GetConfigFileRootElement(cfgFiles(1).FullName)
            If (rootTelegram Is Nothing) Then
                Throw New Exception("Get root XmlElement failure! [Xml File: " & cfgFiles(1).FullName & "].")
            End If

            nodeTele = XMLConfig.GetConfigSetElement(rootTelegram, XML_CONFIGSET, "name", XML_CONFIGSET_TELEGRAM_FORMAT)
            If (nodeTele Is Nothing) Then
                Throw New Exception("ConfigSet <configSet name=" & XML_CONFIGSET_TELEGRAM_FORMAT & _
                            "> is not found in the XML file.")
            End If

            If (_logger.IsInfoEnabled) Then
                _logger.Info("Loading application settings from configuration file(s): " & cfgFiles(0).FullName & ", " & _
                             cfgFiles(1).FullName & ". <" + thisMethod & ">")
            End If


            ' -------------------------------------------------------------------------------
            ' Load AppInitializer settings from 	<configSet name="BHS.ServiceMonitor.Application.Initializer"> XmlNode()
            ' -------------------------------------------------------------------------------
            '<!--Configuration Parameters that are divided into different configSet-->
            '<configSet name="BHS.SortEngine.Application.Initializer">
            '  <!--Generate Application Information-->
            '  <company>PterisGlobal</company>
            '  <department>CSI</department>
            '  <author>HSChia</author>
            '</configSet>
            ' -------------------------------------------------------------------------------
            ' Description: In order to prevent the overwriting the existing system settings 
            ' stored in the gloabl variables due to the failure of reloading configuration
            ' file, the loaded parameters shall be stored into the temporary variables and 
            ' only assign to global variables is the loading successed.
            node = XMLConfig.GetConfigSetElement(rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_APPINITIALIZER)
            If (node Is Nothing) Then
                Throw New Exception("ConfigSet <" & XML_CONFIGSET_APPINITIALIZER & "> is not found in the XML file.")
            Else
                ' Declare a temporary parameter class object
                Dim tempParam1 As GlobalContext = New GlobalContext()

                tempParam1.AppStartedTime = Now
                tempParam1.Company = XMLConfig.GetSettingFromInnerText(node, "company", "PterisGlobal")
                tempParam1.Department = XMLConfig.GetSettingFromInnerText(node, "department", "CSI")
                tempParam1.Author = XMLConfig.GetSettingFromInnerText(node, "author", "HSChia")

                ' Assign temporary parameter object reference to initializer parameter object 
                If (Not tempParam1 Is Nothing) Then
                    Parameters_GlobalContext = tempParam1
                Else
                    Throw New Exception("Reading settings from ConfigSet <configSet name=" & _
                            XML_CONFIGSET_APPINITIALIZER + "> is failed!")
                End If

            End If


#If DEBUG Then
            ' Start of debugging codes.
            If (_logger.IsDebugEnabled) Then
                _logger.Debug(String.Format("[Param: Parameters_AppInitializer] AppStartedTime= " & _
                    Parameters_GlobalContext.AppStartedTime & ", Company=" & Parameters_GlobalContext.Company & _
                    ", Department=" & Parameters_GlobalContext.Department & ", Author=" & _
                    Parameters_GlobalContext.Author))
            End If
            ' End of debugging codes.
#End If
            ' -------------------------------------------------------------------------------
            ' Load Monitor class parameters from <configSet name="BHS.ServiceMonitor.Monitoring.Monitor"> XMLNode
            ' -------------------------------------------------------------------------------
            '<configSet name="BHS.ServiceMonitor.Monitoring.Monitor">
            '	<threadInterval>3000</threadInterval>
            '	<svcRestartTimeDelay>60000</svcRestartTimeDelay>
            '	<perfMonTimerInterval>60000</perfMonTimerInterval>
            '	<enableServiceControl>True</enableServiceControl>
            '	<!--Machine and services that need to be monitored/controlled by this service-->
            '	<!--BHS_SvcMonitor can also monitor or control (Start/Stop) itself by add its entry here-->
            '	<!--BHS_SvcMonitor can start or stop services if its isControllable is True. The start/stop-->
            '	<!--sequence will follow the running sequence as this list. So BHS_SvcMonitor service-->
            '	<!--must be put as the last one in this list.-->
            '       <service serviceName="BHS_SAC2PLC1GW" processName="BHS_SAC2PLC1GW" machineName="SAC1" serverIP="192.168.100.33" isControllable="True"></service>
            '       <service serviceName="BHS_SAC2PLC2GW" processName="BHS_SAC2PLC2GW" machineName="SAC1" serverIP="192.168.100.33" isControllable="True"></service>
            '       <service serviceName="BHS_SAC2PLC3GW" processName="BHS_SAC2PLC3GW" machineName="SAC1" serverIP="192.168.100.33" isControllable="True"></service>
            '       <service serviceName="BHS_SAC2PLC4GW" processName="BHS_SAC2PLC4GW" machineName="SAC1" serverIP="192.168.100.33" isControllable="True"></service>
            '       <service serviceName="BHS_SAC2PLC6GW" processName="BHS_SAC2PLC6GW" machineName="SAC1" serverIP="192.168.100.33" isControllable="True"></service>
            '       <service serviceName="BHS_SortEngine" processName="BHS_SortEngine" machineName="SAC1" serverIP="192.168.100.33" isControllable="True"></service>
            '	    <service serviceName="BHS_SvcMonitor" processName="BHS_SvcMonitor" machineName="SAC1" serverIP="192.168.100.33" isControllable="True"></service>
            '</configSet>		
            node = XMLConfig.GetConfigSetElement(rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_MONITORING)
            If (node Is Nothing) Then
                Throw New Exception("ConfigSet <" & XML_CONFIGSET_MONITORING & "> is not found in the XML file.")
            Else
                ' Declare a temporary parameter class object
                Dim tempParam1 As PALS.Common.IParameters

                tempParam1 = New Monitoring.MonitorParameters(node, Nothing)

                ' Assign temporary parameter object reference to global parameter object 
                If (Not tempParam1 Is Nothing) Then
                    Parameters_Monitor = tempParam1
                End If
            End If

#If DEBUG Then
            ' Start of debugging codes.
            If (_logger.IsDebugEnabled) Then
                Dim param As Monitoring.MonitorParameters = CType(Parameters_Monitor, Monitoring.MonitorParameters)
                Dim counter As Integer = 0
                Dim list As String = Nothing
                While counter < param.Services.Length
                    list = list & param.Services(counter).ServiceName & ", " & param.Services(counter).ProcessName & ", " & _
                            param.Services(counter).MachineName & ", " & param.Services(counter).ServerIP & ", " & _
                            param.Services(counter).IsControllable.ToString & vbCrLf
                    counter = counter + 1
                End While

                _logger.Debug(String.Format("[Param: Parameters_Monitor] threadInterval= " & _
                    param.ThreadInterval.ToString & ", svcRestartTimeDelay=" & param.SvcRestartTimeDelay.ToString & _
                    ", perfMonTimerInterval=" & param.PerfMonTimerInterval.ToString & ", enableServiceControl=" & param.EnableServiceControl.ToString & _
                    ", service=" & list))

            End If
            ' End of debugging codes.
#End If

            ' -------------------------------------------------------------------------------
            ' Load TCPServer class parameters from <configSet name="PALS.Net.Transports.TCP.TCPServer"> XMLNode
            ' -------------------------------------------------------------------------------
            '<configSet name="PALS.Net.Transports.TCP.TCPServer">
            '	<threadInterval>10</threadInterval>
            '   <!-- SAC Server 1 IP: 192.168.100.33, SAC Server 2 IP: 192.168.100.35, PLC01 IP: 192.168.100.91, PLC02 IP: 192.168.100.93, 
            '     PLC03 IP: 192.168.100.95, PLC04 IP: 192.168.100.97, PLC06 IP: 192.168.100.101 -->
            '   <localNode name="SvcMontr" ip="192.168.100.33" port="24042"/>
            '	<!--The minimum allowed client connections must be 2, one for bussiness data forwarding, another for console connection.-->
            '	<maxConnections>3</maxConnections>
            '</configSet>

            node = Nothing
            node = XMLConfig.GetConfigSetElement(rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_TCPSERVER)
            If (node Is Nothing) Then
                Throw New Exception("Reading settings from ConfigSet <configSet name=" & _
                            XML_CONFIGSET_TCPSERVER & "> is failed!")
            Else
                ' Declare a temporary parameter class object
                Dim tempParam2 As PALS.Common.IParameters

                ' Read settings from particular <configSet> by constructor of parameter class object.
                tempParam2 = New PALS.Net.Transports.TCP.TCPServerParameters(node)

                ' Assign temporary parameter object reference to global parameter object 
                If (Not tempParam2 Is Nothing) Then
                    Parameters_TCPServer = tempParam2
                End If
            End If

#If DEBUG Then
            ' Start of debugging codes.
            If (_logger.IsDebugEnabled) Then
                Dim param As PALS.Net.Transports.TCP.TCPServerParameters = CType(Parameters_TCPServer, PALS.Net.Transports.TCP.TCPServerParameters)

                _logger.Debug(String.Format("[Param: Parameters_TCPServer] threadInterval= " & _
                    param.ThreadInterval.ToString & _
                    ", localNode.name=" & param.LocalNode.Name & _
                    ", localNode.ip=" & param.LocalNode.IP.ToString & _
                    ", localNode.port=" & param.LocalNode.Port.ToString & _
                    ", maxConnections=" & param.MaxConnections.ToString))
            End If
            ' End of debugging codes.
#End If

            ' -------------------------------------------------------------------------------
            ' Load Frame class parameters from <configSet name="PALS.Net.Filters.Frame.Frame"> XMLNode
            ' -------------------------------------------------------------------------------
            '<configSet name="PALS.Net.Filters.Frame.Frame">
            '	<!--Only single character can be used as startMarker, endMarker, and specialMarker-->
            '	<startMarker>02</startMarker>
            '	<endMarker>03</endMarker>
            '	<!--If the character of startMarker or endMarker is included in the outgoing-->
            '	<!--data, the specialMarker is required to be prefixed in order to differentiate-->
            '	<!--the start or end marker and the actual data character.-->
            '	<specialMarker>27</specialMarker>
            '	<!--If accumulated incoming telegram length has been more than maxTelegramSize-->
            '	<!--(number of byte) but no EndMarker received, all accumulated data will be discarded.-->
            '	<maxTelegramSize>10240</maxTelegramSize>
            '</configSet>

            node = Nothing
            node = XMLConfig.GetConfigSetElement(rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_FRAME)
            If (node Is Nothing) Then
                Throw New Exception("Reading settings from ConfigSet <configSet name=" & _
                        XML_CONFIGSET_FRAME & "> is failed!")
            Else
                ' Declare a temporary parameter class object
                Dim tempParam4 As PALS.Common.IParameters

                ' Read settings from particular <configSet> by constructor of parameter class object.
                tempParam4 = New PALS.Net.Filters.Frame.FrameParameters(node, Nothing)

                ' Assign temporary parameter object reference to global parameter object 
                If (Not tempParam4 Is Nothing) Then
                    Parameters_Frame = tempParam4
                End If
            End If

#If DEBUG Then
            ' Start of debugging codes.
            If (_logger.IsDebugEnabled) Then
                Dim param As PALS.Net.Filters.Frame.FrameParameters = CType(Parameters_Frame, PALS.Net.Filters.Frame.FrameParameters)

                _logger.Debug(String.Format("[Param: Parameters_Frame] startMarker= " & _
                    param.StartMarker.ToString("X2") & _
                    ", endMarker=" & param.EndMarker.ToString("X2") & _
                    ", specialMarker=" & param.SpecialMarker.ToString() & _
                    ", maxTelegramSize=" & param.MaxTelegramSize.ToString))
            End If
            ' End of debugging codes.
#End If

            ' -------------------------------------------------------------------------------
            ' Load AppServer class parameters from <configSet name="PALS.Net.Filters.Application.AppServer"> XMLNode
            ' -------------------------------------------------------------------------------
            '<configSet name="PALS.Net.Filters.Application.AppServer">
            '	<threadInterval>100</threadInterval>
            '	<connectionRequestTimeout>3000</connectionRequestTimeout>
            '	<minSequenceNo>1</minSequenceNo>
            '	<maxSequenceNo>9999</maxSequenceNo>
            '	<clients>
            '		<!--The max length of client application code is 8.-->
            '		<appCode>BHSCons1</appCode>
            '		<appCode>BHSCons2</appCode>
            '	</clients>
            '</configSet>
            node = Nothing
            node = XMLConfig.GetConfigSetElement(rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_APPSERVER)
            If (node Is Nothing) Then
                Throw New Exception("Reading settings from ConfigSet <configSet name=" & _
                            XML_CONFIGSET_APPSERVER & "> is failed!")
            Else
                ' Declare a temporary parameter class object
                Dim tempParam6 As PALS.Common.IParameters

                ' Read settings from particular <configSet> by constructor of parameter class object.
                tempParam6 = New PALS.Net.Filters.Application.AppServerParameters(node, nodeTele)

                ' Assign temporary parameter object reference to global parameter object 
                If (Not tempParam6 Is Nothing) Then
                    Parameters_AppServer = tempParam6
                End If
            End If


#If DEBUG Then
            ' Start of debugging codes.
            If (_logger.IsDebugEnabled) Then
                Dim param As PALS.Net.Filters.Application.AppServerParameters = CType(Parameters_AppServer, PALS.Net.Filters.Application.AppServerParameters)
                Dim counter As Integer = 0
                Dim clientList As String = Nothing

                While counter < param.ClientAppCodeList.Count
                    clientList = clientList & param.ClientAppCodeList(counter).ToString & ", "
                    counter = counter + 1
                End While

                _logger.Debug(String.Format("[Param: Parameters_AppServer] threadInterval= " & _
                    param.ThreadInterval.ToString() & ", connectionRequestTimeout=" & param.CRQTimeout.ToString & _
                    ", minSequenceNo=" & param.MinSequenceNo.ToString & _
                    ", maxSequenceNo=" & param.MaxSequenceNo.ToString & _
                    ", clients=" & clientList))
            End If
            ' End of debugging codes.
#End If

            ' -------------------------------------------------------------------------------
            ' Load SOL class parameters from <configSet name="PALS.Net.Filters.SignOfLife.SOL"> XMLNode
            ' -------------------------------------------------------------------------------
            '<configSet name="PALS.Net.Filters.SignOfLife.SOL">
            '	<threadInterval>100</threadInterval>
            '	<solSendTimeout>10000</solSendTimeout>
            '	<solReceiveTimeout>25000</solReceiveTimeout>
            '</configSet>	
            node = Nothing
            node = XMLConfig.GetConfigSetElement(rootSetting, XML_CONFIGSET, "name", XML_CONFIGSET_SOL)
            If (node Is Nothing) Then
                Throw New Exception("Reading settings from ConfigSet <configSet name=" & _
                            XML_CONFIGSET_SOL & "> is failed!")
            Else
                ' Declare a temporary parameter class object
                Dim tempParam7 As PALS.Common.IParameters

                ' Read settings from particular <configSet> by constructor of parameter class object.
                tempParam7 = New PALS.Net.Filters.SignOfLife.SOLParameters(node, nodeTele)

                ' Assign temporary parameter object reference to global parameter object 
                If (Not tempParam7 Is Nothing) Then
                    Parameters_SOL = tempParam7
                End If
            End If

#If DEBUG Then
                ' Start of debugging codes.
                If (_logger.IsDebugEnabled) Then
                    Dim param As PALS.Net.Filters.SignOfLife.SOLParameters = CType(Parameters_SOL, PALS.Net.Filters.SignOfLife.SOLParameters)

                    _logger.Debug(String.Format("[Param: Parameters_SOL] threadInterval= " & _
                        param.ThreadInterval.ToString() & ", solSendTimeout=" & param.SOLSendTimeout.ToString & _
                        ", solReceiveTimeout=" & param.SOLReceiveTimeout.ToString))
                End If
                ' End of debugging codes.
#End If


                ' -------------------------------------------------------------------------------
                ' Load MID class parameters from <telegramSet name="Application_Telegrams"> XMLNode
                ' -------------------------------------------------------------------------------
                '<telegramSet name="Application_Telegrams">
                '  <header alias="Header" name="App_Header" sequence="False" acknowledge="False">
                '    <field name="Type" offset="0" length="4" default=""/>
                '    <field name="Length" offset="4" length="4" default=""/>
                '    <field name="Sequence" offset="8" length="4" default=""/>
                '  </header>
                '  <!-- "Type, Length" field of Application message is mandatory for APP class. -->
                '  <telegram alias="CRQ" name="App_Connection_Request_Message" sequence="True" acknowledge="False">
                '    <!-- value="48,48,48,49" - the ASCII value (decimal) string. -->
                '    <!-- "48,48,48,49" here represents the default field value are -->
                '    <!-- 4 bytes (H30 H30 H30 H31). The delimiter must be comma(,). -->
                '    <field name="Type" offset="0" length="4" default="48,48,48,49"/>
                '    <field name="Length" offset="4" length="4" default="48,48,50,48"/>
                '    <field name="Sequence" offset="8" length="4" default="?"/>
                '    <field name="ClientAppCode" offset="12" length="8" default="?"/>
                '  </telegram>
                '  ...
                ' -------------------------------------------------------------------------------
                node = Nothing
                If (nodeTele Is Nothing) Then
                    Throw New Exception("Reading settings from ConfigSet <configSet name=" & _
                                XML_CONFIGSET_TELEGRAM_FORMAT & "> is failed!")
                Else
                    ' Declare a temporary parameter class object
                    Dim tempParam14 As PALS.Common.IParameters

                    ' Read settings from particular <configSet> by constructor of parameter class object.
                    tempParam14 = New PALS.Net.Filters.Application.MessageIdentifierParameters(node, nodeTele)

                    ' Assign temporary parameter object reference to global parameter object 
                    If (Not tempParam14 Is Nothing) Then
                        Parameters_MID = tempParam14
                    End If
                End If

#If DEBUG Then
                ' Start of debugging codes.
                If (_logger.IsDebugEnabled) Then
                    Dim param As PALS.Net.Filters.Application.MessageIdentifierParameters = CType(Parameters_MID, PALS.Net.Filters.Application.MessageIdentifierParameters)
                    Dim hash As System.Collections.DictionaryEntry
                    Dim tF As PALS.Telegrams.TelegramFormat
                    Dim msg As System.Text.StringBuilder = New System.Text.StringBuilder()
                    For Each hash In param.MessageFormatHash
                        tF = CType(hash.Value, PALS.Telegrams.TelegramFormat)
                        msg.Append(hash.Key)
                        msg.Append("(")
                        msg.Append(tF.AliasName)
                        msg.Append("), ")
                    Next

                    _logger.Debug(String.Format("[Param: Parameters_MID] messageBooking= " & _
                        msg.ToString))
                End If
                ' End of debugging codes.
#End If


                ' -------------------------------------------------------------------------------
                ' Load MsgHdlr class parameters from <telegramSet name="Application_Telegrams"> XMLNode
                ' -------------------------------------------------------------------------------
                '<telegramSet name="Application_Telegrams">
                '  <header alias="Header" name="App_Header" sequence="False" acknowledge="False">
                '    <field name="Type" offset="0" length="4" default=""/>
                '    <field name="Length" offset="4" length="4" default=""/>
                '    <field name="Sequence" offset="8" length="4" default=""/>
                '  </header>
                '  <!-- "Type, Length" field of Application message is mandatory for APP class. -->
                '  <telegram alias="CRQ" name="App_Connection_Request_Message" sequence="True" acknowledge="False">
                '    <!-- value="48,48,48,49" - the ASCII value (decimal) string. -->
                '    <!-- "48,48,48,49" here represents the default field value are -->
                '    <!-- 4 bytes (H30 H30 H30 H31). The delimiter must be comma(,). -->
                '    <field name="Type" offset="0" length="4" default="48,48,48,49"/>
                '    <field name="Length" offset="4" length="4" default="48,48,50,48"/>
                '    <field name="Sequence" offset="8" length="4" default="?"/>
                '    <field name="ClientAppCode" offset="12" length="8" default="?"/>
                '  </telegram>
                '  ...
                ' -------------------------------------------------------------------------------
                node = Nothing
                If (nodeTele Is Nothing) Then
                    Throw New Exception("Reading settings from ConfigSet <configSet name=" & _
                                XML_CONFIGSET_TELEGRAM_FORMAT & "> is failed!")
                Else
                    ' Declare a temporary parameter class object
                    Dim tempParam15 As PALS.Common.IParameters

                    ' Read settings from particular <configSet> by constructor of parameter class object.
                tempParam15 = New BHS.Net.ServiceMonitor.Application.MessageHandlerParameters(node, nodeTele)

                    ' Assign temporary parameter object reference to global parameter object 
                    If (Not tempParam15 Is Nothing) Then
                        Parameters_MsgHdlr = tempParam15
                    End If
                End If

#If DEBUG Then
                ' Start of debugging codes.
                If (_logger.IsDebugEnabled) Then
                Dim param As BHS.Net.ServiceMonitor.Application.MessageHandlerParameters = CType(Parameters_MsgHdlr, BHS.Net.ServiceMonitor.Application.MessageHandlerParameters)

                    _logger.Debug(String.Format("[Param: Parameters_MsgHdlr] messages= " & _
                        param.SRPMessageType & "(" & param.SRPMessageFormat.AliasName & _
                        "), " & param.SRQMessageType & "(" & param.SRQMessageFormat.AliasName & _
                        "), " & param.STOMessageType & "(" & param.STOMessageFormat.AliasName & _
                        "), " & param.STRMessageType & "(" & param.STRMessageFormat.AliasName & ")"))
                End If
                ' End of debugging codes.
#End If
                ' Raise event when reload setting from changed configuration file is successfully completed.
                If (isReloading) Then
                    RaiseEvent ReloadSettingCompleted()
                End If
                ' -------------------------------------------------------------------------------
                If (_logger.IsInfoEnabled) Then
                    _logger.Info("Loading application settings is successed. <" & thisMethod + ">")
                End If
                ' -------------------------------------------------------------------------------


        End Sub
#End Region


#Region "7. Class Events Defination."
        ''' <summary>
        ''' Event will be raised when reload setting from changed configuration 
        ''' file is successfully completed.
        ''' </summary>
        Public Event ReloadSettingCompleted()
#End Region

    End Class

End Namespace

