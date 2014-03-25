'=====================================================================================
' Copyright 2009, Xu Jian, All Rights Reserved.
'=====================================================================================
'FileName       MonitorParameters.vb
'Revision:      1.0 -   13 Mar 2009, By hs chia, For Winnipeg International Airport BHS project.
'=====================================================================================

Option Explicit On
Option Strict On

Imports System.Xml
Imports PALS.Utilities

Namespace ServiceMonitor.Monitoring 'Root Namespace: BHS

    Public Class MonitorParameters
        Implements PALS.Common.IParameters, IDisposable

#Region "1. Class Fields Declaration."
        Private Const CONFIGSET_THREAD_INTERVAL As String = "threadInterval"
        Private Const CONFIGSET_SERVICE_RESTART_TIME_DELAY As String = "svcRestartTimeDelay"
        Private Const CONFIGSET_PERFMON_INTERVAL As String = "perfMonTimerInterval"
        Private Const CONFIGSET_ENABLE_SERVICE_CONTROL As String = "enableServiceControl"
        Private Const CONFIGSET_SERVICE_NAME As String = "serviceName"
        Private Const CONFIGSET_PROCESS_NAME As String = "processName"
        Private Const CONFIGSET_MACHINE_NAME As String = "machineName"
        Private Const CONFIGSET_SERVER_IP As String = "serverIP"
        Private Const CONFIGSET_IS_CONTROLLABLE As String = "isControllable"

        Private Const MINIMUM_THREAD_INTERVAL As Integer = 2000
        Private Const MINIMUM_SERVICE_RESTART_TIMEDELAY As Integer = 10000
        Private Const MINIMUM_PERFMON_TIMEDELAY As Integer = 60000
        Private Const UNKNOWN_SERVICE_STATUS As String = "Unknown"

        'The name of current class 
        Private Shared ReadOnly m_ClassName As String = _
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString
        'Create a logger for use in this class
        Private Shared ReadOnly m_Logger As log4net.ILog = _
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType)

        Private m_Services() As ServiceInfo
        Private m_EnableServiceControl As Boolean

        Private m_ThreadInterval As Long
        Private m_SvcRestartTimeDelay As Integer
        Private m_PerfMonTimerInterval As Integer
#End Region

#Region "2. Class Constructor and Destructor Declaration."
        Public Sub New(ByRef ConfigSet As XmlNode, ByRef TelegramSet As XmlNode)
            MyBase.new()

            If ConfigSet Is Nothing Then
                Throw New Exception("There is no [" & ConfigSet.Name & _
                        "] XMLNode was passed to constructer, class " & m_ClassName & _
                        "is unable to be instantiated!")
            Else
                If Not Init(ConfigSet, TelegramSet) Then
                    Throw New Exception("Initializing failure, class " & _
                        m_ClassName & "is unable to be instantiated!")
                End If
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            m_Services = Nothing
        End Sub 'Dispose
#End Region

#Region "3. Class Properties Defination."

        Public ReadOnly Property Services() As ServiceInfo()
            Get
                Return m_Services
            End Get
        End Property

        Public ReadOnly Property EnableServiceControl() As Boolean
            Get
                Return m_EnableServiceControl
            End Get
        End Property

        Public ReadOnly Property ThreadInterval() As Long
            Get
                Return m_ThreadInterval
            End Get
        End Property

        Public ReadOnly Property SvcRestartTimeDelay() As Integer
            Get
                Return m_SvcRestartTimeDelay
            End Get
        End Property

        Public ReadOnly Property PerfMonTimerInterval() As Integer
            Get
                Return m_PerfMonTimerInterval
            End Get
        End Property

#End Region

#Region "4. Class Overrides Method Declaration."

#End Region

#Region "5. Class Method Declaration."

        Private Function Init(ByRef ConfigSet As XmlNode, ByRef TelegramSet As XmlNode) As Boolean _
                        Implements PALS.Common.IParameters.Init
            Dim ThisMethod As String = m_ClassName & "." & _
              System.Reflection.MethodBase.GetCurrentMethod().Name & "()"
            Dim ConfigSetName As String

            Try
                ConfigSetName = XMLConfig.GetSettingFromAttribute(ConfigSet, "name", "Unknown")

                '<configSet name="DBT2_BHS.ServiceMonitor.Monitoring.Monitor">
                '	<threadInterval>5000</threadInterval>
                '	<svcRestartTimeDelay>30000</svcRestartTimeDelay>
                '	<perfMonTimerInterval>60000</perfMonTimerInterval>
                '	<enableServiceControl>True</enableServiceControl>
                '	<!--Machine and services that need to be monitored/controlled by this service-->
                '	<service serviceName="MSSQL$DBINS" processName="sqlservr" machineName="SAC-COM1" serverIP="192.168.15.38" isControllable="False"></service>
                '	<service serviceName="BHS_SvcMonitor" processName="BHS_SvcMonitor" machineName="SAC-COM1" serverIP="192.168.15.38" isControllable="True"></service>
                '	<service serviceName="BHS_SAC2PLC2GW" processName="BHS_SAC2PLC2GW" machineName="SAC-COM1" serverIP="192.168.15.38" isControllable="True"></service>
                '	<service serviceName="BHS_SAC2PLC4GW" processName="BHS_SAC2PLC4GW" machineName="SAC-COM1" serverIP="192.168.15.38" isControllable="True"></service>
                '	<service serviceName="BHS_SortEngine" processName="BHS_SortEngine" machineName="SAC-COM1" serverIP="192.168.15.38" isControllable="True"></service>
                '</configSet>
                Dim Count As Integer
                Dim SvcState As System.ServiceProcess.ServiceControllerStatus
                Dim Child As XmlNode

                Count = 0
                For Each Child In ConfigSet.ChildNodes
                    If Child.Name = "service" Then
                        ReDim Preserve m_Services(Count)

                        Try
                            Clear(m_Services(Count))

                            m_Services(Count).ServiceName = _
                                    XMLConfig.GetSettingFromAttribute(Child, CONFIGSET_SERVICE_NAME, Nothing)
                            m_Services(Count).ProcessName = _
                                    XMLConfig.GetSettingFromAttribute(Child, CONFIGSET_PROCESS_NAME, Nothing)
                            m_Services(Count).MachineName = _
                                    XMLConfig.GetSettingFromAttribute(Child, CONFIGSET_MACHINE_NAME, Nothing)
                            m_Services(Count).ServerIP = _
                                    XMLConfig.GetSettingFromAttribute(Child, CONFIGSET_SERVER_IP, Nothing)
                            m_Services(Count).IsControllable = _
                                    CType(XMLConfig.GetSettingFromAttribute(Child, CONFIGSET_IS_CONTROLLABLE, "True"), Boolean)

                            m_Services(Count).SvcController = New System.ServiceProcess.ServiceController( _
                                            m_Services(Count).ServiceName, m_Services(Count).MachineName)

                            'Used to verify whether the service has been installed.
                            SvcState = m_Services(Count).SvcController.Status
                            m_Services(Count).Status = SvcState.ToString
                            m_Services(Count).IsExisted = True
                            m_Services(Count).StopDetectedTime = Now

                        Catch ex As Exception
                            m_Services(Count).IsExisted = False
                            m_Services(Count).Status = UNKNOWN_SERVICE_STATUS

                            If m_Logger.IsErrorEnabled Then
                                m_Logger.Error("[Service:" & m_Services(Count).ServiceName & _
                                            ", MachineIP:" & m_Services(Count).ServerIP & _
                                            ", MachineName:" & m_Services(Count).MachineName & _
                                            "] was not detected! <" & ThisMethod & _
                                            "> has exception: Source = " & ex.Source & _
                                            " | Type : " & ex.GetType.ToString & _
                                            " | Message : " & ex.Message)
                            End If
                        End Try

                        Count = Count + 1
                    End If
                Next

                If Count <= 0 Then
                    If m_Logger.IsErrorEnabled Then
                        m_Logger.Error("There are no any service setting in the Monitor configSet. <" & _
                                ThisMethod & ">")
                    End If

                    Return False
                End If

                m_EnableServiceControl = CType(XMLConfig.GetSettingFromInnerText( _
                        ConfigSet, CONFIGSET_ENABLE_SERVICE_CONTROL, "False"), Boolean)

                m_ThreadInterval = CType(XMLConfig.GetSettingFromInnerText( _
                                ConfigSet, CONFIGSET_THREAD_INTERVAL, "2000"), Integer)
                If (m_ThreadInterval < MINIMUM_THREAD_INTERVAL) Or (m_ThreadInterval > MINIMUM_SERVICE_RESTART_TIMEDELAY) Then
                    m_ThreadInterval = MINIMUM_THREAD_INTERVAL
                End If

                m_SvcRestartTimeDelay = CType(XMLConfig.GetSettingFromInnerText( _
                                ConfigSet, CONFIGSET_SERVICE_RESTART_TIME_DELAY, "10000"), Integer)
                If m_SvcRestartTimeDelay < MINIMUM_SERVICE_RESTART_TIMEDELAY Then
                    m_SvcRestartTimeDelay = MINIMUM_SERVICE_RESTART_TIMEDELAY
                End If

                m_PerfMonTimerInterval = CType(XMLConfig.GetSettingFromInnerText( _
                                ConfigSet, CONFIGSET_PERFMON_INTERVAL, "30000"), Integer)
                If m_PerfMonTimerInterval < MINIMUM_PERFMON_TIMEDELAY Then
                    m_PerfMonTimerInterval = MINIMUM_PERFMON_TIMEDELAY
                End If

                Return True
            Catch ex As Exception
                If m_Logger.IsErrorEnabled Then
                    m_Logger.Error("Exception occurred.<" & ThisMethod & _
                            "> has exception: Source = " & ex.Source & _
                            " | Type : " & ex.GetType.ToString & _
                            " | Message : " & ex.Message)
                End If
                Return False
            End Try
        End Function

        Private Sub Clear(ByRef SvcInfo As ServiceInfo)
            SvcInfo.ServiceName = "Unknown"
            SvcInfo.MachineName = "Unknown"
            SvcInfo.SvcController = Nothing
            SvcInfo.IsExisted = False
            SvcInfo.IsPending = False
        End Sub

#End Region

#Region "6. Class Events Defination."

#End Region

    End Class

End Namespace

