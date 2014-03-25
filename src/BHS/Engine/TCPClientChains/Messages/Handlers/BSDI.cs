#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       BSDI.cs
// Revision:      1.0 -   05 Oct 2009, By HS Chia
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
    /// BSDI message handler class
    /// </summary>
    public class BSDI : AbstractMessageHandler, IDisposable
    {
        #region Class Fields and Properties Declaration

        // The name of current class 
        private static readonly string _className =
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
        // Create a logger for use in this class
        private static readonly log4net.ILog _logger =
                    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region Class Constructor, Dispose, & Destructor

        /// <summary>
        /// Class constructor.
        /// </summary>
        public BSDI()
        {
            if (!Init())
                throw new Exception("Creating class " + _className + " object failed! <Constructor()>");
        }

        /// <summary>
        /// Class destructer.
        /// </summary>
        ~BSDI()
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
            string inMsgSender, inMsgReceiver, channelName, type, length, sequence, gid, lincesePlate, bdsLocation, plcTimeStamp, status;
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
                gid = string.Empty;
                lincesePlate = string.Empty;
                bdsLocation = string.Empty;
                plcTimeStamp = string.Empty;
                status = string.Empty;

                // 1. Decode message.
                MessageDecoding(message, out type, out length, out sequence, out gid, out lincesePlate, 
                                out bdsLocation, out plcTimeStamp, out status);

                // 2. Log message data into database.
                LogMessageIntoDatabase(inMsgSender, inMsgReceiver, gid, lincesePlate, bdsLocation, plcTimeStamp, status);

                // 3. Log message data into log file.
                if (_logger.IsInfoEnabled)
                    _logger.Info("[Channel:" + channelName +
                        "] <- [MSG(" + message.Format.AliasName +
                        "): Type=" + Functions.InvisibleCharacterFormating(ref type) +
                        ", Length=" + Functions.InvisibleCharacterFormating(ref length) +
                        ", Sequence=" + Functions.InvisibleCharacterFormating(ref sequence) +
                        ", GID=" + Functions.InvisibleCharacterFormating(ref gid) +
                        ", License plate=" + Functions.InvisibleCharacterFormating(ref lincesePlate) +
                        ", BDS Location=" + Functions.InvisibleCharacterFormating(ref bdsLocation) +
                        ", PLC TimeStamp=" + Functions.InvisibleCharacterFormating(ref plcTimeStamp) +
                        ", Status=" + Functions.InvisibleCharacterFormating(ref status) +
                        "]. (Perf:" + DateTime.Now.Subtract(Perf).Milliseconds.ToString() +
                        "ms). <" + thisMethod + ">");
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
                        out string sequence, out string gid, out string lincesePlate, 
                        out string bdsLocation, out string plcTimeStamp, out string status)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            //<telegram alias="BSDI" name="Bag_Status_Display_Information_Message" sequence="True" acknowledge="False">
            //  <field name="Type" offset="0" length="4" default="48,48,50,52"/>
            //  <field name="Length" offset="4" length="4" default="48,48,54,50"/>
            //  <field name="Sequence" offset="8" length="4" default="?"/>
            //  <field name="GID" offset="12" length="10" default="?"/>
            //  <field name="LicensePlate" offset="22" length="10" default="?"/>
            //  <field name="BDSSource" offset="32" length="10" default="?"/>       
            //  <field name="TimeStamp" offset="42" length="18" default="?"/>
            //  <field name="Status" offset="60" length="2" default="?"/>   
            //</telegram>  

            type = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Type"), -1, HexToStrMode.ToAscString).Trim();
            length = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Length"), -1, HexToStrMode.ToAscString).Trim();
            sequence = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Sequence"), -1, HexToStrMode.ToAscString).Trim();
            gid = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("GID"), -1, HexToStrMode.ToAscString).Trim();
            lincesePlate = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("LicensePlate"), -1, HexToStrMode.ToAscString).Trim();
            bdsLocation = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("BDSSource"), -1, HexToStrMode.ToAscString).Trim();
            plcTimeStamp = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("TimeStamp"), -1, HexToStrMode.ToAscString).Trim();
            status = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Status"), -1, HexToStrMode.ToAscString).Trim();


            return;
        }


        private void LogMessageIntoDatabase(string sender, string receiver, string gid,
                        string lincesePlate, string bdsLocation, string plcTimeStamp, string status)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;

            try
            {
                //sqlConn = new SqlConnection(DBPersistor.ClassParameters.DBConnectionString);
                //sqlCmd = new SqlCommand(DBPersistor.ClassParameters.STP_SAC_BAGSTATUSDISPLAYINFORATION, sqlConn);
                //sqlCmd.CommandType = CommandType.StoredProcedure;

                ////sqlCmd.Parameters.Add("@Sender", SqlDbType.VarChar, 8);
                ////sqlCmd.Parameters["@Sender"].Value = sender;

                ////sqlCmd.Parameters.Add("@Receiver", SqlDbType.VarChar, 8);
                ////sqlCmd.Parameters["@Receiver"].Value = receiver;

                //sqlCmd.Parameters.Add("@GID", SqlDbType.VarChar, 10);
                //sqlCmd.Parameters["@GID"].Value = gid;

                //sqlCmd.Parameters.Add("@LicensePlate", SqlDbType.VarChar, 10);
                //sqlCmd.Parameters["@LicensePlate"].Value = lincesePlate;

                //sqlCmd.Parameters.Add("@BDSLocation", SqlDbType.VarChar, 10);
                //sqlCmd.Parameters["@BDSLocation"].Value = bdsLocation;

                //sqlCmd.Parameters.Add("@TimeStamp", SqlDbType.VarChar, 18);
                //sqlCmd.Parameters["@TimeStamp"].Value = plcTimeStamp;

                //sqlCmd.Parameters.Add("@Status", SqlDbType.VarChar, 2);
                //sqlCmd.Parameters["@Status"].Value = status;

                //sqlConn.Open();
                //sqlCmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("BSDI message database process failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Logging received BSDI message failure! <" + thisMethod + ">", ex);

                return;
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
            }
        }


        #endregion


    }
}
