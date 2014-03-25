'=====================================================================================
' Copyright 2009, Xu Jian, All Rights Reserved.
'=====================================================================================
'FileName       ServerMsgForwarder.vb
'Revision:      1.0 -   13 Mar 2009, By hs chia, For Winnipeg International Airport BHS project.
'=====================================================================================

Option Explicit On
Option Strict On

Imports PALS.Common
Imports PALS.Net.Common
Imports PALS.Net.Handlers
Imports PALS.Telegrams
Imports PALS.Utilities

Namespace ServiceMonitor.Net.Handlers 'Root Namespace: BHS

    ''' -----------------------------------------------------------------------------
    ''' Project	 : DBT2_BHS
    ''' Class	 : ServiceMonitor.Net.Handlers.ServerMsgForwarder
    ''' 
    ''' -----------------------------------------------------------------------------
    ''' <summary>
    ''' Intermedia class between its top layer business data handler class 
    ''' (DBT2_BHS.SortEngine.Application.MessageHandler) and its bottom layer network
    ''' communication protocol chain classes. This class itself is the most top
    ''' class in the protocol chain classes. It receives the incoming message from 
    ''' its bottom chain class by its MessageReceived() method. And then forwards the 
    ''' received incoming message to MessageHandler class by event firing 
    ''' (OnReceived event). 
    ''' </summary>
    ''' <remarks>
    ''' </remarks>
    ''' <history>
    ''' 	[xujian]	12/19/2005	Created
    ''' </history>
    ''' -----------------------------------------------------------------------------
    Public Class ServerMsgForwarder
        Inherits SessionHandler
        Implements IDisposable

#Region "1. Class Fields Declaration."

        'The name of current class 
        Private Shared ReadOnly m_ClassName As String = _
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString
        'Create a logger for use in this class
        Private Shared ReadOnly m_Logger As log4net.ILog = _
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType)

        'Upon ConnectionOpened() method is invoked by bottom chain class,
        'the ChannelName will be stored into this ArrayList.
        'Once the bottom protocol layer connection is closed, its ChannelName
        'will be removed from this ArrayList accordingly.
        Private m_ConnectionList As ArrayList
        Private m_SycedConnectionList As ArrayList

#End Region

#Region "2. Class Constructor and Destructor Declaration."

        Public Sub New()
            MyBase.New()

            If Not Init(Nothing) Then
                Throw New Exception("Unable to initialize object, class " & _
                        m_ClassName & "instantiation failure!")
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Try
                If m_Logger.IsInfoEnabled Then
                    m_Logger.Info("Class:[" & m_ClassName & "] object is going to be destroyed... <" & _
                            m_ClassName & ".Dispose()>")
                End If

                If Not m_SycedConnectionList Is Nothing Then
                    SyncLock m_SycedConnectionList.SyncRoot
                        m_SycedConnectionList.Clear()
                    End SyncLock

                    m_SycedConnectionList = Nothing
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

#Region "4. Class Overrides Method Declaration."

        Protected Overrides Function Init(ByRef Param As IParameters) As Boolean
            Dim ThisMethod As String = m_ClassName & "." & _
                    System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Try
                If m_Logger.IsInfoEnabled Then
                    m_Logger.Info("Class:[" & m_ClassName & "] object is initializing... <" & _
                            ThisMethod & ">")
                End If

                '##################################################
                'Any initialization tasks can be add in here.

                m_ConnectionList = New ArrayList
                m_SycedConnectionList = ArrayList.Synchronized(m_ConnectionList)
                m_SycedConnectionList.Clear()

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

        Public Overrides Sub Disconnect(Optional ByVal ChannelName As String = Nothing)
            Dim ThisMethod As String = m_ClassName & "." & _
             System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Try
                '1. Close application layer connection first
                If Trim(ChannelName) = "" Then
                    'If no Channel name is given, then close all channel connections.
                    Dim Channel As String
                    SyncLock m_SycedConnectionList
                        While (m_SycedConnectionList.Count > 0)
                            Channel = Nothing
                            Channel = CType(m_SycedConnectionList.Item(0), String)
                            m_SycedConnectionList.RemoveAt(0)

                            If m_HasPrevChain Then
                                CType(m_PrevChain, AbstractProtocolChain).ConnectionClosed(Channel)
                            End If
                        End While
                    End SyncLock
                Else
                    SyncLock m_SycedConnectionList
                        If m_SycedConnectionList.Contains(ChannelName) Then
                            m_SycedConnectionList.Remove(ChannelName)
                            If m_HasPrevChain Then
                                CType(m_PrevChain, AbstractProtocolChain).ConnectionClosed(ChannelName)
                            End If
                        End If
                    End SyncLock
                End If

                '2. Invoke next chain class Disconnect() method to close the channel 
                'connection of next chain layer.
                If m_HasNextChain Then
                    CType(m_NextChain, AbstractProtocolChain).Disconnect(ChannelName)
                End If
            Catch ex As Exception
                If m_Logger.IsErrorEnabled Then
                    m_Logger.Error("Class:[" & m_ClassName & _
                            "] object Disconnect() process failure! <" & _
                            ThisMethod & "> Exception: Source = " & _
                            ex.Source & " | Type : " & ex.GetType.ToString & _
                            " | Message : " & ex.Message)
                End If
            End Try
        End Sub

        Public Overrides Sub ConnectionOpened(ByVal ChannelName As String)
            Dim ThisMethod As String = m_ClassName & "." & _
                        System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Try
                SyncLock m_SycedConnectionList
                    If Not m_SycedConnectionList.Contains(ChannelName) Then
                        m_SycedConnectionList.Add(ChannelName)
                    End If
                End SyncLock

                If m_HasPrevChain Then
                    CType(m_PrevChain, AbstractProtocolChain).ConnectionOpened(ChannelName)
                End If
            Catch ex As Exception
                If m_Logger.IsErrorEnabled Then
                    m_Logger.Error("Class:[" & m_ClassName & _
                            "] Exception occured! <" & _
                            ThisMethod & "> Exception: Source = " & _
                            ex.Source & " | Type : " & ex.GetType.ToString & _
                            " | Message : " & ex.Message)
                End If
            End Try
        End Sub

        Public Overrides Sub ConnectionClosed(ByVal ChannelName As String)
            Dim ThisMethod As String = m_ClassName & "." & _
                       System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Try
                SyncLock m_SycedConnectionList
                    If m_SycedConnectionList.Contains(ChannelName) Then
                        m_SycedConnectionList.Remove(ChannelName)

                        If m_HasPrevChain Then
                            CType(m_PrevChain, AbstractProtocolChain).ConnectionClosed(ChannelName)
                        End If
                    End If
                End SyncLock
            Catch ex As Exception
                If m_Logger.IsErrorEnabled Then
                    m_Logger.Error("Class:[" & m_ClassName & _
                            "] Exception occured! <" & _
                            ThisMethod & "> Exception: Source = " & _
                            ex.Source & " | Type : " & ex.GetType.ToString & _
                            " | Message : " & ex.Message)
                End If
            End Try
        End Sub

        Public Overrides Sub MessageReceived(ByVal ChannelName As String, _
                 ByRef Message As Telegram)
            Dim ThisMethod As String = m_ClassName & "." & _
                       System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Try
                RaiseEvent OnReceived(ChannelName, Message)
            Catch ex As Exception
                If m_Logger.IsErrorEnabled Then
                    m_Logger.Error("Class:[" & m_ClassName & _
                            "] Exception occured! <" & _
                            ThisMethod & "> Exception: Source = " & _
                            ex.Source & " | Type : " & ex.GetType.ToString & _
                            " | Message : " & ex.Message)
                End If
            End Try
        End Sub

#End Region

#Region "5. Class Method Declaration."

        ''' <para>Outgoing message which is passed to MessageCollector.Send() method will
        ''' be forwarded down to the communication channel that was specified in the 
        ''' ChannelName property of the message. 
        ''' 
        ''' If there is not any ChannelName was given to this property, then the message
        ''' will be sent to all opened channels whose ChannelName was in the class
        ''' internal Opened channel list.</para>
        Public Overloads Function Send(ByRef Message As Telegram) As Boolean
            Dim ThisMethod As String = m_ClassName & "." & _
                   System.Reflection.MethodBase.GetCurrentMethod().Name & "()"
            Dim i, Count As Integer
            Dim ChannelName As String

            Try
                Count = m_SycedConnectionList.Count
                If Count = 0 Then
                    If m_Logger.IsErrorEnabled Then
                        m_Logger.Error("No connection to remote client was opened! " & _
                                "Message will be discarded. [Msg(APP):" & _
                                Message.ToString(HexToStrMode.ToAscPaddedHexString) & _
                                "]. <" & ThisMethod & ">")
                    End If

                    Return False
                End If

                ChannelName = Trim(Message.ChannelName)
                If ChannelName <> "" Then
                    If m_SycedConnectionList.Contains(ChannelName) Then
                        'If the Channel was given and it is opened, then send message to it.
                        If m_Logger.IsDebugEnabled Then
                            m_Logger.Debug("Message will be forwarded to channel " & _
                                    ChannelName & "... [Msg(APP):" & _
                                    Message.ToString(HexToStrMode.ToAscPaddedHexString) & _
                                    "]. <" & ThisMethod & ">")
                        End If

                        Me.Send(ChannelName, Message)
                    Else
                        'If the Channel was given but is not opened, then discarded message.
                        If m_Logger.IsErrorEnabled Then
                            m_Logger.Error("Channel: " & ChannelName & " was not opened! " & _
                                    "Message will be discarded. [Msg(APP):" & _
                                    Message.ToString(HexToStrMode.ToAscPaddedHexString) & _
                                    "]. <" & ThisMethod & ">")
                        End If

                        Return False
                    End If
                Else
                    For i = 0 To Count - 1
                        ChannelName = Nothing
                        ChannelName = CType(m_SycedConnectionList.Item(i), String)

                        Message.ChannelName = ChannelName
                        If m_Logger.IsDebugEnabled Then
                            m_Logger.Debug("Message will be forwarded to channel " & _
                                    ChannelName & "... [Msg(APP):" & _
                                    Message.ToString(HexToStrMode.ToAscPaddedHexString) & _
                                    "]. <" & ThisMethod & ">")
                        End If

                        Me.Send(ChannelName, Message)
                    Next
                End If

                Return True
            Catch ex As Exception
                If m_Logger.IsErrorEnabled Then
                    m_Logger.Error("Class:[" & m_ClassName & _
                            "] Exception occured! <" & _
                            ThisMethod & "> Exception: Source = " & _
                            ex.Source & " | Type : " & ex.GetType.ToString & _
                            " | Message : " & ex.Message)
                End If
                Return False
            End Try
        End Function

#End Region

#Region "6. Class Events Defination."

        Public Event OnReceived(ByVal ChannelName As String, ByRef Message As Telegram)

#End Region

    End Class

End Namespace
