'=====================================================================================
' Copyright 2009, Xu Jian, All Rights Reserved.
'=====================================================================================
'FileName       AppInitializer.vb
'Revision:      1.0 -   13 Mar 2009, By hs chia, For Winnipeg International Airport BHS project.
'=====================================================================================


Option Explicit On
Option Strict On

Namespace ServiceMonitor.Configure

    Public Class GlobalContext
#Region "1. Class Fields Declaration."
        Private _company As String
        Private _department As String
        Private _author As String
        Private _appName As String
        Private _appStartedTime As Date

#End Region

#Region "2. Class Constructor and Destructor Declaration."

#End Region

#Region "3. Class Properties Defination."
        '''  <summary>
        '''  Company name
        '''  </summary>
        Public Property Company() As String
            Get
                Return _company
            End Get
            Set(ByVal Value As String)
                _company = Value
            End Set
        End Property

        '''  <summary>
        '''  Department name
        '''  </summary>
        Public Property Department() As String
            Get
                Return _department
            End Get
            Set(ByVal Value As String)
                _department = Value
            End Set
        End Property

        '''  <summary>
        '''  Author name
        '''  </summary>
        Public Property Author() As String
            Get
                Return _author
            End Get
            Set(ByVal Value As String)
                _author = Value
            End Set
        End Property

        '''  <summary>
        '''  AppName name
        '''  </summary>
        Public Property AppName() As String
            Get
                Return _appName
            End Get
            Set(ByVal Value As String)
                _appName = Value
            End Set
        End Property

        '''  <summary>
        '''  AppName name
        '''  </summary>
        Public Property AppStartedTime() As Date
            Get
                Return _appStartedTime
            End Get
            Set(ByVal Value As Date)
                _appStartedTime = Value
            End Set
        End Property


#End Region

#Region "5. Class Method Declaration."

#End Region


    End Class
End Namespace

