'=====================================================================================
' Copyright 2009, Xu Jian, All Rights Reserved.
'=====================================================================================
'FileName       MessageHandlerParameters.vb
'Revision:      1.0 -   13 Mar 2009, By hs chia, For Winnipeg International Airport BHS project.
'=====================================================================================

Option Explicit On
Option Strict On

Imports System.Xml
Imports PALS.Telegrams
Imports PALS.Utilities

Namespace ServiceMonitor.Application 'Root Namespace: BHS

    Public Class MessageHandlerParameters
        Implements PALS.Common.IParameters, IDisposable

#Region "1. Class Fields Declaration."

        Private Const APP_TELEGRAMSET As String = "Application_Telegrams"
        Private Const SRQ_MESSAGE_ALIAS As String = "SRQ"
        Private Const SRP_MESSAGE_ALIAS As String = "SRP"
        Private Const STR_MESSAGE_ALIAS As String = "STR"
        Private Const STO_MESSAGE_ALIAS As String = "STO"

        'The name of current class 
        Private Shared ReadOnly m_ClassName As String = _
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString
        'Create a logger for use in this class
        Private Shared ReadOnly m_Logger As log4net.ILog = _
                log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType)

        Private m_AppHeaderFormat As TelegramFormat
        Private m_SRQMessageType As String
        Private m_SRQMessageFormat As TelegramFormat
        Private m_SRPMessageType As String
        Private m_SRPMessageFormat As TelegramFormat
        Private m_STRMessageType As String
        Private m_STRMessageFormat As TelegramFormat
        Private m_STOMessageType As String
        Private m_STOMessageFormat As TelegramFormat
#End Region

#Region "2. Class Constructor and Destructor Declaration."
        Public Sub New(ByRef ConfigSet As XmlNode, ByRef TelegramSet As XmlNode)
            MyBase.new()

            If TelegramSet Is Nothing Then
                Throw New Exception("There is no [" & TelegramSet.Name & _
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
            m_AppHeaderFormat = Nothing
            m_SRQMessageFormat = Nothing
            m_SRPMessageFormat = Nothing
            m_STRMessageFormat = Nothing
            m_STOMessageFormat = Nothing
        End Sub 'Dispose
#End Region

#Region "3. Class Properties Defination."

        Public ReadOnly Property AppHeaderFormat() As TelegramFormat
            Get
                Return m_AppHeaderFormat
            End Get
        End Property

        Public ReadOnly Property SRQMessageType() As String
            Get
                Return m_SRQMessageType
            End Get
        End Property

        Public ReadOnly Property SRQMessageFormat() As TelegramFormat
            Get
                Return m_SRQMessageFormat
            End Get
        End Property

        Public ReadOnly Property SRPMessageType() As String
            Get
                Return m_SRPMessageType
            End Get
        End Property

        Public ReadOnly Property SRPMessageFormat() As TelegramFormat
            Get
                Return m_SRPMessageFormat
            End Get
        End Property

        Public ReadOnly Property STRMessageType() As String
            Get
                Return m_STRMessageType
            End Get
        End Property

        Public ReadOnly Property STRMessageFormat() As TelegramFormat
            Get
                Return m_STRMessageFormat
            End Get
        End Property

        Public ReadOnly Property STOMessageType() As String
            Get
                Return m_STOMessageType
            End Get
        End Property

        Public ReadOnly Property STOMessageFormat() As TelegramFormat
            Get
                Return m_STOMessageFormat
            End Get
        End Property

#End Region

#Region "5. Class Method Declaration."

        Private Function Init(ByRef ConfigSet As XmlNode, _
                        ByRef TelegramSet As XmlNode) As Boolean _
                        Implements PALS.Common.IParameters.Init
            Dim ThisMethod As String = m_ClassName & "." & _
              System.Reflection.MethodBase.GetCurrentMethod().Name & "()"
            Dim TelegramSetName As String

            Try
                TelegramSetName = XMLConfig.GetSettingFromAttribute(TelegramSet, "name", "Unknown")

                '<telegramSet name="APP_Telegrams">
                '       <telegram alias="SRQ" name="Status_Request_Message" sequence="True" acknowledge="False">
                '	        <field name="Type" offset="0" length="4" default="49,48,48,49"/>
                '	        <field name="Length" offset="4" length="4" default="?"/>
                '	        <field name="Sequence" offset="8" length="4" default="?"/>
                '	        <field name="Class" offset="12" length="?" default="?"/>
                '       </telegram>		
                '       <telegram alias="SRP" name="Status_Reply_Message" sequence="False" acknowledge="False">
                '       	<field name="Type" offset="0" length="4" default="49,48,48,50"/>
                '       	<field name="Length" offset="4" length="4" default="?"/>
                '       	<field name="Sequence" offset="8" length="4" default="?"/>
                '       	<field name="Status" offset="12" length="?" default="?"/>
                '       </telegram>		
                '</telegramSet>	
                Dim FormatConfigSet As XmlNode
                FormatConfigSet = XMLConfig.GetConfigSetElement( _
                       TelegramSet, "telegramSet", "name", APP_TELEGRAMSET)
                If FormatConfigSet Is Nothing Then
                    If m_Logger.IsErrorEnabled Then
                        m_Logger.Error("The [telegramFormat] node is missing in the config set [" & _
                                TelegramSet.Name & "]! <" & ThisMethod & ">")
                    End If
                    Return False
                End If
                If Not GetTelegramFormat(FormatConfigSet) Then
                    Return False
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

        Private Function GetTelegramFormat(ByRef ConfigSet As XmlNode) As Boolean
            Dim ThisMethod As String = m_ClassName & "." & _
                  System.Reflection.MethodBase.GetCurrentMethod().Name & "()"

            Try
                m_SRQMessageType = Nothing
                m_SRQMessageFormat = Nothing
                m_SRPMessageType = Nothing
                m_SRPMessageFormat = Nothing

                '<header alias="Header" name="App_Header" sequence="False" acknowledge="False">
                '	<field name="Type" offset="0" length="4" value=""/>
                '	<field name="Length" offset="4" length="4" value=""/>
                '	<field name="Sequence" offset="8" length="4" value=""/>
                '</header>
                Dim HeaderConfigSet As XmlNode
                HeaderConfigSet = XMLConfig.GetConfigSetElement(ConfigSet, "header")
                If HeaderConfigSet Is Nothing Then
                    If m_Logger.IsErrorEnabled Then
                        m_Logger.Error("The [header] node is missing in the config set [" & _
                                ConfigSet.Name & "]! <" & ThisMethod & ">")
                    End If
                    Return False
                End If
                m_AppHeaderFormat = New TelegramFormat(HeaderConfigSet)

                '       <telegram alias="SRQ" name="Status_Request_Message" sequence="True" acknowledge="False">
                '	        <field name="Type" offset="0" length="4" default="49,48,48,49"/>
                '	        <field name="Length" offset="4" length="4" default="?"/>
                '	        <field name="Sequence" offset="8" length="4" default="?"/>
                '	        <field name="Class" offset="12" length="?" default="?"/>
                '       </telegram>		
                Dim SRQConfigSet As XmlNode
                SRQConfigSet = XMLConfig.GetConfigSetElement( _
                            ConfigSet, "telegram", "alias", SRQ_MESSAGE_ALIAS)
                If SRQConfigSet Is Nothing Then
                    If m_Logger.IsErrorEnabled Then
                        m_Logger.Error("The format of " & SRQ_MESSAGE_ALIAS & _
                                " message is missing in the config set [" & _
                                ConfigSet.Name & "]! <" & ThisMethod & ">")
                    End If
                    Return False
                End If
                m_SRQMessageFormat = New TelegramFormat(SRQConfigSet)
                m_SRQMessageType = Functions.ConvertByteArrayToString( _
                            m_SRQMessageFormat.Field("Type").DefaultValue, , _
                            HexToStrMode.ToAscString)

                '       <telegram alias="SRP" name="Status_Reply_Message" sequence="False" acknowledge="False">
                '       	<field name="Type" offset="0" length="4" default="49,48,48,50"/>
                '       	<field name="Length" offset="4" length="4" default="?"/>
                '       	<field name="Sequence" offset="8" length="4" default="?"/>
                '       	<field name="Status" offset="12" length="?" default="?"/>
                '       </telegram>		
                Dim SRPConfigSet As XmlNode
                SRPConfigSet = XMLConfig.GetConfigSetElement( _
                            ConfigSet, "telegram", "alias", SRP_MESSAGE_ALIAS)
                If SRPConfigSet Is Nothing Then
                    If m_Logger.IsErrorEnabled Then
                        m_Logger.Error("The format of " & SRP_MESSAGE_ALIAS & _
                                " message is missing in the config set [" & _
                                ConfigSet.Name & "]! <" & ThisMethod & ">")
                    End If
                    Return False
                End If
                m_SRPMessageFormat = New TelegramFormat(SRPConfigSet)
                m_SRPMessageType = Functions.ConvertByteArrayToString( _
                            m_SRPMessageFormat.Field("Type").DefaultValue, , _
                            HexToStrMode.ToAscString)

                '<telegram alias="STR" name="Service_Start_Command_Message" sequence="True" acknowledge="False">
                '	<field name="Type" offset="0" length="4" default="48,49,48,52"/>
                '	<field name="Length" offset="4" length="4" default="?"/>
                '	<field name="Sequence" offset="8" length="4" default="?"/>
                '	<field name="Services" offset="12" length="?" default="?"/>
                '</telegram>		
                Dim STRConfigSet As XmlNode
                STRConfigSet = XMLConfig.GetConfigSetElement( _
                            ConfigSet, "telegram", "alias", STR_MESSAGE_ALIAS)
                If STRConfigSet Is Nothing Then
                    If m_Logger.IsErrorEnabled Then
                        m_Logger.Error("The format of " & STR_MESSAGE_ALIAS & _
                                " message is missing in the config set [" & _
                                ConfigSet.Name & "]! <" & ThisMethod & ">")
                    End If
                    Return False
                End If
                m_STRMessageFormat = New TelegramFormat(STRConfigSet)
                m_STRMessageType = Functions.ConvertByteArrayToString( _
                            m_STRMessageFormat.Field("Type").DefaultValue, , _
                            HexToStrMode.ToAscString)

                '<telegram alias="STO" name="Service_Stop_Command_Message" sequence="True" acknowledge="False">
                '	<field name="Type" offset="0" length="4" default="48,49,48,53"/>
                '	<field name="Length" offset="4" length="4" default="?"/>
                '	<field name="Sequence" offset="8" length="4" default="?"/>
                '	<field name="Services" offset="12" length="?" default="?"/>
                '</telegram>	
                Dim STOConfigSet As XmlNode
                STOConfigSet = XMLConfig.GetConfigSetElement( _
                            ConfigSet, "telegram", "alias", STO_MESSAGE_ALIAS)
                If STOConfigSet Is Nothing Then
                    If m_Logger.IsErrorEnabled Then
                        m_Logger.Error("The format of " & STO_MESSAGE_ALIAS & _
                                " message is missing in the config set [" & _
                                ConfigSet.Name & "]! <" & ThisMethod & ">")
                    End If
                    Return False
                End If
                m_STOMessageFormat = New TelegramFormat(STOConfigSet)
                m_STOMessageType = Functions.ConvertByteArrayToString( _
                            m_STOMessageFormat.Field("Type").DefaultValue, , _
                            HexToStrMode.ToAscString)

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

#End Region

    End Class

End Namespace

