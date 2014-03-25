#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       ILT.cs
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
    /// ILT message handler class
    /// </summary>
    public class ILT : AbstractMessageHandler, IDisposable
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
        /// Class constructer.
        /// </summary>
        public ILT()
        {
            if (!Init())
                throw new Exception("Creating class " + _className + " object failed! <Constructor()>");
        }

        /// <summary>
        /// Class destructer.
        /// </summary>
        ~ILT()
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
            string inMsgSender, inMsgReceiver, channelName, type, length, sequence, subsystem, source, gid, lincesePlate;
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
                source = string.Empty;
                gid = string.Empty;
                

                // 1. Decode message.
                MessageDecoding(message, out type, out length, out sequence, out source, out gid);

                // 2. Log message data into database.
                LogMessageIntoDatabase(inMsgSender, inMsgReceiver, source, gid);

                // 3. Log message data into log file.
                if (_logger.IsInfoEnabled)
                    _logger.Info("[Channel:" + channelName +
                        "] <- [MSG(" + message.Format.AliasName +
                        "): Type=" + Functions.InvisibleCharacterFormating(ref type) +
                        ", Length=" + Functions.InvisibleCharacterFormating(ref length) +
                        ", Sequence=" + Functions.InvisibleCharacterFormating(ref sequence) +
                        ", GID=" + Functions.InvisibleCharacterFormating(ref gid) +
                        ", Location=" + Functions.InvisibleCharacterFormating(ref source) +
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
                        out string sequence, out string source, out string gid)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

          //<telegram alias="ILT" name="Item_Lost_Tracking_Message" sequence="True" acknowledge="False">
          //  <field name="Type" offset="0" length="4" default="48,48,49,48"/>
          //  <field name="Length" offset="4" length="4" default="48,48,49,57"/>
          //  <field name="Sequence" offset="8" length="4" default="?"/>
          //  <field name="GID_MSB" offset="12" length="1" default="?"/>
          //  <field name="GID_LSB" offset="13" length="4" default="?"/>
          //  <field name="LOCATION" offset="17" length="2" default="?"/>
          //</telegram>

            string strGID_MSB = string.Empty;
            string strGID_LSB = string.Empty;

            type = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Type"), -1, HexToStrMode.ToAscString).Trim();
            length = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Length"), -1, HexToStrMode.ToAscString).Trim();

            sequence = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("Sequence"), "32");
            strGID_MSB = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("GID_MSB"), "16");
            strGID_LSB = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("GID_LSB"), "32");
            source = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("LOCATION"), "32");

            gid = strGID_MSB + strGID_LSB;
    
            return;
        }


        private void LogMessageIntoDatabase(string sender, string receiver, string source, string gid)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;

            try
            {
                sqlConn = new SqlConnection(DBPersistor.ClassParameters.DBConnectionString);
                sqlCmd = new SqlCommand(DBPersistor.ClassParameters.STP_SAC_ITEMLOST, sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                sqlCmd.Parameters.Add("@Location", SqlDbType.VarChar, 20);
                sqlCmd.Parameters["@Location"].Value = source;

                sqlCmd.Parameters.Add("@GID", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@GID"].Value = gid;

                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("ILT message database process failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Logging received ILT message failure! <" + thisMethod + ">", ex);

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
