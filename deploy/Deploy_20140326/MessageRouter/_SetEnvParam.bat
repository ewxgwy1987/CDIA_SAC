
REM #######################################################################
REM # SetX has three ways of working: 
REM # Syntax 1:
REM #    SETX [/S system [/U [domain\]user [/P [password]]]] var value [/M]
REM # Syntax 2:
REM #    SETX [/S system [/U [domain\]user [/P [password]]]] var /K regpath [/M]
REM # Syntax 3:
REM #    SETX [/S system [/U [domain\]user [/P [password]]]] /F file {var {/A x,y | /R x,y string}[/M] | /X} [/D delimiters]
REM #
REM #
REM #    /S     system      	Specifies the remote system to connect to.
REM #    /U     [domain\]user  	Specifies the user context under which the 
REM #                           command should execute.
REM #    /K     regpath         Specifies that the variable is set based 
REM #                           on information from a registry key.
REM #                           Path should be specified in the format of 
REM #                           hive\key\...\value. 
REM #                           For example:                           
REM #                           HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\TimeZoneInformation\StandardName.
REM #    /P     [password]      Specifies the password for the given user 
REM #                           context. Prompts for input if omitted.
REM #    var                    Specifies the environment variable to set.
REM #    value                 	Specifies a value to be assigned to the 
REM #                           environment variable.
REM #    /M                    	Specifies that the variable should be set 
REM #                           in the system wide (HKEY_LOCAL_MACHINE) 
REM #                           environment. The default is to set the 
REM #                           variable under the HKEY_CURRENT_USER 
REM #                           environment.
REM #######################################################################

REM #######################################################################
REM # Note: In order to set the variables as the system environment, 
REM #       instead of user environment variable, the ?M?parameter must 
REM #       be used with the SetX.exe utility.
REM #######################################################################

REM #######################################################################
REM # Note: BHS SAC Application Folder Structure. 
REM #       C:\PterisGlobal\PALS
REM #       D:\PterisGlobal\PALS\Log
REM #######################################################################

SETX PALS_BASE "C:\PterisGlobal\PALS" /M
SETX PALS_LOG "D:\PterisGlobal\PALS\Log" /M