'=====================================================================================
' Copyright 2009, Xu Jian, All Rights Reserved.
'=====================================================================================
'FileName       MessageHandler.vb
'Revision:      1.0 -   13 Mar 2009, By hs chia, For Winnipeg International Airport BHS project.
'=====================================================================================

Option Explicit On
Option Strict On

Imports System.Threading
Imports PALS.Common
Imports PALS.Diagnostics
Imports PALS.Telegrams
Imports PALS.Telegrams.Common
Imports PALS.Utilities

Namespace ServiceMonitor.Application

    ''' -----------------------------------------------------------------------------
    ''' Project	 : BHS
    ''' Class	 : ServiceMonitor.Application.MessageHandler
    ''' 
    ''' -----------------------------------------------------------------------------
    ''' <summary>
    ''' <para>All received messages will be buffered into class internal incoming
    ''' queue before data handling process is taken. Only one message queue was 
    ''' implemented for all incoming messages that are received from both
    ''' MessageCollector and MessageForwarder classes.</para>
    ''' 
    ''' <para>There is no outgoing message queue was implemented in this layer class.
    ''' It because all acknowledge required outgoing messages will be buffered by
    ''' bottom ACK class. Such message won't be lost in case of the sending process
    ''' failure or it is not acknowledged. But for those acknowledge unrequired 
    ''' message, they are not buffered in any layer. Such message will be sent and 
    ''' forget. Hence, if the connection is broken at the time of Send() method
    ''' is invoked, this acknowledge unrequired message will be lost. Hence, all
    ''' critical messages should be defined as the acknowledge required message in 
    ''' the interface protocol design.</para>
    ''' 
    ''' <para>According to interface protocol design (ItemTracking protocol and 
    ''' TCPServer2Client protocol), the ItemTracking message will only come from the
    ''' SAC2PLC-GW service to PLC interfaces; the BagEvent (BEV) message will only come 
    ''' from the SAC2PLC-GW service to SortEngent service interface; the Running 
    ''' Status Request (SRQ) and Reply (SRP) message will only come from the 
    ''' SAC2PLC-GW service to BHSConsole GUI application interface.</para>
    ''' 
    ''' <para>All incoming ItemTracking protocol messages, which are received from 
    ''' remote PLC via TCPClient channel, will be encoded into BagEvent message and 
    ''' sent to SortEngine via TCPServer channel.</para>
    ''' 
    ''' <para>All incoming BagEvent protocol messages, which are received from 
    ''' SortEngine via TCPServer channel, will be decoded. If the decoded data is 
    ''' the ItemTracking protocol message, then the decoded data will be sent to
    ''' remote PLC via TCPClient channel.</para>
    ''' 
    ''' <para>If the received message is the Running Status Request (SRQ) message, then
    ''' the system classes internal status will be collected by this class and 
    ''' encapsulated into Running Status Reply (SRP) message and sent to remote 
    ''' requester. According to desing, the SRQ and SRP messages will be received 
    ''' and sent only via TCPServer channel to remote BHSConsole application, not via
    ''' TCPClient channel to remote PLCs.</para>
    ''' 
    ''' <para>One DataHandlingThread is implemented in the class to handle all 
    ''' received messages that were buffered in the incoming message queue.</para>
    ''' 
    ''' <para>In order to use TCPClient class, the follow settings has to be defined
    ''' in the XML configuration file. They are:</para>
    ''' <para><![CDATA[
    '''	    <configSet name="Telegram_Formats">
    '''         <!--The "" or "?" shall be used if the value of attributes is not constant.-->
    '''         <!--The value of offset and length attributes is number of bytes -->
    '''         <!--The "acknowledge" indicates whether this message is the acknowledgement required message -->
    '''         <!--The "sequence" indicates whether this sequence field need to be assigned the new value before sent out -->
    '''         <!--The "alias" attribute of "telegram" node is constant value for all projects-->
    '''         <!--The "name" attribute of "field" node is constant value for all projects-->
    '''         <telegramSet name="Application_Telegrams">
    '''             <telegram alias="SRQ" name="Status_Request_Message" sequence="True" acknowledge="False">
    '''	                <field name="Type" offset="0" length="4" default="49,48,48,49"/>
    '''	                <field name="Length" offset="4" length="4" default="?"/>
    '''	                <field name="Sequence" offset="8" length="4" default="?"/>
    '''	                <field name="Class" offset="12" length="?" default="?"/>
    '''             </telegram>		
    '''             <telegram alias="SRP" name="Status_Reply_Message" sequence="False" acknowledge="False">
    '''       	        <field name="Type" offset="0" length="4" default="49,48,48,50"/>
    '''       	        <field name="Length" offset="4" length="4" default="?"/>
    '''       	        <field name="Sequence" offset="8" length="4" default="?"/>
    '''       	        <field name="Status" offset="12" length="?" default="?"/>
    '''             </telegram>		
    ''' 			<telegram alias="STR" name="Service_Start_Command_Message" sequence="True" acknowledge="False">
    ''' 				<field name="Type" offset="0" length="4" default="48,49,48,52"/>
    ''' 				<field name="Length" offset="4" length="4" default="?"/>
    ''' 				<field name="Sequence" offset="8" length="4" default="?"/>
    ''' 				<field name="Services" offset="12" length="?" default="?"/>
    ''' 			</telegram>		
    ''' 			<telegram alias="STO" name="Service_Stop_Command_Message" sequence="True" acknowledge="False">
    ''' 				<field name="Type" offset="0" length="4" default="48,49,48,53"/>
    ''' 				<field name="Length" offset="4" length="4" default="?"/>
    ''' 				<field name="Sequence" offset="8" length="4" default="?"/>
    ''' 				<field name="Services" offset="12" length="?" default="?"/>
    ''' 			</telegram>		
    '''	        </telegramSet>	
    '''     </configSet>	
    ''' ]]></para> 
    ''' 
    ''' </summary>
    ''' <remarks>
    ''' </remarks>
    ''' <history>
    ''' 	[xujian]	12/5/2005	Created
    ''' </history>
    ''' -----------------------------------------------------------------------------
    Public Class MessageHandler
        Implements IDisposable

#Region "1. Class Fields Declaration."

        Private Const THREAD_INTERVAL As Integer = 10 '10 millisecond
        Private Const SOURCE_SERVER_CHANNEL As String = "SERVER"
        Private Const MIN_SRQ_INTERVAL As Integer = 500 '1000 millisecond
        Private Const RUNNING_STATUS_ALL As String = "ALL"

        'The name of current class 
        Private Shared ReadOnly m_ClassName As String = _
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString
        'Create a logger for use in this class
        Private Shared ReadOnly m_Logger As log4net.ILog = _
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType)
        Private m_ObjectID As String

        Private m_Parameters As BHS.Net.ServiceMonitor.Application.MessageHandlerParameters

        Private m_IncomingQueue As Queue
        Private m_SyncedIncomingQueue As Queue

        Private m_HandlingThread As Thread

        Private m_ServerForwarder As Net.Handlers.ServerMsgForwarder

        Private m_LastSRQReceivedTime As Date

        Private m_ThreadCounter As Long
        Private m_NoOfMsgFromSvrChannel As Long
        Private m_NoOfMsgToSvrChannel As Long
        Private m_NoOfDiscardedMessage As Long
        Private m_NoOfSRQ As Long
        Private m_NoOfSTR As Long
        Private m_NoOfSTO As Long

        Private _PerfMonitor As ClassStatus
        'The Hashtable that contains the ClassStatus object of current class 
        'and all of its instance of sub classes.
        Private _PerfMonitorList As ArrayList
#End Region

#Region "2. Class Constructor and Destructor Declaration."

        Public Sub New(ByRef Param As IParameters, _
                        ByRef ServerForwarder As Net.Handlers.ServerMsgForwarder)
            MyBase.New()

            If ServerForwarder Is Nothing Then
                Throw New Exception("ServerMsgForwarder object is Null, class " & _
                        m_ClassName & "instantiation failure!")
            End If

            If Param Is Nothing Then
                Throw New Exception("There is no PALS.Common.AbstractParameters object " & _
                        "was passed to constructer, class " & m_ClassName & _
                        "is unable to be instantiated!")
            Else
                If Not Init(Param, ServerForwarder) Then
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

                'Terminate the Incoming data handling Thread
                If Not (m_HandlingThread Is Nothing) Then
                    m_HandlingThread.Abort()
                    m_HandlingThread.Join()
                    m_HandlingThread = Nothing
                End If

                If Not m_SyncedIncomingQueue Is Nothing Then
                    m_SyncedIncomingQueue.Clear()
                    m_SyncedIncomingQueue = Nothing
                End If

                m_Parameters.Dispose()
                m_Parameters = Nothing

                If Not (_PerfMonitor Is Nothing) Then
                    _PerfMonitor.Dispose()
                    _PerfMonitor = Nothing
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

#End Region

#Region "3. Class Property Declaration."

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
        ''' The reference to the instant of DBT2_BHS.ServiceMonitor.Net.Handlers.ServerMsgForwarder
        ''' class. 
        ''' 
        ''' This class property was assigned with the actual value by class constructor. 
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' </remarks>
        ''' <history>
        ''' 	[xujian]	12/19/2005	Created
        ''' </history>
        ''' -----------------------------------------------------------------------------
        Public Property ServerForwarder() As Net.Handlers.ServerMsgForwarder
            Get
                Return m_ServerForwarder
            End Get
            Set(ByVal Value As Net.Handlers.ServerMsgForwarder)
                m_ServerForwarder = Value
            End Set
        End Property

        Public Property PerfMonitorList() As ArrayList
            Get
                Return _PerfMonitorList
            End Get
            Set(ByVal Value As ArrayList)
                _PerfMonitorList = Value
            End Set
        End Property

        Public ReadOnly Property PerfMonitor() As PALS.Diagnostics.ClassStatus
            Get
                Try
                    'Refresh current class perfermance monitoring counters.
                    _PerfMonitor.ObjectID = m_ObjectID
                    PerfCounterRefresh()

                    Return _PerfMonitor
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

#Region "5. Class Method Declaration."

        Protected Function Init(ByRef Param As IParameters, _
                        ByRef ServerForwarder As Net.Handlers.ServerMsgForwarder) As Boolean
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

                m_Parameters = CType(Param, MessageHandlerParameters)

                m_ServerForwarder = ServerForwarder
                AddHandler m_ServerForwarder.OnReceived, AddressOf OnReceived_ServerForwarder

                m_IncomingQueue = New Queue
                m_SyncedIncomingQueue = Queue.Synchronized(m_IncomingQueue)
                m_SyncedIncomingQueue.Clear()

                'Create Handling Thread object
                m_HandlingThread = New System.Threading.Thread(AddressOf DataHandlingThread)
                m_HandlingThread.Name = m_ClassName & ".DataHandlingThread"

                m_ThreadCounter = 0
                m_NoOfMsgFromSvrChannel = 0
                m_NoOfMsgToSvrChannel = 0
                m_NoOfDiscardedMessage = 0
                m_NoOfSRQ = 0
                m_NoOfSTR = 0
                m_NoOfSTO = 0

                _PerfMonitor = New PALS.Diagnostics.ClassStatus

                m_LastSRQReceivedTime = Now

                m_HandlingThread.Start()
                Thread.Sleep(0)
                '##################################################

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

        Private Sub DataHandlingThread()
            Dim ThisMethod As String = m_ClassName & "." & _
                    System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Try
                If m_Logger.IsInfoEnabled Then
                    m_Logger.Info("Class:[" & m_ClassName & _
                            "] Incoming message data handling thread has been started! <" & ThisMethod & ">")
                End If

                Dim i, Count As Integer
                Dim MsgSouc As MessageAndSource
                While (True)
                    Count = 0
                    Count = m_SyncedIncomingQueue.Count

                    For i = 0 To Count - 1
                        MsgSouc = Nothing
                        MsgSouc = CType(m_SyncedIncomingQueue.Dequeue, MessageAndSource)

                        IncomingMessageHandling(MsgSouc)
                    Next

                    m_ThreadCounter = Functions.CounterIncrease(m_ThreadCounter)
                    Threading.Thread.Sleep(THREAD_INTERVAL)
                End While

                If m_Logger.IsInfoEnabled Then
                    m_Logger.Info("Class:[" & m_ClassName & _
                            "] Incoming message data handling thread has been exited! <" & ThisMethod & ">")
                End If

            Catch ex As ThreadAbortException
                Thread.ResetAbort()

                If m_Logger.IsInfoEnabled Then
                    m_Logger.Info("Class:[" & m_ClassName & _
                            "] Incoming message data handling thread has been exited! <" & ThisMethod & ">")
                End If
            Catch ex As Exception
                If m_Logger.IsErrorEnabled Then
                    m_Logger.Error("Incoming message data handling thread failure! <" & _
                            ThisMethod & "> Exception: Source = " & _
                            ex.Source & " | Type : " & ex.GetType.ToString & _
                            " | Message : " & ex.Message)
                End If
            End Try
        End Sub

        ''' <para>According to interface protocol design (ItemTracking protocol and 
        ''' TCPServer2Client protocol), the ItemTracking message will only come from the
        ''' SAC2PLC-GW service to PLC interfaces; the BagEvent (BEV) message will only come 
        ''' from the SAC2PLC-GW service to SortEngent service interface; the Running 
        ''' Status Request (SRQ) and Reply (SRP) message will only come from the 
        ''' SAC2PLC-GW service to BHSConsole GUI application interface.</para>
        Private Sub IncomingMessageHandling(ByRef MsgSouc As MessageAndSource)
            Dim ThisMethod As String = m_ClassName & "." & _
                    System.Reflection.MethodBase.GetCurrentMethod().Name & "()"
            Dim Source As String
            Dim Message As Telegram

            Try
                If MsgSouc Is Nothing Then
                    Exit Sub
                Else
                    Source = MsgSouc.Source
                    Message = MsgSouc.Message
                End If

                If Message.Format Is Nothing Then
                    If m_Logger.IsErrorEnabled Then
                        m_Logger.Error("[Channel:" & Message.ChannelName & _
                                "] TelegramFormat was not defined for this incoming message! " & _
                                "Message will be discarded... [Msg(APP):" & _
                                Message.ToString(HexToStrMode.ToAscPaddedHexString) & _
                                "]. <" & ThisMethod & ">")
                    End If

                    m_NoOfDiscardedMessage = Functions.CounterIncrease(m_NoOfDiscardedMessage)
                    Exit Sub
                Else
                    Dim Type As String
                    Type = Functions.ConvertByteArrayToString( _
                            Message.GetFieldActualValue("Type"), , HexToStrMode.ToAscString)

                    'The SRQ telegram only comes from TCP Server channel.
                    If (Source = SOURCE_SERVER_CHANNEL) Then
                        Select Case Type
                            Case m_Parameters.SRQMessageType
                                m_NoOfSRQ = Functions.CounterIncrease(m_NoOfSRQ)

                                'If SRQ message was received from BHSConsole via TCPServer channel,
                                'then the SRP message will be created and sent to BHSConsole
                                RunningStatusReply(Message)
                            Case m_Parameters.STRMessageType
                                'Service Start Command telegram is received
                                m_NoOfSTR = Functions.CounterIncrease(m_NoOfSTR)

                                If m_Logger.IsInfoEnabled Then
                                    m_Logger.Info("[Channel:" & Message.ChannelName & _
                                            "] [Msg(" & Message.Format.AliasName & "):" & _
                                            Message.ToString(HexToStrMode.ToAscPaddedHexString) & _
                                            "] Service START Command is received. <" & ThisMethod & ">")
                                End If

                                Dim Services As String
                                Message.Format.Field("Services").Length = Message.RawData.Length - _
                                                Message.Format.Field("Services").Offset
                                Services = Trim(Functions.ConvertByteArrayToString( _
                                                Message.GetFieldActualValue("Services"), , _
                                                HexToStrMode.ToAscString))

                                RaiseEvent OnServiceStartRequest(Services)

                            Case m_Parameters.STOMessageType
                                'Service Stop Command telegram is received
                                m_NoOfSTO = Functions.CounterIncrease(m_NoOfSTO)

                                If m_Logger.IsInfoEnabled Then
                                    m_Logger.Info("[Channel:" & Message.ChannelName & _
                                            "] [Msg(" & _
                                            Message.Format.AliasName & "):" & _
                                            Message.ToString(HexToStrMode.ToAscPaddedHexString) & _
                                            "] Service STOP Command is received. <" & ThisMethod & ">")
                                End If

                                Dim Services As String
                                Message.Format.Field("Services").Length = Message.RawData.Length - _
                                                Message.Format.Field("Services").Offset
                                Services = Trim(Functions.ConvertByteArrayToString( _
                                                Message.GetFieldActualValue("Services"), , _
                                                HexToStrMode.ToAscString))

                                RaiseEvent OnServiceStopRequest(Services)

                            Case Else
                                m_NoOfDiscardedMessage = Functions.CounterIncrease(m_NoOfDiscardedMessage)

                                'Undesired message will be discarded.
                                If m_Logger.IsErrorEnabled Then
                                    m_Logger.Error("Undesired message was received from channel " & _
                                            Message.ChannelName & ", it " & _
                                            "will be discarded... [Msg(APP):" & _
                                            Message.ToString(HexToStrMode.ToAscPaddedHexString) & _
                                            "]. <" & ThisMethod & ">")
                                End If
                        End Select
                    Else
                        m_NoOfDiscardedMessage = Functions.CounterIncrease(m_NoOfDiscardedMessage)

                        'Undesired message will be discarded.
                        If m_Logger.IsErrorEnabled Then
                            m_Logger.Error("Undesired message was received from channel " & _
                                    Message.ChannelName & ", it " & _
                                    "will be discarded... [Msg(APP):" & _
                                    Message.ToString(HexToStrMode.ToAscPaddedHexString) & _
                                    "]. <" & ThisMethod & ">")
                        End If
                    End If
                End If

            Catch ex As Exception
                If m_Logger.IsErrorEnabled Then
                    m_Logger.Error("Exception occurs! <" & _
                            ThisMethod & "> Exception: Source = " & ex.Source & _
                            " | Type : " & ex.GetType.ToString & " | Message : " & ex.Message)
                End If
            End Try
        End Sub

        'If SRQ message was received from BHSConsole via TCPServer channel,
        'then the SRP message will be created and sent to BHSConsole
        '<telegram alias="SRQ" name="Status_Request_Message" sequence="True" acknowledge="False">
        '	<field name="Type" offset="0" length="4" default="49,48,48,49"/>
        '	<field name="Length" offset="4" length="4" default="?"/>
        '	<field name="Sequence" offset="8" length="4" default="?"/>
        '	<field name="Class" offset="12" length="?" default="?"/>
        '</telegram>		
        Private Sub RunningStatusReply(ByRef Message As Telegram)
            Dim ThisMethod As String = m_ClassName & "." & _
                        System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Try
                If m_Logger.IsInfoEnabled Then
                    m_Logger.Info("SRQ Message was received from channel " & _
                            Message.ChannelName & "... [Msg(" & _
                            Message.Format.AliasName & "):" & _
                            Message.ToString(HexToStrMode.ToAscPaddedHexString) & _
                            "]. <" & ThisMethod & ">")
                End If

                If Not _PerfMonitorList Is Nothing Then
                    Dim TimeDiff As TimeSpan
                    'If the time interval of SRQ message received is less than 1 second,
                    'then no SRP will be created and sent to remote.
                    TimeDiff = Now.Subtract(m_LastSRQReceivedTime)
                    If Math.Abs(TimeDiff.TotalMilliseconds) < MIN_SRQ_INTERVAL Then
                        If m_Logger.IsInfoEnabled Then
                            m_Logger.Info("The SRQ receiving time interval is too short (<" & _
                                    MIN_SRQ_INTERVAL.ToString & _
                                    "ms), no SRP Message will be sent. <" & ThisMethod & ">")
                        End If
                    Else
                        'Fire event to inform Initializer object to refresh all object
                        'running status counters.
                        RaiseEvent OnStatusRequested()
                        Thread.Sleep(0)

                        '	<field name="Class" offset="12" length="?" default="?"/>
                        Dim Cls As String
                        Message.Format.Field("Class").Length = Message.RawData.Length - _
                                    Message.Format.Field("Class").Offset
                        Cls = Trim(Functions.ConvertByteArrayToString( _
                                    Message.GetFieldActualValue("Class"), , _
                                    HexToStrMode.ToAscString))

                        Dim IsAll As Boolean
                        Dim Classes() As String
                        Dim i As Integer
                        Dim IDList As ArrayList = Nothing
                        If Cls = RUNNING_STATUS_ALL Then
                            IsAll = True
                        Else
                            IsAll = False
                            Classes = Split(Cls, ",", , CompareMethod.Binary)
                            IDList = New ArrayList
                            For i = 0 To Classes.Length - 1
                                IDList.Add(Classes(i))
                            Next
                        End If

                        'Collect status from desired class objects.
                        Dim Status, OneStatus, StatusDoc As String
                        Status = Nothing : StatusDoc = Nothing
                        Dim ObjStat As ClassStatus
                        For i = 0 To _PerfMonitorList.Count - 1
                            ObjStat = Nothing
                            ObjStat = CType(_PerfMonitorList(i), ClassStatus)

                            If IsAll Then
                                OneStatus = Nothing
                                OneStatus = ObjStat.ToXMLString
                                If OneStatus <> "" Then
                                    Status = Status & OneStatus
                                End If
                            Else
                                If IDList.Contains(ObjStat.ObjectID) Then
                                    OneStatus = Nothing
                                    OneStatus = ObjStat.ToXMLString
                                    If OneStatus <> "" Then
                                        Status = Status & OneStatus
                                    End If
                                End If
                            End If
                        Next

                        If Not Status Is Nothing Then
                            StatusDoc = "<status>" & Status & "</status>"

                            'Construct SRP message and send out.
                            Dim Seq() As Byte
                            Seq = Message.GetFieldActualValue("Sequence")

                            Dim SRPMessage As Telegram
                            SRPMessage = ConstructSRPMessage(Seq, StatusDoc, m_Parameters.SRPMessageFormat)
                            If Not SRPMessage Is Nothing Then
                                If m_Logger.IsInfoEnabled Then
                                    m_Logger.Info("SRP Message is created and will be sent... (Msg Len:" & _
                                            SRPMessage.RawData.Length.ToString & "). <" & ThisMethod & ">")
                                End If

                                'SRP message shall be sent to the same channel from where the SRQ was received.
                                SRPMessage.ChannelName = Message.ChannelName
                                'Forward encoded SRP message to SortEngine via TCPServer channel.
                                SendToServerForwarder(SRPMessage)
                            Else
                                If m_Logger.IsErrorEnabled Then
                                    m_Logger.Error("SRP message constructing failure! <" & _
                                            ThisMethod & ">")
                                End If
                            End If
                        End If 'If Not Status Is Nothing Then

                        m_LastSRQReceivedTime = Now
                    End If 'If Math.Abs(TimeDiff.TotalMilliseconds) < MIN_SRQ_INTERVAL Then
                End If

            Catch ex As Exception
                If m_Logger.IsErrorEnabled Then
                    m_Logger.Error("Exception occurs! <" & _
                            ThisMethod & "> Exception: Source = " & ex.Source & _
                            " | Type : " & ex.GetType.ToString & " | Message : " & ex.Message)
                End If
            End Try
        End Sub

        '<telegram alias="SRP" name="Status_Reply_Message" sequence="False" acknowledge="False">
        '	<field name="Type" offset="0" length="4" default="49,48,48,50"/>
        '	<field name="Length" offset="4" length="4" default="?"/>
        '	<field name="Sequence" offset="8" length="4" default="?"/>
        '	<field name="Status" offset="12" length="?" default="?"/>
        '</telegram>		
        Private Function ConstructSRPMessage(ByRef Seq() As Byte, _
                    ByRef Status As String, _
                    ByRef SRPFormat As TelegramFormat) As Telegram
            Dim ThisMethod As String = m_ClassName & "." & _
                    System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            If Status Is Nothing Then
                Return Nothing
            End If

            If SRPFormat Is Nothing Then
                Return Nothing
            End If

            Try
                Dim Data() As Byte = Nothing
                Dim SRPMessage As New Telegram(Nothing, Data)
                'In order for App class asign the new sequence# to outgoing SOL
                'message, the TelegramFormat object has to be assigned to SOL message
                'at this layer class.
                SRPMessage.Format = SRPFormat

                Dim FieldLen, MsgLen As Integer
                '	<field name="Type" offset="0" length="4" default="49,48,48,50"/>
                FieldLen = SRPMessage.Format.Field("Type").Length
                MsgLen = MsgLen + FieldLen
                If Not SRPMessage.SetFieldActualValue("Type", _
                             SRPMessage.GetFieldDefaultValue("Type"), Common.PaddingRule.Right) Then
                    Throw New Exception("SRP message ""Type"" field value assignment failure!")
                End If

                '	<field name="Sequence" offset="8" length="4" default="?"/>
                FieldLen = SRPMessage.Format.Field("Sequence").Length
                MsgLen = MsgLen + FieldLen
                If Not SRPMessage.SetFieldActualValue("Sequence", Seq, Common.PaddingRule.Left) Then
                    Throw New Exception("SRP message ""Sequence"" field value assignment failure!")
                End If

                '	<field name="Status" offset="12" length="?" default="?"/>
                FieldLen = Status.Length
                MsgLen = MsgLen + FieldLen
                SRPMessage.Format.Field("Status").Length = FieldLen
                If Not SRPMessage.SetFieldActualValue("Status", _
                            System.Text.Encoding.Default.GetBytes(Status), _
                            Common.PaddingRule.Left) Then
                    Throw New Exception("SRP message ""BagEvent"" field value assignment failure!")
                End If

                '	<field name="Length" offset="4" length="4" default="?"/>
                FieldLen = SRPMessage.Format.Field("Length").Length
                MsgLen = MsgLen + FieldLen
                Dim Length(FieldLen - 1) As Byte
                Length = Functions.ConvertStringToFixLengthByteArray( _
                        MsgLen.ToString, FieldLen, Chr(&H30), Functions.PaddingRule.Left)
                If Not SRPMessage.SetFieldActualValue("Length", _
                            Length, Common.PaddingRule.Left) Then
                    Throw New Exception("SRP message ""Length"" field value assignment failure!")
                End If

                Return SRPMessage
            Catch ex As Exception
                If m_Logger.IsErrorEnabled Then
                    m_Logger.Error("Exception occurred. <" & ThisMethod & _
                            "> Exception: Source = " & ex.Source & " | Type : " & _
                            ex.GetType.ToString & " | Message : " & ex.Message)
                End If
                Return Nothing
            End Try
        End Function

        Public Sub SendToServerForwarder(ByVal Message As PALS.Telegrams.Telegram)
            m_NoOfMsgToSvrChannel = Functions.CounterIncrease(m_NoOfMsgToSvrChannel)

            m_ServerForwarder.Send(Message)
        End Sub

        Public Sub Disconnect()
            m_ServerForwarder.Disconnect()
        End Sub

        Private Sub OnReceived_ServerForwarder(ByVal ChannelName As String, ByRef Message As Telegram)
            m_NoOfMsgFromSvrChannel = Functions.CounterIncrease(m_NoOfMsgFromSvrChannel)

            Dim MsgSouc As New MessageAndSource
            Message.ChannelName = ChannelName
            MsgSouc.Source = SOURCE_SERVER_CHANNEL
            MsgSouc.Message = Message
            m_SyncedIncomingQueue.Enqueue(MsgSouc)
        End Sub

        '<object id="4">
        '	<class>DBT2_BHS.ServiceMonitor.Application.MessageHandler</class>
        '	<threadCounter>7353</threadCounter>
        '	<incomingQueue>0</incomingQueue>
        '	<msgsFromConsole>1</msgsFromConsole>
        '	<msgsToConsole>0</msgsToConsole>
        '	<msgsDiscarded>0</msgsDiscarded>
        '	<msgsSRQ>1</msgsSRQ>
        '</object>
        Private Sub PerfCounterRefresh()
            Dim ThisMethod As String = m_ClassName & "." & _
                       System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Try
                If (Not _PerfMonitor Is Nothing) And (Not m_ObjectID Is Nothing) Then
                    _PerfMonitor.OpenObjectNode()

                    _PerfMonitor.AddObjectStatus("class", m_ClassName)
                    _PerfMonitor.AddObjectStatus("threadCounter", m_ThreadCounter.ToString)
                    _PerfMonitor.AddObjectStatus("incomingQueue", m_SyncedIncomingQueue.Count.ToString)
                    _PerfMonitor.AddObjectStatus("msgsFromConsole", m_NoOfMsgFromSvrChannel.ToString)
                    _PerfMonitor.AddObjectStatus("msgsToConsole", m_NoOfMsgToSvrChannel.ToString)
                    _PerfMonitor.AddObjectStatus("msgsDiscarded", m_NoOfDiscardedMessage.ToString)
                    _PerfMonitor.AddObjectStatus("msgsSRQ", m_NoOfSRQ.ToString)
                    _PerfMonitor.AddObjectStatus("msgsSTR", m_NoOfSTR.ToString)
                    _PerfMonitor.AddObjectStatus("msgsSTO", m_NoOfSTO.ToString)

                    _PerfMonitor.CloseObjectNode()
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

        Public Event OnStatusRequested()

        Public Event OnServiceStartRequest(ByVal Services As String)

        Public Event OnServiceStopRequest(ByVal Services As String)

#End Region

    End Class

End Namespace
