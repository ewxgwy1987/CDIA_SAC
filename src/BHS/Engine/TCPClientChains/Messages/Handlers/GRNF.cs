#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       GRNF.cs
// Revision:      1.0 -   04 Jun 2009, By Xu Jian
// =====================================================================================
//
#endregion

using System;
using System.Data;
using System.Data.SqlClient;
using PALS.Utilities;
using PALS.Telegrams;

namespace BHS.Engine.TCPClientChains.Messages.Handlers
{
    /// <summary>
    /// CSNF message handler class
    /// </summary>
    public class GRNF : AbstractMessageHandler, IDisposable
    {
        #region Class Fields and Properties Declaration

        //private const string DB_COLUMN_TIMESTAMP = "TIME_STAMP";
        //private const string DB_COLUMN_SUBSYSTEM = "SUBSYSTEM";
        //private const string DB_COLUMN_EQUIPMENT_ID = "EQUIPMENT_ID";
        //private const string DB_COLUMN_ALARM_TYPE = "ALARM_TYPE";
        //private const string DB_COLUMN_ALARM_DESC = "ALARM_DESCRIPTION";
        //private const string DB_COLUMN_CCTV_DEVICE_TYPE = "CCTV_DEVICE_TYPE";
        //private const string DB_COLUMN_CCTV_DEVICE_ID = "CCTV_DEVICE_ID";
        //private const string DB_COLUMN_CCTV_CAMERA_POSITION_ID = "CCTV_CAMERA_POSITION_ID";

        //private const string MDS_ALARM_ACTION = "ACTIVATED";
        
        // The name of current class 
        private static readonly string _className =
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
        // Create a logger for use in this class
        private static readonly log4net.ILog _logger =
                    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// MAAV Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_MAAV { get; set; }

        ///// <summary>
        ///// Event will be raised when message handler need send a message to external.
        ///// </summary>
        //public event EventHandler<MessageSendRequestEventArgs> OnSendRequest;

        #endregion

        #region Class Constructor, Dispose, & Destructor

        /// <summary>
        /// Class constructer.
        /// </summary>
        public GRNF()
        {
            if (!Init())
                throw new Exception("Creating class " + _className + " object failed! <Constructor()>");
        }

        /// <summary>
        /// Class destructer.
        /// </summary>
        ~GRNF()
        {
            Dispose(false);
        }

        /// <summary>
        /// Class method to be called by class wrapper for release resources explicitly.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        // Dispose(bool disposing) executes in two distinct scenarios. If disposing equals true, 
        // the method has been called directly or indirectly by a user's code. Managed and 
        // unmanaged resources can be disposed.
        // If disposing equals false, the method has been called by the runtime from inside the 
        // finalizer and you should not reference other objects. Only unmanaged resources can be disposed.
        private void Dispose(bool disposing)
        {
            // Release managed & unmanaged resources...
            MessageFormat_MAAV = null;

            if (disposing)
            {
                if (_logger.IsInfoEnabled)
                    _logger.Info("Class:[" + _className + "] object has been destroyed. <" + _className + ".Dispose()>");
            }
        }

        #endregion

        #region Class Method Declaration.

        /// <summary>
        /// Init() method.
        /// </summary>
        /// <returns></returns>
        protected bool Init()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            if (_logger.IsInfoEnabled)
                _logger.Info("Class:[" + _className + "] object is initializing... <" + thisMethod + ">");

            // ========================================================
            // Add initialization task code here.
            MessageFormat_MAAV = null;
            // ========================================================

            if (_logger.IsInfoEnabled)
                _logger.Info("Class:[" + _className + "] object has been initialized. <" + thisMethod + ">");

            return true;
        }

        /// <summary>
        /// Overridden class method for message handling.
        /// </summary>
        /// <param name="msgInfo"></param>
        protected override void MessageHandling(IncomingMessageInfo msgInfo)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            string inMsgSender, inMsgReceiver, channelName, type, length, sequence, gwAppCode;
            Telegram message;
            DateTime Perf = DateTime.Now;

            if (msgInfo == null)
                return;

            inMsgSender = msgInfo.Sender;
            inMsgReceiver = msgInfo.Receiver;
            channelName = msgInfo.ChannelName;
            message = msgInfo.Message;

            if (message == null)
                return;

            try
            {
                type = string.Empty;
                length = string.Empty;
                sequence = string.Empty;
                gwAppCode = string.Empty;

                // 1. Decode message.
                MessageDecoding(message, out type, out length, out sequence, out gwAppCode);
                // Return Gateway application code to upper level
                msgInfo.GRNF_AppCode = gwAppCode;

                // 2. Log message data into database.
                // GRNF is internal message between Gateway and Engine, it is not business data. Therefore, 
                // It is not logged into database for reporting.

                // 3. Log message data into log file.
                if (_logger.IsInfoEnabled)
                    _logger.Info("[Channel:" + channelName +
                        "] <- [MSG(" + message.Format.AliasName + 
                        "): Type=" + Functions.InvisibleCharacterFormating(ref type) + 
                        ", Length=" + Functions.InvisibleCharacterFormating(ref length) + 
                        ", Sequence=" + Functions.InvisibleCharacterFormating(ref sequence) + 
                        ", GatewayAppCode=" + Functions.InvisibleCharacterFormating(ref gwAppCode) + 
                        "] Gateway is ready! (Perf:" + DateTime.Now.Subtract(Perf).TotalMilliseconds.ToString() + 
                        "ms). <" + thisMethod + ">");

                //// 4. send all current MDS alarms stored in the database table [CCTV_MDS_ACTIVATED_ALARMS] 
                //if (DBPersistor.ClassParameters.SendMDSAlarmsUponGWReady)
                //{
                //    // Incoming message sender will be assigned to outgoing message receiver;
                //    // Incoming message receiver will be assigned to outgoing message sender;
                //    SendMDSCurrentAlarms(inMsgReceiver, inMsgSender, channelName);
                //}
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Message handling is failed! <" + thisMethod + ">", ex);
            }
        }

        /// <summary>
        /// Message Decoding
        /// </summary>
        private void MessageDecoding(Telegram message, out string type, out string length, 
                            out string sequence, out string gwAppCode)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            //<telegram alias="GRNF" name="Gateway_Ready_Notification_Message" sequence="True" acknowledge="False">
            //  <field name="Type" offset="0" length="4" default="48,49,48,57"/>
            //  <field name="Length" offset="4" length="4" default="48,48,50,48"/>
            //  <field name="Sequence" offset="8" length="4" default="?"/>
            //  <field name="AppCode" offset="12" length="8" default="?"/>
            //</telegram>
            type = Functions.ConvertByteArrayToString( 
                            message.GetFieldActualValue("Type"),-1, HexToStrMode.ToAscString).Trim();
            length = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Length"), -1, HexToStrMode.ToAscString).Trim();
            sequence = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Sequence"), -1, HexToStrMode.ToAscString).Trim();
            gwAppCode = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("AppCode"), -1, HexToStrMode.ToAscString).Trim();

            return;
        }
        
        #endregion


    }
}
