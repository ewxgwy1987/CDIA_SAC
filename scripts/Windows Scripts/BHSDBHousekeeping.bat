@ECHO OFF
REM #############################################################################
REM # Batch File: BHSDBHousekeeping.bat                                         #
REM # Release#: 1.0                                                             #
REM # Release Date: 10 Nov 2007                                                 #
REM # Author: Xu Jian                                                           #
REM # Description:                                                              #
REM # 	This batch will perform the BHS solution database housekeeping task     #
REM #	by invoking the DB StoredProcedure [stp_DBHousekeeping].                #
REM #	                                                                        #
REM #   There are two integer parameters need to be passed to StoredProcedure:  #
REM #   @LifeTime_WT -	the # of days that the records can be kept in the       #
REM #   		Working data tables;                                    #
REM #   		valid range (14days ~ 30days). If the given value < 14, #
REM #   		then 14 will be used. If the given value >30, then 30   #
REM #   		will be used;                                           #
REM #   @LifeTime_HT -	the # of days that the records can be kept in the       #
REM #   		Historical data tables;		                        #
REM #   		valid range (30days ~ 365days). If the given value < 30,#
REM #   		then 30 will be used. If the given value >365, then 30  #
REM #   		365 will be used;                                       #
REM #############################################################################

SQLCMD -S DBSQL -U sacdbuser -P sac@interr0l1er -v LifeTime_WT=30 LifeTime_HT=90 -Q "USE BHSDB;EXEC [dbo].[stp_DBHousekeeping] $(LifeTime_WT),$(LifeTime_HT);" -o %IR_BHS_LOG%\DBHousekeep.log
