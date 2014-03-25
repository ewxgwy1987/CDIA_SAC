'=====================================================================================
' Copyright 2009, Xu Jian, All Rights Reserved.
'=====================================================================================
'FileName       Initializer.vb
'Revision:      1.0 -   13 Mar 2009, By hs chia, For Winnipeg International Airport BHS project.
'=====================================================================================

Option Explicit On
Option Strict On

Imports System.Xml
Imports PALS.Net.Common
Imports PALS.Utilities
Imports PALS.Configure
Imports PALS.Net.Filters

Namespace ServiceMonitor.Application
    ''' -----------------------------------------------------------------------------
    ''' Project	 : BHS
    ''' Class	 : ServiceMonitor.Application.Initializer
    ''' 
    ''' -----------------------------------------------------------------------------
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <remarks>
    ''' </remarks>
    ''' <history>
    ''' 	[xujian]	26/07/2006	Created
    ''' </history>
    ''' -----------------------------------------------------------------------------
    Public Class Initializer
        Implements IDisposable

#Region "1. Class Fields Declaration."
        Private Const XMLCONFIG_LOG4NET As String = "log4net"

        Private Const OBJECT_ID_INITIALIZER As String = "1"

        Private Const OBJECT_ID_MONITOR As String = "2"

        Private Const OBJECT_ID_SERVERMANAGER As String = "3"
        Private Const OBJECT_ID_TCPSERVER As String = "3.1"
        Private Const OBJECT_ID_SERVERFRAME As String = "3.2"
        Private Const OBJECT_ID_SERVERAPP As String = "3.3"
        Private Const OBJECT_ID_SERVERSOL As String = "3.4"
        Private Const OBJECT_ID_SERVERINMID As String = "3.5"
        Private Const OBJECT_ID_SERVERHANDLER As String = "3.6"
        Private Const OBJECT_ID_SERVER_MSG_FORWARDER As String = "3.7"

        Private Const OBJECT_ID_MESSAGEHANDLER As String = "4"

        'The name of current class 
        Private Shared ReadOnly m_ClassName As String = _
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString
        'Create a logger for use in this class
        Private Shared ReadOnly m_Logger As log4net.ILog = _
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType)
        Private m_ObjectID As String

        ' Configuration files
        Private _xmlFileSetting As IO.FileInfo
        Private _xmlFileTelegram As IO.FileInfo

        ' -----------------------------------------------------------------------------
        ' Used to store the reference of ConfigureAndWatchHandler class object for proper release of file 
        ' watchers (done by Dispose() method of Initializer class) when application is closed .
        Private _fileWatchHandler As ConfigureAndWatchHandler
        '
        ' Code Example: 
        ' Instead of watch multiple configuration files in one ConfigureAndWatchHandler class object. Multiple 
        ' ConfigureAndWatchHandler objects could be created. And each object is responsible for the watching
        ' the changes of different single or multiple configuration files.
        ' In this case, the multiple IConfigurationLoader objects need to be created too. Each loader object
        ' is paired with one ConfigureAndWatchHandler object for loading settings and watching changes.
        '
        ' private  _fileWatchHandler2 As ConfigureAndWatchHandler 
        ' private  _xmlLoader2 As BHS.SAC2PLCGW.Configure.XmlSettingLoader2
        '
        ' -----------------------------------------------------------------------------

        ' -----------------------------------------------------------------------------
        ' Object of class XmlSettingLoader derived from interface IConfigurationLoader for loading setting from XML file.
        Private _xmlLoader As BHS.Net.ServiceMonitor.Configure.XmlSettingLoader
        '
        ' Code Example: 
        ' Object of class IniSettingLoader derived from interface IConfigurationLoader for loading setting from INI file.
        '
        ' private  _iniLoader As BHS.SAC2PLCGW.Configure.IniSettingLoader
        '
        ' -----------------------------------------------------------------------------




        Private m_ServiceMonitor As Monitoring.Monitor

        '####################################################################
        '# Service Monitor Service Application - TCP Server communication channel 
        '# 1. PALS.Net.Managers.SessionManager class instant
        Private m_ServerManager As PALS.Net.Managers.SessionManager 'Object ID:3
        '# 2. PALS.Net.Filters classes instant
        Private m_ServerFrameOnTCP As PALS.Common.IChain 'Object ID:3.2
        Private m_ServerAPP As PALS.Common.IChain 'Object ID:3.3
        Private m_ServerSOL As PALS.Common.IChain 'Object ID:3.4
        Private _ServerIncomingMID As PALS.Common.IChain 'Object ID:3.5
        '# 3. PALS.Net.Handlers classes instant
        Private m_ServerForwarder As PALS.Common.IChain 'Object ID:3.7
        '...
        '####################################################################

        Private m_MessageHandler As BHS.Net.ServiceMonitor.Application.MessageHandler 'Object ID:4

        'The ClassStatus object of current class
        Private m_PerfMonitor As PALS.Diagnostics.ClassStatus
        'The Hashtable that contains the ClassStatus object of current class 
        'and all of its instance of sub classes.
        Private m_PerfMonitorList As ArrayList

#End Region

#Region "2. Class Constructor and Destructor Declaration."
        Public Sub New(ByVal XMLFileOfAppSetting As String, ByVal XMLFileOfTelegrams As String)
            MyBase.new()

            'Step 1. Set the configuration file name
            If Trim(XMLFileOfAppSetting) = "" Then
                Throw New Exception("The name of application setting XML config file is mandatory!")
            End If
            If Trim(XMLFileOfTelegrams) = "" Then
                Throw New Exception("The name of application telegram XML config file is mandatory!")
            End If

            'Step 2. Set the root XmlElement of configuration file
            Dim xmlRootApp As XmlElement = PALS.Utilities.XMLConfig.GetConfigFileRootElement(XMLFileOfAppSetting)
            If xmlRootApp Is Nothing Then
                Throw New Exception("Open application setting XML configuration file failure!")
            End If

            Dim xmlRootTele As XmlElement = PALS.Utilities.XMLConfig.GetConfigFileRootElement(XMLFileOfTelegrams)
            If xmlRootTele Is Nothing Then
                Throw New Exception("Open application setting XML configuration file failure!")
            End If

            m_ObjectID = OBJECT_ID_INITIALIZER

            'Step 3. Initialize the Log4Net config settings
            Dim log4netConfig As XmlElement = CType(PALS.Utilities.XMLConfig.GetConfigSetElement(xmlRootApp, XMLCONFIG_LOG4NET), XmlElement)

            If log4netConfig Is Nothing Then
                Throw New Exception("There is no <" + XMLCONFIG_LOG4NET & _
                                "> settings in the XML configuration file!")
            Else
                _xmlFileSetting = New System.IO.FileInfo(XMLFileOfAppSetting)
                _xmlFileTelegram = New System.IO.FileInfo(XMLFileOfTelegrams)

                log4net.Config.XmlConfigurator.Configure(log4netConfig)
                m_Logger.Info(".")
                m_Logger.Info(".")
                m_Logger.Info(".")
                m_Logger.Info("[..................] <" & m_ClassName & ".New()>")
                m_Logger.Info("[...App Starting...] <" & m_ClassName & ".New()>")
                m_Logger.Info("[..................] <" & m_ClassName & ".New()>")
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            If m_Logger.IsInfoEnabled Then
                m_Logger.Info("[..................] <" & m_ClassName & ".Dispose()>")
                m_Logger.Info("[...App Stopping...] <" & m_ClassName & ".Dispose()>")
                m_Logger.Info("[..................] <" & m_ClassName & ".Dispose()>")
                m_Logger.Info("Class:[" & m_ClassName & _
                            "] object is going to be destroyed... <" & m_ClassName & ".Dispose()>")
            End If

            If Not (m_PerfMonitorList Is Nothing) Then
                m_PerfMonitorList.Clear()
                m_PerfMonitorList = Nothing
            End If

            If Not (m_PerfMonitor Is Nothing) Then
                m_PerfMonitor.Dispose()
                m_PerfMonitor = Nothing
            End If

            If Not (m_ServiceMonitor Is Nothing) Then
                m_ServiceMonitor.Dispose()
                m_ServiceMonitor = Nothing
            End If

            '#######################################################################
            'Destory Server Chain
            If Not (_ServerIncomingMID Is Nothing) Then
                CType(_ServerIncomingMID, PALS.Net.Filters.Application.IncomingMessageIdentifier).Dispose()
                _ServerIncomingMID = Nothing
            End If

            If Not (m_ServerSOL Is Nothing) Then
                CType(m_ServerSOL, PALS.Net.Filters.SignOfLife.SOL).Dispose()
                m_ServerSOL = Nothing
            End If

            If Not (m_ServerAPP Is Nothing) Then
                CType(m_ServerAPP, PALS.Net.Filters.Application.AppServer).Dispose()
                m_ServerAPP = Nothing
            End If

            If Not (m_ServerFrameOnTCP Is Nothing) Then
                CType(m_ServerFrameOnTCP, PALS.Net.Filters.Frame.Frame).Dispose()
                m_ServerFrameOnTCP = Nothing
            End If

            If Not (m_ServerForwarder Is Nothing) Then
                CType(m_ServerForwarder, ServiceMonitor.Net.Handlers.ServerMsgForwarder).Dispose()
                m_ServerForwarder = Nothing
            End If

            If Not (m_ServerManager Is Nothing) Then
                m_ServerManager.Dispose()
                m_ServerManager = Nothing
            End If
            '#######################################################################

            '#######################################################################
            'Destory Message Handlers
            If Not (m_MessageHandler Is Nothing) Then
                m_MessageHandler.Dispose()
                m_MessageHandler = Nothing
            End If
            '#######################################################################
            ' Destory configuration file watcher.
            If Not _fileWatchHandler Is Nothing Then
                _fileWatchHandler.Dispose()
            End If

            If Not _xmlLoader Is Nothing Then
                _xmlLoader.Dispose()
            End If
            '-----------------------------------------------------------------------------

            If m_Logger.IsInfoEnabled Then
                m_Logger.Info("Class:[" & m_ClassName & "] object has been destroyed! <" & _
                            m_ClassName & ".Dispose()>")
            End If
        End Sub 'Dispose

        Protected Overrides Sub Finalize()
            MyBase.Finalize()
        End Sub

#End Region

#Region "3. Class Properties Defination."
        Public ReadOnly Property ClassName() As String
            Get
                Return m_ClassName
            End Get
        End Property

        Public Property ObjectID() As String
            Get
                Return m_ObjectID
            End Get
            Set(ByVal Value As String)
                m_ObjectID = Value
            End Set
        End Property

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        ''' Class property to return the ClassStatus object of current class.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' </remarks>
        ''' <history>
        ''' 	[xujian]	11/2/2005	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Public ReadOnly Property PerfMonitor() As PALS.Diagnostics.ClassStatus
            Get
                Try
                    'Refresh current class perfermance monitoring counters.
                    m_PerfMonitor.ObjectID = m_ObjectID
                    PerfCounterRefresh()

                    Return m_PerfMonitor
                Catch ex As Exception
                    Dim ThisMethod As String = m_ClassName & "." & _
                            System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

                    If m_Logger.IsErrorEnabled Then
                        m_Logger.Error("Exception occurred.<" & ThisMethod & _
                                "> has exception: Source = " & ex.Source & _
                                " | Type : " & ex.GetType.ToString & _
                                " | Message : " & ex.Message)
                    End If
                    Return Nothing
                End Try
            End Get
        End Property

        ''' -----------------------------------------------------------------------------
        ''' <summary>
        ''' Class property to return the ArrayList object that contains the ClassStatus 
        ''' objects of current class and all of its instance of sub classes.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks>
        ''' </remarks>
        ''' <history>
        ''' 	[xujian]	11/2/2005	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Public ReadOnly Property PerfMonitorList() As ArrayList
            Get
                Try
                    Dim Temp As PALS.Diagnostics.ClassStatus
                    Temp = Me.PerfMonitor

                    Return m_PerfMonitorList
                Catch ex As Exception
                    Return Nothing
                End Try
            End Get
        End Property

#End Region

#Region "4. Class Overrides Method Declaration."

#End Region

#Region "5. Class Method Declaration."
        ''' <summary>
        ''' Event handler of ReloadSettingCompleted event fired by IConfigurationLoader interface 
        ''' implemented class method LoadSettingFromConfigFile() upon the reloading setting from
        ''' changed file is successfully completed. 
        ''' 
        ''' This event handler is to make sure the reloaded settings can be taken effective 
        ''' immediately.
        ''' </summary>
        Private Sub OnReloadSettingCompleted()
            'Server Sesion Chain
            CType(m_ServerManager, PALS.Net.Managers.SessionManager).ClassParameters = _
                _xmlLoader.Parameters_TCPServer
            CType(m_ServerFrameOnTCP, PALS.Net.Filters.Frame.Frame).ClassParameters = _
                CType(_xmlLoader.Parameters_Frame, Frame.FrameParameters)
            CType(m_ServerAPP, PALS.Net.Filters.Application.AppServer).ClassParameters = _
                CType(_xmlLoader.Parameters_AppServer, PALS.Net.Filters.Application.AppServerParameters)
            CType(m_ServerSOL, PALS.Net.Filters.SignOfLife.SOL).ClassParameters = _
                CType(_xmlLoader.Parameters_SOL, SignOfLife.SOLParameters)
        End Sub

        Public Function Init() As Boolean
            Dim ThisMethod As String = m_ClassName & "." & _
                    System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Try
                If m_Logger.IsInfoEnabled Then
                    m_Logger.Info("Class:[" & m_ClassName & _
                            "] object is initializing... <" & ThisMethod & ">")
                End If

                'Step 1: Read General application infomation setting parameters
                _xmlLoader = New BHS.Net.ServiceMonitor.Configure.XmlSettingLoader()
                AddHandler _xmlLoader.ReloadSettingCompleted, AddressOf OnReloadSettingCompleted

                '-----------------------------------------------------------------------------
                ' Load system parameters from two configuration files (CFG_MDS2CCTVGW.xml, CFG_Telegrams.xml).
                ' And also start watcher to detect the change of files.  and reload setting if change is detected.
                '
                _fileWatchHandler = PALS.Configure.AppConfigurator.ConfigureAndWatch( _
                                        _xmlLoader, _xmlFileSetting, _xmlFileTelegram)
                '
                ' Note: _fileWatchHandler need to be released in the Dispose() method of Initializer class.
                '-----------------------------------------------------------------------------

                'Step 2: Build Server Channel Session Chain
                If Not BuildServerSessionChain() Then
                    Throw New Exception("Build TCPServer session chain failure!")
                End If

                'Step 3: Create MessageHandler object.
                m_MessageHandler = New MessageHandler(_xmlLoader.Parameters_MsgHdlr, _
                            CType(m_ServerForwarder, Net.Handlers.ServerMsgForwarder))
                m_MessageHandler.ObjectID = OBJECT_ID_MESSAGEHANDLER
                AddHandler m_MessageHandler.OnStatusRequested, AddressOf OnStatusRequested_MessageHandler

                'Step 4: Create ServiceMonitor Monitor object.
                m_ServiceMonitor = New Monitoring.Monitor(_xmlLoader.Parameters_Monitor)
                m_ServiceMonitor.ObjectID = OBJECT_ID_MONITOR
                AddHandler m_MessageHandler.OnServiceStartRequest, AddressOf m_ServiceMonitor.OnServiceStartRequest_MessageHandler
                AddHandler m_MessageHandler.OnServiceStopRequest, AddressOf m_ServiceMonitor.OnServiceStopRequest_MessageHandler

                'Step 5: Class status for debuging only.
                m_PerfMonitor = New PALS.Diagnostics.ClassStatus

                m_PerfMonitor.AddSubClassStatus(m_ServerManager.PerfMonitor)
                m_PerfMonitor.AddSubClassStatus(CType(_ServerIncomingMID, PALS.Net.Filters.Application.IncomingMessageIdentifier).PerfMonitor)
                m_PerfMonitor.AddSubClassStatus(CType(m_ServerSOL, PALS.Net.Filters.SignOfLife.SOL).PerfMonitor)
                m_PerfMonitor.AddSubClassStatus(CType(m_ServerAPP, PALS.Net.Filters.Application.AppServer).PerfMonitor)
                m_PerfMonitor.AddSubClassStatus(CType(m_ServerFrameOnTCP, PALS.Net.Filters.Frame.Frame).PerfMonitor)
                m_PerfMonitor.AddSubClassStatus(m_MessageHandler.PerfMonitor)
                m_PerfMonitor.AddSubClassStatus(m_ServiceMonitor.PerfMonitor)

                '#####################################################################
                '#Important:                                                         #
                '#Create the reference link between m_PerfMonitorList object items    #
                '#to class itself and all individual sub classes.                    #
                '#This reference link can only be done once, hence it is located in  #
                '#this Init() method, instead of PerfMonitorHash() property.         #
                '#In the PerfMonitorHash() property, only perfoemance monitoring     #
                'counter refresh will be invoked.                                    #
                m_PerfMonitorList = m_PerfMonitor.GetAllClassStatusArray()
                m_MessageHandler.PerfMonitorList = m_PerfMonitorList
                '#####################################################################

                'Step 6: Open underlying layer connection to start retrieving/writing data from/to PLC
                m_ServerManager.SessionStart()

                'Step 7: Start service monitoring timer
                m_ServiceMonitor.StartMonitoring()

                If m_Logger.IsInfoEnabled Then
                    m_Logger.Info("Class:[" & m_ClassName & _
                            "] object has been initialized. <" & ThisMethod & ">")
                End If

                Return True
            Catch ex As Exception
                If m_Logger.IsErrorEnabled Then
                    m_Logger.Error("Class:[" & m_ClassName & _
                            "] object initialization failure.<" & ThisMethod & _
                            "> has exception: Source = " & ex.Source & _
                            " | Type : " & ex.GetType.ToString & _
                            " | Message : " & ex.Message)
                End If

                If Not m_ServerManager Is Nothing Then m_ServerManager.Dispose()
                If Not m_ServerFrameOnTCP Is Nothing Then CType(m_ServerFrameOnTCP, PALS.Net.Filters.Frame.Frame).Dispose()
                If Not m_ServerAPP Is Nothing Then CType(m_ServerAPP, PALS.Net.Filters.Application.AppServer).Dispose()
                If Not m_ServerSOL Is Nothing Then CType(m_ServerSOL, PALS.Net.Filters.SignOfLife.SOL).Dispose()
                If Not _ServerIncomingMID Is Nothing Then CType(_ServerIncomingMID, PALS.Net.Filters.Application.IncomingMessageIdentifier).Dispose()
                If Not m_MessageHandler Is Nothing Then m_MessageHandler.Dispose()
                If Not m_ServiceMonitor Is Nothing Then m_ServiceMonitor.Dispose()
                If Not m_PerfMonitor Is Nothing Then m_PerfMonitor.Dispose()

                Return False
            End Try
        End Function

        Private Function BuildServerSessionChain() As Boolean
            Dim ThisMethod As String = m_ClassName & "." & _
             System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Try
                '################################################################
                'Instantial SessionManager, and create the basic 
                'Handler-Filter-Transport protocol chain.
                m_ServerManager = New PALS.Net.Managers.SessionManager(TransportProtocol.TCPServer, _xmlLoader.Parameters_TCPServer)
                m_ServerManager.ObjectID = OBJECT_ID_SERVERMANAGER
                m_ServerManager.TransportObjectID = OBJECT_ID_TCPSERVER
                m_ServerManager.HandlerObjectID = OBJECT_ID_SERVERHANDLER


                m_ServerFrameOnTCP = New PALS.Net.Filters.Frame.Frame(_xmlLoader.Parameters_Frame)
                CType(m_ServerFrameOnTCP, AbstractProtocolChain).ObjectID = OBJECT_ID_SERVERFRAME

                'Instantial filter chain class App, and add it into 
                'Handler-Filter-Transport protocol chain.
                m_ServerAPP = New PALS.Net.Filters.Application.AppServer(_xmlLoader.Parameters_AppServer)
                CType(m_ServerAPP, AbstractProtocolChain).ObjectID = OBJECT_ID_SERVERAPP

                'Instantial filter chain class SOL, and add it into 
                'Handler-Filter-Transport protocol chain.
                m_ServerSOL = New PALS.Net.Filters.SignOfLife.SOL(_xmlLoader.Parameters_SOL)
                CType(m_ServerSOL, AbstractProtocolChain).ObjectID = OBJECT_ID_SERVERSOL
    

                _ServerIncomingMID = New PALS.Net.Filters.Application.IncomingMessageIdentifier(_xmlLoader.Parameters_MID)
                CType(_ServerIncomingMID, AbstractProtocolChain).ObjectID = OBJECT_ID_SERVERINMID

                m_ServerForwarder = New BHS.Net.ServiceMonitor.Net.Handlers.ServerMsgForwarder
                CType(m_ServerForwarder, AbstractProtocolChain).ObjectID = OBJECT_ID_SERVER_MSG_FORWARDER

                '################################################################
                'Construct additional Handlers and Filters into basic chain.
                '--------------------------------------------
                '(1)Handler - (2)Filter - (3)Transport chain:
                '--------------------------------------------
                '(0)ServerMsgForwarder ->
                '(1)SessionHandler -> 
                '(2.1)SOL -> 
                '(2.2)APPServer -> 
                '(2.3)Frame -> 
                '(3)TCPServer
                '--------------------------------------------
                m_ServerManager.AddHandlerToLast(m_ServerForwarder)

                m_ServerManager.AddFilterToLast(_ServerIncomingMID)
                m_ServerManager.AddFilterToLast(m_ServerSOL)
                m_ServerManager.AddFilterToLast(m_ServerAPP)
                m_ServerManager.AddFilterToLast(m_ServerFrameOnTCP)
                '################################################################

                Return True
            Catch ex As Exception
                If m_Logger.IsErrorEnabled Then
                    m_Logger.Error("Class:[" & m_ClassName & _
                            "] Exception occurred.<" & ThisMethod & _
                            "> has exception: Source = " & ex.Source & _
                            " | Type : " & ex.GetType.ToString & _
                            " | Message : " & ex.Message)
                End If
                Return False
            End Try
        End Function

        Private Sub OnStatusRequested_MessageHandler()
            'Refresh current class perfermance monitoring counters.
            m_PerfMonitor.ObjectID = m_ObjectID
            PerfCounterRefresh()
        End Sub

        '<object id="1">
        '	<class>DBT2_BHS.ServiceMonitor.Application.Initializer</class>
        '	<service>ServiceMonitor</service>
        '	<serviceStartedTime>27/07/2006 15:26:21</serviceStartedTime>
        '	<company>Inter-Roller</company>
        '	<department>CSI</department>
        '	<author>XuJian</author>
        '</object>
        Private Sub PerfCounterRefresh()
            Dim ThisMethod As String = m_ClassName & "." & _
            System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Try
                'Refresh current class object status counters.
                If (Not m_PerfMonitor Is Nothing) And (Not m_ObjectID Is Nothing) Then
                    m_PerfMonitor.OpenObjectNode()

                    m_PerfMonitor.AddObjectStatus("class", m_ClassName)

                    m_PerfMonitor.CloseObjectNode()
                End If

                'Refresh all other class object status counters.
                Dim Temp As PALS.Diagnostics.ClassStatus

                Temp = m_ServerManager.PerfMonitor()
                Temp = CType(m_ServerFrameOnTCP, PALS.Net.Filters.Frame.Frame).PerfMonitor
                Temp = CType(m_ServerAPP, PALS.Net.Filters.Application.AppServer).PerfMonitor
                Temp = CType(m_ServerSOL, PALS.Net.Filters.SignOfLife.SOL).PerfMonitor
                Temp = CType(_ServerIncomingMID, PALS.Net.Filters.Application.IncomingMessageIdentifier).PerfMonitor
                Temp = m_MessageHandler.PerfMonitor
                Temp = m_ServiceMonitor.PerfMonitor

            Catch ex As Exception
                If m_Logger.IsErrorEnabled Then
                    m_Logger.Error("Exception occurred.<" & ThisMethod & _
                            "> has exception: Source = " & ex.Source & _
                            " | Type : " & ex.GetType.ToString & _
                            " | Message : " & ex.Message)
                End If
            End Try
        End Sub

#End Region

#Region "6. Class Events Defination."

#End Region

    End Class

End Namespace
