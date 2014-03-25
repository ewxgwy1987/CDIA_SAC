#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       BMAM.cs
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
    /// BMAM message handler class
    /// </summary>
    public class BMAM : AbstractMessageHandler, IDisposable
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
        public BMAM()
        {
            if (!Init())
                throw new Exception("Creating class " + _className + " object failed! <Constructor()>");
        }

        /// <summary>
        /// Class destructer.
        /// </summary>
        ~BMAM()
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
            string inMsgSender, inMsgReceiver, channelName, type, length, sequence, gid, bmaLocation, len,width, height, bmaType;
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
                length = string.Empty;
                len = string.Empty;
                width = string.Empty;
                height = string.Empty;
                bmaLocation = string.Empty;
                bmaType = string.Empty;

                // 1. Decode message.
                MessageDecoding(message, out type, out length, out sequence, out gid, out len, out width, out height, out bmaLocation, out bmaType);

                // 2. Log message data into database.
                LogMessageIntoDatabase(inMsgSender, inMsgReceiver, gid, bmaLocation, bmaType, len, width, height);

                // 3. Log message data into log file.
                if (_logger.IsInfoEnabled)
                    _logger.Info("[Channel:" + channelName +
                        "] <- [MSG(" + message.Format.AliasName +
                        "): Type=" + Functions.InvisibleCharacterFormating(ref type) +
                        ", Length=" + Functions.InvisibleCharacterFormating(ref length) +
                        ", Sequence=" + Functions.InvisibleCharacterFormating(ref sequence) +
                        ", GID=" + Functions.InvisibleCharacterFormating(ref gid) +
                        ", Baggage's Length=" + Functions.InvisibleCharacterFormating(ref len) +
                         ",Baggage's Width=" + Functions.InvisibleCharacterFormating(ref width) +
                         ",Baggage's Height=" + Functions.InvisibleCharacterFormating(ref height) +
                        ", BMA's Location=" + Functions.InvisibleCharacterFormating(ref bmaLocation) +
                        ", BMA's Type=" + Functions.InvisibleCharacterFormating(ref bmaType) +
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
                        out string sequence, out string gid, out string Length, out string Width, out string Height, 
                        out string bmaLocation,out string bmaType)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            
          //  <telegram alias="BMAM" name="Baggage_Measurement_Array_Message" sequence="True" acknowledge="False">
          //  <field name="Type" offset="0" length="4" default="48,48,48,54"/>
          //  <field name="Length" offset="4" length="4" default="48,48,50,54"/>
          //  <field name="Sequence" offset="8" length="4" default="?"/>
          //  <field name="GID_MSB" offset="12" length="1" default="?"/>
          //  <field name="GID_LSB" offset="13" length="4" default="?"/>
          //  <field name="BMASource" offset="17" length="2" default="?"/>
          //  <field name="LENGTH" offset="19" length="2" default="?"/>
          //  <field name="WIDTH" offset="21" length="2" default="?"/>
          //  <field name="HEIGHT" offset="23" length="2" default="?"/>
          //  <field name="BMAType" offset="25" length="1" default="?"/>
          //</telegram> 

            type = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Type"), -1, HexToStrMode.ToAscString).Trim();
            length = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Length"), -1, HexToStrMode.ToAscString).Trim();

            sequence = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("Sequence"), "32");
            gid = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("GID_MSB"), "16") + Utilities.ConvertVal2Decimal(message.GetFieldActualValue("GID_LSB"), "32");
            bmaLocation = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("BMASource"), "32");
            Length = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("LENGTH"), "16");
            Width = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("WIDTH"), "16");
            Height = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("HEIGHT"), "16");
            bmaType = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("BMAType"), "16");

            return;
        }


        private void LogMessageIntoDatabase(string sender, string receiver, string gid, 
                        string bmaLocation, string bmaType, string strLength, string strWidth, string strHeight)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;

            try
            {
                sqlConn = new SqlConnection(DBPersistor.ClassParameters.DBConnectionString);
                sqlCmd = new SqlCommand(DBPersistor.ClassParameters.STP_SAC_BAGGAGEMEASUREMENTARRAY, sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                sqlCmd.Parameters.Add("@GID", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@GID"].Value = gid;

                sqlCmd.Parameters.Add("@BMALocation", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@BMALocation"].Value = bmaLocation;

                sqlCmd.Parameters.Add("@Length", SqlDbType.Decimal);
                sqlCmd.Parameters["@Length"].Value = strLength;

                sqlCmd.Parameters.Add("@Width", SqlDbType.Decimal);
                sqlCmd.Parameters["@Width"].Value = strWidth;

                sqlCmd.Parameters.Add("@Height", SqlDbType.Decimal);
                sqlCmd.Parameters["@Height"].Value = strHeight;

                sqlCmd.Parameters.Add("@BMAType", SqlDbType.VarChar, 2);
                sqlCmd.Parameters["@BMAType"].Value = bmaType;                

                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("BMAM message database process failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Logging received BMAM message failure! <" + thisMethod + ">", ex);

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
