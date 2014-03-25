'==========================================================================
' NAME: 	SACLogFileHousekeeping.vbs
' AUTHOR: 	Xu Jian
' DATE : 	27 Jan 2005
' COMMENT: 	Delete all files that are older than 30 days from the specified directory
'==========================================================================

dim Files, File, Filesystem 
dim LogFileFolder, RootFolder
dim DaysOld, DateMod, DeadLine, DateToday
dim Count 

'30 days older log files will be deleted.
DaysOld = 30

Set WshShell = WScript.CreateObject("WScript.Shell")
Set WshSysEnv = WshShell.Environment("SYSTEM")
LogFileFolder = WshSysEnv("IR_BHS_LOG")
'0 - SUCCESS; 1 - ERROR; 2 - WARNING; 4 - INFORMATION; 
'8 - AUDIT_SUCCESS; 16 - AUDIT_FAILURE
WshShell.LogEvent 4, "SAC log file housekeeping process is starting..." & _
				vbcrlf & "Log files folder:" & LogFileFolder & "." & _
				vbcrlf & "Log files that are older than " & DaysOld & _
				" days will be permanentely removed."

' Instantiate the filesystemobject
set Filesystem = createobject("Scripting.FileSystemObject")
On Error Resume Next
set RootFolder = Filesystem.GetFolder(LogFileFolder)
if Err.Number <> 0 then
	WshShell.LogEvent 1, "(" & Err.Number & ") " & Err.Description & vbcrlf & _
				"Log file path is invalid (" & LogFileFolder & ")."
	wscript.Quit 0
end if

' print todays date and deadline
DateToday = date
DeadLine = DateToday - DaysOld

'Find all files in the root folder
set Files = RootFolder.Files

' For each file in folder
Count = 0
for each File in Files
	' Determine Date modified
	DateMod = File.datelastmodified

	'Show us what you are going todo!
	if DateMod < DeadLine then
		File.delete
		Count = Count + 1
	end If
next

WshShell.LogEvent 4, "SAC log file housekeeping process is completed." & vbcrlf & _
			"Total " & Count & " files has been removed from folder " & LogFileFolder & "."
'==========================================================================
