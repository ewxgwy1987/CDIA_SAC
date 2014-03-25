#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       ICR.cs
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
    /// ICR message handler class
    /// </summary>
    public class ICR : AbstractMessageHandler, IDisposable
    {
        #region Class Fields and Properties Declaration

        // The name of current class 
        private static readonly string _className =
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
        // Create a logger for use in this class
        private static readonly log4net.ILog _logger =
                    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Global Context 
        //private Configure.GlobalContext GlobalContext;

        #endregion

        #region Class Constructor, Dispose, & Destructor

        /// <summary>
        /// Class constructer.
        /// </summary>
        public ICR()
        {
            if (!Init())
                throw new Exception("Creating class " + _className + " object failed! <Constructor()>");
        }

        /// <summary>
        /// Class destructer.
        /// </summary>
        ~ICR()
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
            string inMsgSender, inMsgReceiver, channelName, type, length, sequence, gid, location, hbsLevel, hbsResult, plc_idx;
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
                location = string.Empty;
                hbsLevel = string.Empty;
                hbsResult = string.Empty;
                plc_idx = string.Empty;

                // 1. Decode message.
                MessageDecoding(message, out type, out length, out sequence, out gid, out location, out hbsLevel, out hbsResult, out plc_idx);

                // 2. Log message data into database.
                LogMessageIntoDatabase(inMsgSender, inMsgReceiver, gid, location, hbsLevel, hbsResult, plc_idx);

                // 3. Log message data into log file.
                if (_logger.IsInfoEnabled)
                    _logger.Info("[Channel:" + channelName +
                        "] <- [MSG(" + message.Format.AliasName +
                        "): Type=" + Functions.InvisibleCharacterFormating(ref type) +
                        ", Length=" + Functions.InvisibleCharacterFormating(ref length) +
                        ", Sequence=" + Functions.InvisibleCharacterFormating(ref sequence) +
                        ", GID=" + Functions.InvisibleCharacterFormating(ref gid) +
                        ", Location=" + Functions.InvisibleCharacterFormating(ref location) +
                        ", HBS Level=" + Functions.InvisibleCharacterFormating(ref hbsLevel) +
                        ", HBS Result=" + Functions.InvisibleCharacterFormating(ref hbsResult) +
                         ",PLC Index=" + Functions.InvisibleCharacterFormating(ref plc_idx) +
                        "]. (Perf:" + DateTime.Now.Subtract(Perf).TotalMilliseconds.ToString() +
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
                        out string sequence, out string gid, out string location, out string hbsLevel, out string hbsResult,
            out string plc_idx)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            string strGID_MSB = string.Empty;
            string strGID_LSB =  string.Empty;

          //<telegram alias="ICR" name="Item_Screened_Message" sequence="True" acknowledge="False">
          //  <field name="Type" offset="0" length="4" default="48,48,48,52"/>
          //  <field name="Length" offset="4" length="4" default="48,48,50,50"/>
          //  <field name="Sequence" offset="8" length="4" default="?"/>
          //  <field name="GID_MSB" offset="12" length="1" default="?"/>
          //  <field name="GID_LSB" offset="13" length="4" default="?"/>
          //  <field name="LOCATION" offset="17" length="2" default="?"/>
          //  <field name ="SCR_LVL"  offset="19" length="1" default="?" />
          //  <field name ="SCR_RES" offset="20" length="1" default="?" />
          //  <field name ="PLC_IDX" offset="21" length="1" default="?" />
          //</telegram>

            type = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Type"), -1, HexToStrMode.ToAscString).Trim();
            length = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Length"), -1, HexToStrMode.ToAscString).Trim();

            sequence = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("Sequence"), "32");
            strGID_MSB = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("GID_MSB"), "32");
            strGID_LSB = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("GID_LSB"), "32");
            location = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("LOCATION"), "32");
            hbsLevel = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("SCR_LVL"), "32");
            hbsResult = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("SCR_RES"), "32");
            plc_idx = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("PLC_IDX"), "32");

            gid = strGID_MSB.ToString().Trim() + strGID_LSB.ToString().Trim();

            return;
        }

        /// <summary>
        /// Log Message into database.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="receiver"></param>
        /// <param name="gid"></param>
        /// <param name="location"></param>
        /// <param name="hbsLevel"></param>
        /// <param name="hbsResult"></param>
        /// <param name="plc_idx"></param>
        private void LogMessageIntoDatabase(string sender, string receiver, string gid, string location, string hbsLevel, string hbsResult, string plc_idx)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;

            try
            {
                sqlConn = new SqlConnection(DBPersistor.ClassParameters.DBConnectionString);
                sqlCmd = new SqlCommand(DBPersistor.ClassParameters.STP_SAC_ITEMSCREENED, sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                sqlCmd.Parameters.Add("@GID", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@GID"].Value = gid;

                sqlCmd.Parameters.Add("@LOCATION", SqlDbType.VarChar, 20);
                sqlCmd.Parameters["@LOCATION"].Value = location;

                sqlCmd.Parameters.Add("@SCREEN_LEVEL", SqlDbType.VarChar, 1);
                sqlCmd.Parameters["@SCREEN_LEVEL"].Value = hbsLevel;

                sqlCmd.Parameters.Add("@RESULT_TYPE", SqlDbType.VarChar, 2);
                sqlCmd.Parameters["@RESULT_TYPE"].Value = hbsResult;

                sqlCmd.Parameters.Add("@PLC_IDX", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@PLC_IDX"].Value = plc_idx;

                //sqlCmd.Parameters.Add("@origin", SqlDbType.VarChar, 10);
                //sqlCmd.Parameters["@origin"].Value = GlobalContext.AppName;

                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("ICR message database process failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Logging received ICR message failure! <" + thisMethod + ">", ex);

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
