'=====================================================================================
' Copyright 2009, Xu Jian, All Rights Reserved.
'=====================================================================================
'FileName       Common.vb
'Revision:      1.0 -   13 Mar 2009, By hs chia, For Winnipeg International Airport BHS project.
'=====================================================================================

Option Explicit On
Option Strict On

'Root Namespace: BHS

'<!--Machine and services that need to be monitored/controlled by this service-->
'<service serviceName="BHS_SAC2PLC2GW" processName="BHS_SAC2PLC2GW" machineName="SAC-COM1" serverIP="192.168.10.58" isControllable="True"></service>
'<service serviceName="BHS_SAC2PLC3GW" processName="BHS_SAC2PLC3GW" machineName="SAC-COM1" serverIP="192.168.10.58" isControllable="True"></service>
'<service serviceName="BHS_SAC2PLC4GW" processName="BHS_SAC2PLC4GW" machineName="SAC-COM1" serverIP="192.168.10.58" isControllable="True"></service>
'<service serviceName="BHS_SAC2PLC5GW" processName="BHS_SAC2PLC5GW" machineName="SAC-COM1" serverIP="192.168.10.58" isControllable="True"></service>
'<service serviceName="BHS_SortEngine" processName="BHS_SortEngine" machineName="SAC-COM1" serverIP="192.168.10.58" isControllable="True"></service>
'<service serviceName="BHS_SvcMonitor" processName="BHS_SvcMonitor" machineName="SAC-COM1" serverIP="192.168.10.58" isControllable="True"></service>
Public Structure ServiceInfo
    Dim ServiceName As String
    Dim ProcessName As String
    Dim MachineName As String
    Dim ServerIP As String
    Dim SvcController As System.ServiceProcess.ServiceController
    'To indicate whether the service can be controlled through BHS_SvcMonitor service application
    Dim IsControllable As Boolean
    'To indicate whether the specified service has been detected in the server.
    Dim IsExisted As Boolean
    Dim StopDetectedTime As Date
    'To indicate whether the service is pending for restarting (waiting for service restart timeout)
    Dim IsPending As Boolean
    'This service status (If service was not detected, this Status will be Unknown.
    'Value could be: Unknown, ContinuePending, Paused, PausePending, Running, StartPending, Stopped, StopPending 
    Dim Status As String
End Structure

Public Enum StartStopCommand
    cmdStart = 0
    cmdStop = 1
End Enum

Public Structure LocationID
    Dim SubSystem As String
    Dim Location As String
End Structure


Public Structure LocationCost
    Dim Location As LocationID
    Dim Cost As Integer
End Structure


Public Structure LastSortation
    Dim Destination As LocationID 'The last destination that particular flight bag was sorted to.
    Dim Time As Date 'The time of last sortation.
End Structure

Public Enum TagType
    IATA_Tag = 0
    Fallback_Tag = 1
    DummyEmptyLP = 2
    DummyMultipleLP = 3
End Enum

Public Structure Tag
    Dim LP As String
    Dim Valid As Boolean
    Dim Type As TagType
    Dim AirlineCode As String
    Dim AirportLocationCode As String
    Dim FallbackDischarge As String
End Structure

Public Structure RelatedNames
    Dim STD As String
    Dim ETD As String
    Dim ITD As String
    Dim ATD As String
End Structure

Public Enum BagStates
    Unknow = 0
    TooEarly = 1
    Early = 2
    Open = 3
    Rush = 4
    TooLate = 5
End Enum

Public Class Utilities

    Private Const IDENTIFIER_FALLBACK_TAG As String = "1"
    Private Const IDENTIFIER_IATA_TAG As String = "0"
    Private Const EMPTY_AIRPORT_LOCATION As String = "0000"

    'The name of current class 
    Private Shared ReadOnly mClassName As String = _
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString
    'Create a logger for use in this class
    Private Shared ReadOnly mLogger As log4net.ILog = _
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType)


    ''' <summary>
    ''' Convert LocationID structure object array to "Location1/Subsystem1, Location2/Subsystem2, ..."
    ''' format string for display purpose.
    ''' 
    ''' </summary>
    ''' <param name="Locations"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function LocationIDArrayToString(ByRef Locations() As LocationID) As String
        Dim i As Integer
        Dim Temp, Dest As String

        If Locations Is Nothing Then
            Return Nothing
        End If

        Dest = Nothing
        For i = 0 To Locations.Length - 1
            Temp = Locations(i).Location & "/" & Locations(i).SubSystem
            If Dest Is Nothing Then
                Dest = Temp
            Else
                Dest = Dest & ", " & Temp
            End If
        Next

        Return Dest
    End Function




    ''' <summary>
    ''' Decode a 10-digit IATA code into a Tag structure object:
    ''' Public Structure Tag
    '''     Dim LP As String
    '''     Dim Valid As Boolean
    '''     Dim Type As TagType
    '''     Dim Airline As String
    '''     Dim Location As String
    '''     Dim Destination As String
    ''' End Structure
    ''' 
    ''' Tag.Type will indicate this bag tag is Fallback Tag or Normal IATA License Plate Tag.
    ''' Tag.Valid will indicate whether the Fallback tag is valid or invalid (the airport location
    ''' code in the 10-digit code is not identical to the specific airport location code (given by 
    ''' the function argument: AirportLocationCode).
    ''' If it is IATA tag, then  the Tag.Valid field value will alwasy be True.
    ''' </summary>
    ''' <param name="LicensePlate"></param>
    ''' <param name="AirportLocationCode"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function BagTagDecoding(ByVal LicensePlate As String, _
                    ByVal AirportLocationCode As String) As Tag
        Dim ThisMethod As String = mClassName & "." & System.Reflection.MethodBase.GetCurrentMethod().Name & "()"
        Dim BagTag As Tag = Nothing

        BagTag.LP = LicensePlate
        BagTag.AirportLocationCode = EMPTY_AIRPORT_LOCATION
        BagTag.Valid = True

        Try
            'Get tag type (1 digit of LP#)
            Dim Type As String = Left(LicensePlate, 1)

            'get the Airline code (2-4 digits of LP#)
            BagTag.AirlineCode = Mid(LicensePlate, 2, 3)

            If Type = IDENTIFIER_FALLBACK_TAG Then
                BagTag.Type = TagType.Fallback_Tag
                'get the Airport location code (5-8 digits of LP#)
                BagTag.AirportLocationCode = Mid(LicensePlate, 5, 4)

                'get the Destination code (9-10 digits of LP#)
                BagTag.FallbackDischarge = Mid(LicensePlate, 9, 2)

                If BagTag.AirportLocationCode <> AirportLocationCode Then
                    BagTag.Valid = False
                End If
            ElseIf Type = IDENTIFIER_IATA_TAG Then
                BagTag.Type = TagType.IATA_Tag
            Else
                BagTag.Type = TagType.IATA_Tag
            End If

            Return BagTag
        Catch ex As Exception
            If mLogger.IsErrorEnabled Then
                mLogger.Error("Exception occurs! <" & _
                        ThisMethod & "> Exception: Source = " & ex.Source & _
                        " | Type : " & ex.GetType.ToString & " | Message : " & ex.Message)
            End If

            BagTag.LP = LicensePlate
            BagTag.Type = TagType.IATA_Tag
            BagTag.AirportLocationCode = EMPTY_AIRPORT_LOCATION
            BagTag.Valid = True
            Return BagTag
        End Try
    End Function


    Public Shared Function ConvertStringToDate(ByVal YYYY As String, ByVal MM As String, ByVal DD As String) As Date
        Dim MTH As Integer = CInt(MM)
        Dim DAY As Integer = CInt(DD)

        If MTH > 12 Or MTH < 1 Then
            Throw New Exception("Invalid month value (" & MM & ")! It has to be (>=1) and (<=12).")
        End If

        If DAY > 31 Or DAY < 1 Then
            Throw New Exception("Invalid day value (" & DD & ")! It has to be (>=1) and (<=31).")
        End If

        Dim DT As String = Nothing
        Select Case MTH
            Case 1
                DT = YYYY & "-JAN-" & DAY.ToString
            Case 2
                DT = YYYY & "-FEB-" & DAY.ToString
            Case 3
                DT = YYYY & "-MAR-" & DAY.ToString
            Case 4
                DT = YYYY & "-APR-" & DAY.ToString
            Case 5
                DT = YYYY & "-MAY-" & DAY.ToString
            Case 6
                DT = YYYY & "-JUN-" & DAY.ToString
            Case 7
                DT = YYYY & "-JUL-" & DAY.ToString
            Case 8
                DT = YYYY & "-AUG-" & DAY.ToString
            Case 9
                DT = YYYY & "-SEP-" & DAY.ToString
            Case 10
                DT = YYYY & "-OCT-" & DAY.ToString
            Case 11
                DT = YYYY & "-NOV-" & DAY.ToString
            Case 12
                DT = YYYY & "-DEC-" & DAY.ToString
        End Select

        Return CDate(DT)
    End Function




End Class




