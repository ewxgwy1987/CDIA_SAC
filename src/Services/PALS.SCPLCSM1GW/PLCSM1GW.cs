
using System;
using System.ServiceProcess;
using System.Diagnostics;

namespace PALS.PLCSM1GW
{
    public partial class PLCSM1GW : ServiceBase
    {
       private const string SERVICE_NAME = "PALS.PLCSM1GW";

        // The name of current class 
        private static readonly string _className =
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();

        private BHS.Gateway.TCPClientTCPClientChains.Application.Initializer _init;

        public PLCSM1GW()
        {
            InitializeComponent();

            if (!System.Diagnostics.EventLog.SourceExists("PALS.PLCSM1GW"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "PALS.PLCSM1GW", "Application");
            }
            eventLog1.Source = "PALS.PLCSM1GW";
            eventLog1.Log = "Application";
        }

        protected override void OnStart(string[] args)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            try
            {
                // In the case of Windows Eventlog has been full, WriteEntry function will has 
                // exeception. But the service should still be started for BHS production. Hence, 
                // WriteEntry function is located in the Try...Catch...End Try block.
                eventLog1.WriteEntry(SERVICE_NAME + " service is starting...", EventLogEntryType.Information);
            }
            catch
            {
            }

            try
            {
                // ================================================================================
                // PALS Application Standard Installation Directories:
                // C:\PterisGlobal\PALS\
                //                  |_ \SAC             - SAC Gateway and Engine service applications;
                //                  |_ \MDS2CCTV        - MDS2VCCTV Gateway and Engine service applications;
                //                  |_ \MessageRouter   - Message Router service applications;
                //                  |_ \ReportViewer    - Report Viewer Windows form applications;
                //
                //
                // Windows system environment parameters required by PALS applications:
                // PALS_BASE    - Base path of PALS applications
                // PALS_LOG     - PALS application log file directory
                // ================================================================================

                string xmlSettingFile = PALS.Utilities.Functions.GetXMLFileFullName("PALS_BASE", "CFG_PLCSM1GW.xml", 5);
                if (xmlSettingFile == null)
                {
                    // Read XML configuration file from \MessageRouter sub folder.
                    xmlSettingFile = PALS.Utilities.Functions.GetXMLFileFullName("PALS_BASE", @"SAC\CFG_PLCSM1GW.xml", 5);

                    if (xmlSettingFile == null)
                        throw new Exception("XML configuration file (CFG_PLCSM1GW.xml) could not be found! " +
                                    "Please verify whether the Windows environment parameter (PALS_BASE) has been defined, " +
                                    "or file CFG_PLCSM1GW.xml is existing in the folder {PALS_BASE}\\SAC\\");
                }

                string xmlTelegramFile = PALS.Utilities.Functions.GetXMLFileFullName("PALS_BASE", "CFG_Telegrams.xml", 5);
                if (xmlTelegramFile == null)
                {
                    // Read XML configuration file from \MessageRouter sub folder.
                    xmlTelegramFile = PALS.Utilities.Functions.GetXMLFileFullName("PALS_BASE", @"SAC\CFG_Telegrams.xml", 5);

                    if (xmlTelegramFile == null)
                        throw new Exception("XML configuration file (CFG_Telegrams.xml) could not be found!" +
                                    "Please verify whether the Windows environment parameter (PALS_BASE) has been defined, " +
                                    "or file CFG_Telegrams.xml is existing in the folder {PALS_BASE}\\SAC\\");
                }

                _init = new BHS.Gateway.TCPClientTCPClientChains.Application.Initializer(xmlSettingFile, xmlTelegramFile);
                if (_init.Init() == false)
                {
                    _init.Dispose();
                    _init = null;
                    throw new Exception(SERVICE_NAME + " service initialization failure! Please verify XML config file settings.");
                }
                else
                {
                    try
                    {
                        // In the case of Windows Eventlog has been full, WriteEntry function will has 
                        // exeception. But the service should still be started for BHS production. Hence, 
                        // WriteEntry function is located in the Try...Catch...End Try block.
                        eventLog1.WriteEntry(SERVICE_NAME + " service has been successfully started.", EventLogEntryType.Information);
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    // In the case of Windows Eventlog has been full, WriteEntry function will has 
                    // exeception. But the service should still be started for BHS production. Hence, 
                    // WriteEntry function is located in the Try...Catch...End Try block.
                    eventLog1.WriteEntry(SERVICE_NAME + " service startup failure. <" + thisMethod +
                        "> Exception occurred!" + ex.ToString(), EventLogEntryType.Error);
                }
                catch
                {
                }
            }
        }

        protected override void OnStop()
        {
            try
            {
                eventLog1.WriteEntry(SERVICE_NAME + " service is stopping...", EventLogEntryType.Information);
            }
            catch
            {
            }

            if (_init != null)
            {
                _init.Dispose();
                _init = null;
            }

            try
            {
                eventLog1.WriteEntry(SERVICE_NAME + " service has been stopped.", EventLogEntryType.Information);
            }
            catch
            {
            }
        }

    }
}
