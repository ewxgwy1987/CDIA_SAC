'=====================================================================================
' Copyright 2009, Xu Jian, All Rights Reserved.
'=====================================================================================
'FileName       GlobalAppInfo.vb
'Revision:      1.0 -   13 Mar 2009, By hs chia, For Winnipeg International Airport BHS project.
'=====================================================================================

Option Explicit On
Option Strict On

Namespace ServiceMonitor.Application

    Public NotInheritable Class GlobalAppInfo

#Region "1. Class Fields Declaration."

        Private Shared m_AppName As String = "UnKnown"
        Private Shared m_AppStartedTime As Date
        Private Shared m_Company As String = "PALS"
        Private Shared m_Department As String = "CSI"
        Private Shared m_Author As String = "XuJian"

#End Region

#Region "2. Class Constructor and Destructor Declaration."

        Public Sub New()
        End Sub

#End Region

#Region "3. Class Properties Defination."

        Public Shared Property AppName() As String
            Get
                Return m_AppName
            End Get
            Set(ByVal Value As String)
                m_AppName = Value
            End Set
        End Property

        Public Shared Property AppStartedTime() As Date
            Get
                Return m_AppStartedTime
            End Get
            Set(ByVal Value As Date)
                m_AppStartedTime = Value
            End Set
        End Property

        Public Shared Property Company() As String
            Get
                Return m_Company
            End Get
            Set(ByVal Value As String)
                m_Company = Value
            End Set
        End Property

        Public Shared Property Department() As String
            Get
                Return m_Department
            End Get
            Set(ByVal Value As String)
                m_Department = Value
            End Set
        End Property

        Public Shared Property Author() As String
            Get
                Return m_Author
            End Get
            Set(ByVal Value As String)
                m_Author = Value
            End Set
        End Property

#End Region

    End Class

End Namespace

