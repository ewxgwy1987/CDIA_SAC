#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       PV1K.cs
// Revision:      1.0 -   05 Oct 2009, By HS Chia
// =====================================================================================
//
#endregion

using System;
using System.Data;
using System.Data.SqlClient;
using PALS.Utilities;
using PALS.Telegrams;
using System.Collections.Generic;
using System.Collections;

namespace BHS.Engine.TCPClientChains.Messages.Handlers
{
    /// <summary>
    /// PV1K message handler class
    /// </summary>
    public class PV1K : AbstractMessageHandler, IDisposable
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
        public PV1K()
        {
            if (!Init())
                throw new Exception("Creating class " + _className + " object failed! <Constructor()>");
        }

        /// <summary>
        /// Class destructer.
        /// </summary>
        ~PV1K()
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
            string inMsgSender, inMsgReceiver, channelName, type, length, sequence, gid, lincesePlate, xRayID, location, status;
            List<string> reasonCode = new List<string>();
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
                location = string.Empty;
                xRayID = string.Empty;
                status = string.Empty;

                // 1. Decode message.
                MessageDecoding(message, out type, out length, out sequence, out gid, out lincesePlate, out xRayID, out location, out status, out reasonCode);

                string dbReason = string.Empty;
                string logReason = string.Empty;

                string[] tempReasonCode = new string[reasonCode.Count];
                tempReasonCode = reasonCode.ToArray();

                for (int i = 0; i < reasonCode.Count; i++)
                {
                    dbReason = dbReason + tempReasonCode[i];
                    logReason = logReason + i.ToString() + "(" + tempReasonCode[i] + ")";

                    if (i < reasonCode.Count - 1)
                    {
                        logReason = logReason + ", ";
                    }
                }

                // 2. Log message data into database.
                LogMessageIntoDatabase(inMsgSender, inMsgReceiver, gid, lincesePlate, xRayID, location, status, dbReason);

                // 3. Log message data into log file.
                if (_logger.IsInfoEnabled)
                    _logger.Info("[Channel:" + channelName +
                        "] <- [MSG(" + message.Format.AliasName +
                        "): Type=" + Functions.InvisibleCharacterFormating(ref type) +
                        ", Length=" + Functions.InvisibleCharacterFormating(ref length) +
                        ", Sequence=" + Functions.InvisibleCharacterFormating(ref sequence) +
                        ", GID=" + Functions.InvisibleCharacterFormating(ref gid) +
                        ", License plate=" + Functions.InvisibleCharacterFormating(ref lincesePlate) +
                        ", X-Ray ID=" + Functions.InvisibleCharacterFormating(ref xRayID) +
                        ", Location=" + Functions.InvisibleCharacterFormating(ref location) +
                        ", Status=" + Functions.InvisibleCharacterFormating(ref status) +
                        ", Reason Code=" + Functions.InvisibleCharacterFormating(ref logReason) +
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
                        out string xRayID, out string location, out string status, out List<string> reasonCode)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            byte[] byteArray = new byte[14];

            //<telegram alias="PV1K" name="PVP1000_Information_Message" sequence="True" acknowledge="False">
            //  <field name="Type" offset="0" length="4" default="48,48,50,51"/>
            //  <field name="Length" offset="4" length="4" default="48,48,52,54"/>
            //  <field name="Sequence" offset="8" length="4" default="?"/>
            //  <field name="GID" offset="12" length="10" default="?"/>
            //  <field name="LicensePlate" offset="22" length="10" default="?"/>
            //  <field name="XRayID" offset="32" length="10" default="?"/>       
            //  <field name="Status" offset="42" length="2" default="?"/>
            //  <field name="Reason" offset="44" length="2" default="?"/>   
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
            xRayID = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("XRayID"), -1, HexToStrMode.ToAscString).Trim();
            location = Functions.ConvertByteArrayToString(
                message.GetFieldActualValue("Location"), -1, HexToStrMode.ToAscString).Trim();
            status = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Status"), -1, HexToStrMode.ToAscString).Trim();
            byteArray = message.GetFieldActualValue("Reason");

            // Get values from each scanner head
            byte[] andLogical = new byte[8] { 0x1, 0x2, 0x4, 0x8, 0x10, 0x20, 0x40, 0x80 }; // values - 1,2,4,8,16,32,64,128
            int[] position = new int[14] { 0, 8, 16, 24, 32, 40, 48, 56, 64, 72, 80, 88, 96, 104 }; // as 8 bits 
            //byte asii0 = 0x30; // 48
            //byte asiiA = 0x40; // 64
            //byte asiiA2F = 0x37; // 55
            int counterByte;
            int innerCounter;

            //// Convert from ASII to Char (0 to F)
            //for (counterByte = 0; counterByte < byteArray.Length; counterByte++)
            //{
            //    if (byteArray[counterByte] < asiiA) //0-9
            //    {
            //        byteArray[counterByte] = (byte)(byteArray[counterByte] - asii0);
            //    }
            //    else // A-F
            //    {
            //        byteArray[counterByte] = (byte)(byteArray[counterByte] - asiiA2F);
            //    }
            //}

            int[] head = new int[32];

            string temp;
            reasonCode = new List<string>();


            // Convert to Bit (first 8 bits)
            for (counterByte = 0; counterByte < byteArray.Length; counterByte++)
            {
                innerCounter = 0;
                temp = string.Empty;

                while (innerCounter < 8)
                {
                    if ((byteArray[counterByte] & andLogical[innerCounter]) == andLogical[innerCounter])
                    {
                        //(head[innerCounter + position[counterByte]]) = 1;
                        temp = temp + "1";
                    }
                    else if ((byteArray[counterByte] & andLogical[innerCounter]) == 0x0)
                    {
                        //(head[innerCounter + position[counterByte]]) = 0;
                        temp = temp + "0";
                    }
                    innerCounter = innerCounter + 1;
                }

                reasonCode.Add(temp);
            }

            return;
        }


        private void LogMessageIntoDatabase(string sender, string receiver, string gid, string lincesePlate,
                        string xRayID, string location, string status, string reasonCode)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;
            string temp = string.Empty;
            try
            {
                sqlConn = new SqlConnection(DBPersistor.ClassParameters.DBConnectionString);
                sqlCmd = new SqlCommand(DBPersistor.ClassParameters.STP_SAC_1500PINFORMATION, sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                //sqlCmd.Parameters.Add("@Sender", SqlDbType.VarChar, 8);
                //sqlCmd.Parameters["@Sender"].Value = sender;

                //sqlCmd.Parameters.Add("@Receiver", SqlDbType.VarChar, 8);
                //sqlCmd.Parameters["@Receiver"].Value = receiver;

                sqlCmd.Parameters.Add("@GID", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@GID"].Value = gid;

                sqlCmd.Parameters.Add("@LicensePlate", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@LicensePlate"].Value = lincesePlate;

                sqlCmd.Parameters.Add("@XRayID", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@XRayID"].Value = xRayID;

                sqlCmd.Parameters.Add("@Location", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@Location"].Value = location;

                sqlCmd.Parameters.Add("@Status", SqlDbType.VarChar, 2);
                sqlCmd.Parameters["@Status"].Value = status;

                sqlCmd.Parameters.Add("@ReasonCode", SqlDbType.VarChar, 112);
                sqlCmd.Parameters["@ReasonCode"].Value = reasonCode;

                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("PV1K message database process failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Logging received PV1K message failure! <" + thisMethod + ">", ex);

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
