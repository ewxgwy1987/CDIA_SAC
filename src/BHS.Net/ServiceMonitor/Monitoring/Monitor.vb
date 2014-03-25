'=====================================================================================
' Copyright 2009, Xu Jian, All Rights Reserved.
'=====================================================================================
'FileName       Monitor.vb
'Revision:      1.0 -   13 Mar 2009, By hs chia, For Winnipeg International Airport BHS project.
'=====================================================================================

Option Explicit On
Option Strict On

Imports System.Xml
Imports System.Timers
Imports System.Threading
Imports PALS.Common
Imports PALS.Diagnostics
Imports PALS.Utilities

Namespace ServiceMonitor.Monitoring

    Public Class Monitor
        Implements IDisposable

#Region "1. Class Fields Declaration."
        Private Const SERVICES_ALL As String = "ALL"

        'The name of current class 
        Private Shared ReadOnly m_ClassName As String = _
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString
        'Create a logger for use in this class
        Private Shared ReadOnly m_Logger As log4net.ILog = _
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType)

        Private m_ObjectID As String

        Private m_Parameters As BHS.Net.ServiceMonitor.Monitoring.MonitorParameters

        Private m_MonitoringTimer As System.Timers.Timer
        Private m_ThreadCounter As Long
        Private m_LastPerfMonTime As Date

        Private m_PerfMonitor As ClassStatus

#End Region

#Region "2. Class Constructor and Destructor Declaration."
        Public Sub New(ByRef Param As IParameters)
            MyBase.New()

            If Param Is Nothing Then
                Throw New Exception("There is no PALS.Common.AbstractParameters object " & _
                        "was passed to constructer, class " & m_ClassName & _
                        "is unable to be instantiated!")
            Else
                If Not Init(Param) Then
                    Throw New Exception("Unable to initialize object, class " & _
                            m_ClassName & "instantiation failure!")
                End If
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Try
                If m_Logger.IsInfoEnabled Then
                    m_Logger.Info("Class:[" & m_ClassName & "] object is going to be destroyed... <" & _
                            m_ClassName & ".Dispose()>")
                End If

                If Not m_MonitoringTimer Is Nothing Then
                    m_MonitoringTimer.Enabled = False
                    m_MonitoringTimer = Nothing
                End If

                m_Parameters.Dispose()
                m_Parameters = Nothing

                If Not (m_PerfMonitor Is Nothing) Then
                    m_PerfMonitor.Dispose()
                    m_PerfMonitor = Nothing
                End If

                If m_Logger.IsInfoEnabled Then
                    m_Logger.Info("Class:[" & m_ClassName & "] object has been destroyed. <" & _
                            m_ClassName & ".Dispose()>")
                End If
            Catch ex As Exception
                If m_Logger.IsErrorEnabled Then
                    m_Logger.Error("Class:[" & m_ClassName & "] System Error! <" & _
                            m_ClassName & ".Dispose()> Exception: Source = " & ex.Source & _
                            " | Type : " & ex.GetType.ToString & " | Message : " & ex.Message)
                End If
            End Try
        End Sub 'Dispose

        Protected Overrides Sub Finalize()
            MyBase.Finalize()
        End Sub
#End Region

#Region "3. Class Properties Defination."

        Public Property ObjectID() As String
            Get
                Return m_ObjectID
            End Get
            Set(ByVal Value As String)
                m_ObjectID = Value
            End Set
        End Property

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

#End Region

#Region "4. Class Overrides Method Declaration."

#End Region

#Region "5. Class Method Declaration."

        '******************************************************************************
        'Procedure:     Public Function Init
        'Description:   The class initialisation method. This is the main application 
        '               initialization entry point. Instantial all object here.
        'Inputs:        ConfigFileName - ByVal, String, XML config file full name.
        '               args - ByVal, String, Additional command line parameter.
        'Returns:       Return True if initialization success; otherwise return False.
        'Note:          
        '******************************************************************************
        Protected Function Init(ByRef Param As IParameters) As Boolean
            Dim ThisMethod As String = m_ClassName & "." & _
                    System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Try
                If m_Logger.IsInfoEnabled Then
                    m_Logger.Info("Class:[" & m_ClassName & "] object is initializing... <" & _
                            ThisMethod & ">")
                End If

                '##################################################
                'Any initialization tasks can be add in here.
                m_ObjectID = Nothing

                m_Parameters = CType(Param, MonitorParameters)

                m_MonitoringTimer = New System.Timers.Timer(m_Parameters.ThreadInterval)
                AddHandler m_MonitoringTimer.Elapsed, AddressOf OnTimedEvent
                m_MonitoringTimer.Enabled = True

                m_LastPerfMonTime = Now

                m_PerfMonitor = New PALS.Diagnostics.ClassStatus

                If m_Logger.IsInfoEnabled Then
                    m_Logger.Info("Class:[" & m_ClassName & "] object has been initialized. <" & _
                            ThisMethod & ">")
                End If

                Return True
            Catch ex As Exception
                If m_Logger.IsErrorEnabled Then
                    m_Logger.Error("Class:[" & m_ClassName & "] object initialization failure! <" & _
                            ThisMethod & "> Exception: Source = " & ex.Source & _
                            " | Type : " & ex.GetType.ToString & " | Message : " & ex.Message)
                End If

                Return False
            End Try
        End Function

        Public Sub StartMonitoring()
            m_MonitoringTimer.Start()
        End Sub

        Private Sub OnTimedEvent(ByVal source As Object, ByVal e As ElapsedEventArgs)
            Dim ThisMethod As String = m_ClassName & "." & _
               System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Dim SvcState As System.ServiceProcess.ServiceControllerStatus
            Dim TimeDiff As TimeSpan
            Dim i As Integer

            Try
                m_MonitoringTimer.Stop()

                m_ThreadCounter = Functions.CounterIncrease(m_ThreadCounter)

                'Restart all stopped services.
                For i = 0 To m_Parameters.Services.Length - 1
                    If Not (m_Parameters.Services(i).IsExisted) Then
                        If m_Logger.IsWarnEnabled Then
                            m_Logger.Warn("[Service:" & m_Parameters.Services(i).ProcessName & _
                                    ", MachineIP:" & m_Parameters.Services(i).ServerIP & _
                                    ", MachineName:" & m_Parameters.Services(i).MachineName & _
                                    "] is not detected! <" & ThisMethod & ">")
                        End If
                    Else
                        Try
                            m_Parameters.Services(i).SvcController.Refresh()
                            SvcState = Nothing
                            SvcState = m_Parameters.Services(i).SvcController.Status
                            m_Parameters.Services(i).Status = SvcState.ToString

                            'Restart stopped services if its restart timeout is elapsed. 
                            'Performance monitoring only service (e.g. SQL Service) won't be restarted.
                            If (m_Parameters.Services(i).IsPending) And _
                                            (m_Parameters.Services(i).IsControllable) Then
                                TimeDiff = Now.Subtract(m_Parameters.Services(i).StopDetectedTime)

                                If Math.Abs(TimeDiff.TotalMilliseconds) >= m_Parameters.SvcRestartTimeDelay Then
                                    Select Case SvcState
                                        Case ServiceProcess.ServiceControllerStatus.Running
                                            m_Parameters.Services(i).StopDetectedTime = Now
                                            m_Parameters.Services(i).IsPending = False
                                            'Do nothing
                                        Case ServiceProcess.ServiceControllerStatus.Paused
                                            m_Parameters.Services(i).StopDetectedTime = Now
                                            m_Parameters.Services(i).SvcController.[Continue]()

                                            If m_Logger.IsInfoEnabled Then
                                                m_Logger.Info("[Service:" & m_Parameters.Services(i).ProcessName & _
                                                        ", MachineIP:" & m_Parameters.Services(i).ServerIP & _
                                                        "] is being continued automatically... <" & _
                                                        ThisMethod & ">")
                                            End If
                                        Case ServiceProcess.ServiceControllerStatus.Stopped
                                            m_Parameters.Services(i).StopDetectedTime = Now
                                            m_Parameters.Services(i).SvcController.Start()

                                            If m_Logger.IsInfoEnabled Then
                                                m_Logger.Info("[Service:" & m_Parameters.Services(i).ProcessName & _
                                                        ", MachineIP:" & m_Parameters.Services(i).ServerIP & _
                                                        "] is being started automatically... <" & _
                                                        ThisMethod & ">")
                                            End If
                                    End Select
                                End If 'If Math.Abs(TimeDiff.TotalMilliseconds) >= m_Parameters.SvcRestartTimeDelay Then
                            End If 'If (m_Parameters.Services(i).IsStopped) And (m_Parameters.Services(i).IsControllable) Then

                            'Check the service current running status -
                            If Not m_Parameters.Services(i).IsPending Then
                                Select Case SvcState
                                    Case ServiceProcess.ServiceControllerStatus.Running
                                        'Do nothing
                                    Case ServiceProcess.ServiceControllerStatus.Paused
                                        If m_Parameters.Services(i).IsControllable Then
                                            m_Parameters.Services(i).IsPending = True
                                            m_Parameters.Services(i).StopDetectedTime = Now

                                            If m_Logger.IsInfoEnabled Then
                                                m_Logger.Info("[Service:" & m_Parameters.Services(i).ProcessName & _
                                                        ", MachineIP:" & m_Parameters.Services(i).ServerIP & _
                                                        "] is going to be started in " & _
                                                        m_Parameters.SvcRestartTimeDelay.ToString & _
                                                        "ms. <" & ThisMethod & ">")
                                            End If
                                        Else
                                            If m_Logger.IsWarnEnabled Then
                                                m_Logger.Warn("[Service:" & m_Parameters.Services(i).ProcessName & _
                                                        ", MachineIP:" & m_Parameters.Services(i).ServerIP & _
                                                        "] is not running (Current Status:" & _
                                                        ServiceProcess.ServiceControllerStatus.Paused.ToString & _
                                                        "). <" & ThisMethod & ">")
                                            End If
                                        End If
                                    Case ServiceProcess.ServiceControllerStatus.Stopped
                                        If m_Parameters.Services(i).IsControllable Then
                                            m_Parameters.Services(i).IsPending = True
                                            m_Parameters.Services(i).StopDetectedTime = Now

                                            If m_Logger.IsInfoEnabled Then
                                                m_Logger.Info("[Service:" & m_Parameters.Services(i).ProcessName & _
                                                        ", MachineIP:" & m_Parameters.Services(i).ServerIP & _
                                                        "] is going to be started in " & _
                                                        m_Parameters.SvcRestartTimeDelay.ToString & _
                                                        "ms. <" & ThisMethod & ">")
                                            End If
                                        Else
                                            If m_Logger.IsWarnEnabled Then
                                                m_Logger.Warn("[Service:" & m_Parameters.Services(i).ProcessName & _
                                                        ", MachineIP:" & m_Parameters.Services(i).ServerIP & _
                                                        "] is not running (Current Status:" & _
                                                        ServiceProcess.ServiceControllerStatus.Paused.ToString & _
                                                        "). <" & ThisMethod & ">")
                                            End If
                                        End If
                                    Case Else
                                        If m_Logger.IsWarnEnabled Then
                                            m_Logger.Warn("[Service:" & m_Parameters.Services(i).ProcessName & _
                                                    ", MachineIP:" & m_Parameters.Services(i).ServerIP & _
                                                    "] is not running (Current Status:" & _
                                                    ServiceProcess.ServiceControllerStatus.Paused.ToString & _
                                                    "). <" & ThisMethod & ">")
                                        End If
                                        'Do nothing
                                End Select
                            End If

                        Catch ex As Exception
                            If m_Logger.IsErrorEnabled Then
                                m_Logger.Error("Monitoring timer process failure(1)! <" & _
                                        ThisMethod & "> Exception: Source = " & _
                                        ex.Source & " | Type : " & ex.GetType.ToString & _
                                        " | Message : " & ex.Message)
                            End If
                        End Try
                    End If 'If  not (m_Parameters.Services(i).IsExisted) Then

                    Threading.Thread.Sleep(0)
                Next

                'Capture the performance counter and logging.
                TimeDiff = Now.Subtract(m_LastPerfMonTime)
                If Math.Abs(TimeDiff.TotalMilliseconds) >= m_Parameters.PerfMonTimerInterval Then
                    m_LastPerfMonTime = Now

                    If m_Logger.IsInfoEnabled Then
                        m_Logger.Info(" ")
                        m_Logger.Info("[--- START ---]")
                    End If

                    Dim SvcProcess() As Process = Nothing
                    For i = 0 To m_Parameters.Services.Length - 1
                        Try
                            If m_Parameters.Services(i).IsExisted Then
                                SvcProcess = Nothing

                                SvcProcess = Process.GetProcessesByName(m_Parameters.Services(i).ProcessName, _
                                                                                m_Parameters.Services(i).ServerIP)
                                If SvcProcess.Length > 0 Then
                                    If m_Logger.IsInfoEnabled Then
                                        m_Logger.Info("[Process:" & m_Parameters.Services(i).ProcessName & _
                                            ", MachineIP:" & m_Parameters.Services(i).ServerIP & _
                                            "] PhysicalMemUsage:" & SvcProcess(0).WorkingSet64.ToString & _
                                            ", BasePriority:" & SvcProcess(0).BasePriority.ToString & _
                                            ", TotalThreads:" & SvcProcess(0).Threads.Count.ToString & _
                                            ". <" & ThisMethod & ">")
                                    End If
                                Else
                                    If m_Logger.IsWarnEnabled Then
                                        m_Logger.Warn("[Process:" & m_Parameters.Services(i).ProcessName & _
                                            ", MachineIP:" & m_Parameters.Services(i).ServerIP & _
                                            "] was not yet started. No performance monitoring on it. <" & _
                                            ThisMethod & ">")
                                    End If
                                End If
                            Else
                                If m_Logger.IsWarnEnabled Then
                                    m_Logger.Warn("[Process:" & m_Parameters.Services(i).ProcessName & _
                                        ", MachineIP:" & m_Parameters.Services(i).ServerIP & _
                                        "] is not detected. No performance monitoring on it. <" & _
                                        ThisMethod & ">")
                                End If
                            End If

                        Catch ex As Exception
                            If m_Logger.IsErrorEnabled Then
                                m_Logger.Error("Monitoring timer process failure(2)! <" & _
                                        ThisMethod & "> Exception: Source = " & _
                                        ex.Source & " | Type : " & ex.GetType.ToString & _
                                        " | Message : " & ex.Message)
                            End If
                        End Try
                    Next

                    If m_Logger.IsInfoEnabled Then
                        m_Logger.Info("[---  END  ---]")
                    End If
                End If

            Catch ex As Exception
                If m_Logger.IsErrorEnabled Then
                    m_Logger.Error("Exception occurred.<" & ThisMethod & _
                            "> has exception: Source = " & ex.Source & _
                            " | Type : " & ex.GetType.ToString & _
                            " | Message : " & ex.Message)
                End If
            Finally
                m_MonitoringTimer.Start()
            End Try
        End Sub

        Private Sub StartStopService(ByRef Service As ServiceInfo, ByVal Command As StartStopCommand)
            Dim ThisMethod As String = m_ClassName & "." & _
                     System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Service.SvcController.Refresh()

            Select Case Service.SvcController.Status
                Case ServiceProcess.ServiceControllerStatus.ContinuePending 'The service continue is pending.
                    If Command = StartStopCommand.cmdStart Then
                        If m_Logger.IsInfoEnabled Then
                            m_Logger.Info("[" & Service.ServiceName & "] service is in " & _
                                    ServiceProcess.ServiceControllerStatus.ContinuePending.ToString & _
                                    " status, no start action will be taken. <" & ThisMethod & ">")
                        End If
                    Else
                        If m_Logger.IsInfoEnabled Then
                            m_Logger.Info("[" & Service.ServiceName & "] service is in " & _
                                    ServiceProcess.ServiceControllerStatus.ContinuePending.ToString & _
                                    " status, it will be stopped. <" & ThisMethod & ">")
                        End If
                        Service.SvcController.Stop()
                    End If
                Case ServiceProcess.ServiceControllerStatus.Paused 'The service is paused.
                    If Command = StartStopCommand.cmdStart Then
                        If m_Logger.IsInfoEnabled Then
                            m_Logger.Info("[" & Service.ServiceName & "] service is in " & _
                                    ServiceProcess.ServiceControllerStatus.ContinuePending.ToString & _
                                    " status, it will be continued. <" & ThisMethod & ">")
                        End If
                        Service.SvcController.[Continue]()
                    Else
                        If m_Logger.IsInfoEnabled Then
                            m_Logger.Info("[" & Service.ServiceName & "] service is in " & _
                                    ServiceProcess.ServiceControllerStatus.ContinuePending.ToString & _
                                    " status, it will be stopped. <" & ThisMethod & ">")
                        End If
                        Service.SvcController.Stop()
                    End If
                Case ServiceProcess.ServiceControllerStatus.PausePending 'The service pause is pending.
                    If Command = StartStopCommand.cmdStart Then
                        If m_Logger.IsInfoEnabled Then
                            m_Logger.Info("[" & Service.ServiceName & "] service is in " & _
                                    ServiceProcess.ServiceControllerStatus.ContinuePending.ToString & _
                                    " status, it will be continued. <" & ThisMethod & ">")
                        End If
                        Service.SvcController.[Continue]()
                    Else
                        If m_Logger.IsInfoEnabled Then
                            m_Logger.Info("[" & Service.ServiceName & "] service is in " & _
                                    ServiceProcess.ServiceControllerStatus.ContinuePending.ToString & _
                                    " status, it will be stopped. <" & ThisMethod & ">")
                        End If
                        Service.SvcController.Stop()
                    End If
                Case ServiceProcess.ServiceControllerStatus.Running 'The service is running.
                    If Command = StartStopCommand.cmdStart Then
                        If m_Logger.IsInfoEnabled Then
                            m_Logger.Info("[" & Service.ServiceName & "] service is in " & _
                                    ServiceProcess.ServiceControllerStatus.ContinuePending.ToString & _
                                    " status, no start action will be taken. <" & ThisMethod & ">")
                        End If
                    Else
                        If m_Logger.IsInfoEnabled Then
                            m_Logger.Info("[" & Service.ServiceName & "] service is in " & _
                                    ServiceProcess.ServiceControllerStatus.ContinuePending.ToString & _
                                    " status, it will be stopped. <" & ThisMethod & ">")
                        End If
                        Service.SvcController.Stop()
                    End If
                Case ServiceProcess.ServiceControllerStatus.StartPending 'The service is starting.
                    If Command = StartStopCommand.cmdStart Then
                        If m_Logger.IsInfoEnabled Then
                            m_Logger.Info("[" & Service.ServiceName & "] service is in " & _
                                    ServiceProcess.ServiceControllerStatus.ContinuePending.ToString & _
                                    " status, no start action will be taken. <" & ThisMethod & ">")
                        End If
                    Else
                        If m_Logger.IsInfoEnabled Then
                            m_Logger.Info("[" & Service.ServiceName & "] service is in " & _
                                    ServiceProcess.ServiceControllerStatus.ContinuePending.ToString & _
                                    " status, it will be stopped. <" & ThisMethod & ">")
                        End If
                        Service.SvcController.Stop()
                    End If
                Case ServiceProcess.ServiceControllerStatus.Stopped 'The service is not running.
                    If Command = StartStopCommand.cmdStart Then
                        If m_Logger.IsInfoEnabled Then
                            m_Logger.Info("[" & Service.ServiceName & "] service is in " & _
                                    ServiceProcess.ServiceControllerStatus.ContinuePending.ToString & _
                                    " status, it will be started. <" & ThisMethod & ">")
                        End If
                        Service.SvcController.Start()
                    Else
                        If m_Logger.IsInfoEnabled Then
                            m_Logger.Info("[" & Service.ServiceName & "] service is in " & _
                                    ServiceProcess.ServiceControllerStatus.ContinuePending.ToString & _
                                    " status, no stop action will be taken. <" & ThisMethod & ">")
                        End If
                    End If
                Case ServiceProcess.ServiceControllerStatus.StopPending 'The service is stopping.
                    If Command = StartStopCommand.cmdStart Then
                        If m_Logger.IsInfoEnabled Then
                            m_Logger.Info("[" & Service.ServiceName & "] service is in " & _
                                    ServiceProcess.ServiceControllerStatus.ContinuePending.ToString & _
                                    " status, it will be started. <" & ThisMethod & ">")
                        End If
                        Service.SvcController.Start()
                    Else
                        If m_Logger.IsInfoEnabled Then
                            m_Logger.Info("[" & Service.ServiceName & "] service is in " & _
                                    ServiceProcess.ServiceControllerStatus.ContinuePending.ToString & _
                                    " status, no stop action will be taken. <" & ThisMethod & ">")
                        End If
                    End If
            End Select
        End Sub

        Private Function IsValidService(ByVal Name As String, _
                    ByRef Service As ServiceInfo) As Boolean
            Dim ThisMethod As String = m_ClassName & "." & _
                      System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Dim i As Integer
            For i = 0 To m_Parameters.Services.Length - 1
                If Name = m_Parameters.Services(i).ServiceName Then
                    If m_Parameters.Services(i).IsExisted Then
                        If m_Parameters.Services(i).IsControllable Then
                            Service = m_Parameters.Services(i)
                            Return True
                        Else
                            If m_Logger.IsErrorEnabled Then
                                m_Logger.Error("[" & Name & "] service(s) is not controllable " & _
                                        "from BHS_SvcMonitor service! <" & ThisMethod & ">")
                            End If
                            Service = Nothing
                            Return False
                        End If
                    Else
                        If m_Logger.IsErrorEnabled Then
                            m_Logger.Error("[" & Name & "] service(s) is not detected! <" & _
                                        ThisMethod & ">")
                        End If
                        Service = Nothing
                        Return False
                    End If
                End If
            Next

            If m_Logger.IsErrorEnabled Then
                m_Logger.Error("[" & Name & "] service(s) is not registered in the BHS_SvcMonitor service! <" & _
                            ThisMethod & ">")
            End If
            Service = Nothing
            Return False
        End Function

        Public Sub OnServiceStartRequest_MessageHandler(ByVal Services As String)
            Dim ThisMethod As String = m_ClassName & "." & _
                       System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Try
                If Not m_Parameters.EnableServiceControl Then
                    If m_Logger.IsInfoEnabled Then
                        m_Logger.Info("Service control feature was disabled in the configuration file, " & _
                                "no service can be started. <" & ThisMethod & ">")
                    End If

                    Exit Sub
                Else
                    If m_Logger.IsInfoEnabled Then
                        m_Logger.Info("[" & Services & "] service(s) is going to be started... <" & _
                                    ThisMethod & ">")
                    End If
                End If

                Dim i, Len As Integer
                Dim Names() As String
                If Services = SERVICES_ALL Then
                    Len = m_Parameters.Services.Length
                    ReDim Names(Len - 1)
                    For i = 0 To Len - 1
                        Names(i) = m_Parameters.Services(i).ServiceName
                    Next
                Else
                    Names = Split(Services, ",", , CompareMethod.Binary)
                End If

                Dim Service As ServiceInfo
                For i = 0 To Names.Length - 1
                    Service = Nothing
                    If IsValidService(Names(i), Service) Then
                        StartStopService(Service, StartStopCommand.cmdStart)
                    End If
                Next

            Catch ex As Exception
                If m_Logger.IsErrorEnabled Then
                    m_Logger.Error("Exception occurred.<" & ThisMethod & _
                            "> has exception: Source = " & ex.Source & _
                            " | Type : " & ex.GetType.ToString & _
                            " | Message : " & ex.Message)
                End If
            End Try
        End Sub

        Public Sub OnServiceStopRequest_MessageHandler(ByVal Services As String)
            Dim ThisMethod As String = m_ClassName & "." & _
                       System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Try
                If Not m_Parameters.EnableServiceControl Then
                    If m_Logger.IsInfoEnabled Then
                        m_Logger.Info("Service control feature was disabled in the configuration file, " & _
                                "no service can be stopped. <" & ThisMethod & ">")
                    End If

                    Exit Sub
                Else
                    If m_Logger.IsInfoEnabled Then
                        m_Logger.Info("[" & Services & "] service(s) is going to be stopped... <" & _
                                    ThisMethod & ">")
                    End If
                End If

                Dim i, Len As Integer
                Dim Names() As String
                If Services = SERVICES_ALL Then
                    Len = m_Parameters.Services.Length
                    ReDim Names(Len - 1)
                    For i = 0 To Len - 1
                        Names(i) = m_Parameters.Services(i).ServiceName
                    Next
                Else
                    Names = Split(Services, ",", , CompareMethod.Binary)
                End If

                Dim Service As ServiceInfo
                For i = 0 To Names.Length - 1
                    Service = Nothing
                    If IsValidService(Names(i), Service) Then
                        StartStopService(Service, StartStopCommand.cmdStop)
                    End If
                Next

            Catch ex As Exception
                If m_Logger.IsErrorEnabled Then
                    m_Logger.Error("Exception occurred.<" & ThisMethod & _
                            "> has exception: Source = " & ex.Source & _
                            " | Type : " & ex.GetType.ToString & _
                            " | Message : " & ex.Message)
                End If
            End Try
        End Sub

        '<status>
        '	<object id="2">
        '		<class>DBT2_BHS.ServiceMonitor.Monitoring.Monitor</class>
        '		<threadCounter>44</threadCounter>
        '		<services>
        '			<service>
        '				<serviceName>MSSQL$DBINS</serviceName>
        '				<processName>sqlservr</processName>
        '				<machineName>T-MSSQL1</machineName>
        '				<serverIP>192.168.15.100</serverIP>
        '				<isControllable>False</isControllable>
        '				<isExisted>True</isExisted>
        '				<status>Running</status>
        '			</service>
        '			<service>
        '				<serviceName>BHS_SAC2PLC2GW</serviceName>
        '				<processName>BHS_SAC2PLC2GW</processName>
        '				<machineName>T-MSSQL1</machineName>
        '				<serverIP>192.168.15.100</serverIP>
        '				<isControllable>True</isControllable>
        '				<isExisted>True</isExisted>
        '				<status>Running</status>
        '			</service>
        '			<service>
        '				<serviceName>BHS_SAC2PLC4GW</serviceName>
        '				<processName>BHS_SAC2PLC4GW</processName>
        '				<machineName>T-MSSQL1</machineName>
        '				<serverIP>192.168.15.100</serverIP>
        '				<isControllable>True</isControllable>
        '				<isExisted>True</isExisted>
        '				<status>Running</status>
        '			</service>
        '			<service>
        '				<serviceName>BHS_SortEngine</serviceName>
        '				<processName>BHS_SortEngine</processName>
        '				<machineName>T-MSSQL1</machineName>
        '				<serverIP>192.168.15.100</serverIP>
        '				<isControllable>True</isControllable>
        '				<isExisted>True</isExisted>
        '				<status>Running</status>
        '			</service>
        '			<service>
        '				<serviceName>BHS_SvcMonitor</serviceName>
        '				<processName>BHS_SvcMonitor</processName>
        '				<machineName>T-MSSQL1</machineName>
        '				<serverIP>192.168.15.100</serverIP>
        '				<isControllable>True</isControllable>
        '				<isExisted>True</isExisted>
        '				<status>Running</status>
        '			</service>
        '		</services>
        '	</object>
        '</status>
        Private Sub PerfCounterRefresh()
            Dim ThisMethod As String = m_ClassName & "." & _
                       System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Try
                If (Not m_PerfMonitor Is Nothing) And (Not m_ObjectID Is Nothing) Then
                    m_PerfMonitor.OpenObjectNode()

                    m_PerfMonitor.AddObjectStatus("class", m_ClassName)
                    m_PerfMonitor.AddObjectStatus("threadCounter", m_ThreadCounter.ToString)

                    m_PerfMonitor.AddStartElementOfSubNode("services")
                    Dim i As Integer

                    For i = 0 To m_Parameters.Services.Length - 1
                        m_PerfMonitor.AddStartElementOfSubNode("service")
                        m_PerfMonitor.AddObjectStatus("serviceName", m_Parameters.Services(i).ServiceName)
                        m_PerfMonitor.AddObjectStatus("processName", m_Parameters.Services(i).ProcessName)
                        m_PerfMonitor.AddObjectStatus("machineName", m_Parameters.Services(i).MachineName)
                        m_PerfMonitor.AddObjectStatus("serverIP", m_Parameters.Services(i).ServerIP)
                        m_PerfMonitor.AddObjectStatus("isControllable", m_Parameters.Services(i).IsControllable.ToString)
                        m_PerfMonitor.AddObjectStatus("isExisted", m_Parameters.Services(i).IsExisted.ToString)
                        m_PerfMonitor.AddObjectStatus("status", m_Parameters.Services(i).Status)
                        m_PerfMonitor.AddEndElementOfSubNode()
                    Next
                    m_PerfMonitor.AddEndElementOfSubNode()

                    m_PerfMonitor.CloseObjectNode()
                End If

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

